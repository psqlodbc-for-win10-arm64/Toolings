using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class Arm64XCLHelper
    {
        private readonly MakeCoffHelper _makeCoffHelper;
        private readonly Func<TempFileHelper> _newTempFileHelper;
        private readonly NormHelper _normHelper;
        private readonly RunLIBHelper _libExe;
        private readonly RunLINKHelper _linkExe;
        private readonly RunCLHelper _clExe;
        private readonly ParseClArgHelper _clParser;
        private readonly ParseLinkArgHelper _linkParser;

        public Arm64XCLHelper(
            ParseClArgHelper clParser,
            ParseLinkArgHelper linkParser,
            RunCLHelper clExe,
            RunLINKHelper linkExe,
            RunLIBHelper libExe,
            NormHelper normHelper,
            MakeCoffHelper makeCoffHelper,
            Func<TempFileHelper> newTempFileHelper)
        {
            _makeCoffHelper = makeCoffHelper;
            _newTempFileHelper = newTempFileHelper;
            _normHelper = normHelper;
            _libExe = libExe;
            _linkExe = linkExe;
            _clExe = clExe;
            _clParser = clParser;
            _linkParser = linkParser;
        }

        public int RunCL(string[] args, bool dualObj)
        {
            var argList = args
                .Select(_clParser.Parse)
                .ToArray();

            var linkAt = Array.FindIndex(argList, it => it.Link);

            var clArgs = (linkAt < 0)
                ? argList
                : argList.Take(linkAt).ToArray();
            var linkArgs = (linkAt < 0)
                ? Array.Empty<LinkArg>()
                : argList
                    .Skip(linkAt + 1)
                    .Select(it => _linkParser.Parse(it.Value))
                    .ToArray();

            using var tempFileHelper = _newTempFileHelper();

            var exitCode = 0;

            if (clArgs.Any(it => it.CompileOnly))
            {
                var arm64xObj = clArgs
                    .FirstOrDefault(it => it.Fo.Length != 0)?
                    .Fo ?? throw new ArgumentException("Need /FoFILENAME.OBJ !");

                var objName = _normHelper.Norm(Path.GetFileName(arm64xObj));

                // Build ARM64 COFF
                var arm64Obj = tempFileHelper.GetTempFile($"{objName}.arm64.obj");
                exitCode = _clExe.RunCL(
                    clArgs
                        .Where(it => it.Fo.Length == 0 && !it.Arm64EC)
                        .Select(it => it.Value)
                        .Append($"/Fo{arm64Obj}")
                        .ToArray()
                );

                if (exitCode != 0)
                {
                    Console.Error.WriteLine("Compile ARM64 obj failed.");
                    return exitCode;
                }

                // Build ARM64EC COFF
                var arm64ECObj = tempFileHelper.GetTempFile($"{objName}.arm64ec.obj");
                exitCode = _clExe.RunCL(
                    clArgs
                        .Where(it => it.Fo.Length == 0 && !it.Arm64EC)
                        .Select(it => it.Value)
                        .Append($"/Fo{arm64ECObj}")
                        .Append($"/arm64EC")
                        .ToArray()
                );

                if (exitCode != 0)
                {
                    Console.Error.WriteLine("Compile ARM64EC obj failed.");
                    return exitCode;
                }

                if (dualObj)
                {
                    // Save pseudo-ARM64X COFF

                    File.WriteAllBytes(
                        arm64xObj,
                        _makeCoffHelper.Make(
                            [
                                (0xAA64, ".obj", File.ReadAllBytes(arm64Obj)),
                                (0xA641, ".obj", File.ReadAllBytes(arm64ECObj)),
                            ]
                        )
                    );

                    return 0;
                }
                else
                {
                    // Save ARM64X COFF as native ARM64X LIB
                    // This ARM64X LIB contains special `/<ECSYMBOLS>/` file so it cannot be made easily.

                    return _libExe.RunLIB(
                        ["/machine:ARM64X", $"/out:{arm64xObj}", arm64Obj, arm64ECObj]
                    );
                }
            }
            else
            {
                // Build ARM64 COFF
                var arm64Obj = tempFileHelper.GetTempFile("arm64.obj");
                exitCode = _clExe.RunCL(
                    clArgs
                        .Where(it => it.Fo.Length == 0 && !it.Arm64EC)
                        .Select(it => it.Value)
                        .Append($"/Fo{arm64Obj}")
                        .Append("/c")
                        .ToArray()
                );

                if (exitCode != 0)
                {
                    Console.Error.WriteLine("Compile ARM64 obj failed.");
                    return exitCode;
                }

                // Build ARM64EC COFF
                var arm64ECObj = tempFileHelper.GetTempFile("arm64ec.obj");
                exitCode = _clExe.RunCL(
                    clArgs
                        .Where(it => it.Fo.Length == 0 && !it.Arm64EC)
                        .Select(it => it.Value)
                        .Append($"/Fo{arm64ECObj}")
                        .Append($"/arm64EC")
                        .Append("/c")
                        .ToArray()
                );

                if (exitCode != 0)
                {
                    Console.Error.WriteLine("Compile ARM64EC obj failed.");
                    return exitCode;
                }

                exitCode = _linkExe.RunLINK(
                    linkArgs
                        .Select(it => it.Value)
                        .Append(arm64Obj)
                        .Append(arm64ECObj)
                        .ToArray()
                );

                return 0;
            }
        }
    }
}
