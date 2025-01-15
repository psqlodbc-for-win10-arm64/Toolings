using CoffReader;
using LibAmong3.Helpers.CLTargetHelpers;
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
        private readonly DecideCLTargetTypeHelper _decideCLTargetTypeHelper;
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
            Func<TempFileHelper> newTempFileHelper,
            DecideCLTargetTypeHelper decideCLTargetTypeHelper)
        {
            _decideCLTargetTypeHelper = decideCLTargetTypeHelper;
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

            var commonClArgs = clArgs
                .Where(it => true
                    && it.Fo.Length == 0
                    && !it.FileInput
                    && !it.Arm64EC
                );

            var clFileArgs = clArgs
                .Where(it => it.FileInput)
                .ToArray();

            var exitCode = 0;

            if (clArgs.Any(it => it.CompileOnly))
            {
                var arm64xObj = clArgs
                    .FirstOrDefault(it => it.Fo.Length != 0)?
                    .Fo
                        ?? clFileArgs
                            .Select(it => Path.ChangeExtension(it.Value, ".obj"))
                            .FirstOrDefault()
                        ?? throw new ArgumentException("Need /FoFILENAME.OBJ or single source file to comiple!");

                var objName = _normHelper.Norm(Path.GetFileName(arm64xObj));

                var buildTargets = clFileArgs
                    .Select(it => it.Value)
                    .ToArray();

                // Build ARM64 COFF
                var arm64Obj = tempFileHelper.GetTempFile($"{objName}.arm64.obj");
                exitCode = _clExe.RunCL(
                    commonClArgs
                        .Select(it => it.Value)
                        .Append($"/Fo{arm64Obj}")
                        .Concat(buildTargets)
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
                    commonClArgs
                        .Select(it => it.Value)
                        .Append($"/Fo{arm64ECObj}")
                        .Append($"/arm64EC")
                        .Concat(buildTargets)
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
            else if (clArgs.Any(it => it.E || it.EP || it.P || it.Zs))
            {
                // Only pre-processor

                var clFilesList = new List<string>();

                clFilesList.AddRange(
                    clFileArgs
                        .Select(it => it.Value)
                );

                exitCode = _clExe.RunCL(
                    new string[0]
                        .Concat(
                            commonClArgs
                                .Select(it => it.Value)
                        )
                        .Concat(clFilesList)
                        .ToArray()
                );

                return exitCode;
            }
            else
            {
                var clFilesList = new List<string>();

                foreach (var fileArg in clFileArgs)
                {
                    var objFileName = _normHelper.Norm(Path.GetFileName(fileArg.Value));

                    switch (_decideCLTargetTypeHelper.Decide(fileArg.Value) ?? CLTargetType.Other)
                    {
                        case CLTargetType.SourceFile:
                            {
                                {
                                    // Build ARM64 COFF
                                    var arm64Obj = tempFileHelper.GetTempFile($"{objFileName}.arm64.obj");
                                    exitCode = _clExe.RunCL(
                                        commonClArgs
                                            .Select(it => it.Value)
                                            .Append($"/Fo{arm64Obj}")
                                            .Append("-c")
                                            .Append(fileArg.Value)
                                            .ToArray()
                                    );

                                    if (exitCode != 0)
                                    {
                                        Console.Error.WriteLine("Compile ARM64 obj failed.");
                                        return exitCode;
                                    }

                                    if (!File.Exists(arm64Obj))
                                    {
                                        Console.Error.WriteLine("No ARM64 obj generated.");

                                        return 1;
                                    }

                                    clFilesList.Add(arm64Obj);
                                }

                                {
                                    // Build ARM64EC COFF
                                    var arm64ECObj = tempFileHelper.GetTempFile($"{objFileName}.arm64EC.obj");
                                    exitCode = _clExe.RunCL(
                                        commonClArgs
                                            .Select(it => it.Value)
                                            .Append($"/Fo{arm64ECObj}")
                                            .Append($"/arm64EC")
                                            .Append("-c")
                                            .Append(fileArg.Value)
                                            .ToArray()
                                    );

                                    if (exitCode != 0)
                                    {
                                        Console.Error.WriteLine("Compile ARM64EC obj failed.");
                                        return exitCode;
                                    }

                                    if (!File.Exists(arm64ECObj))
                                    {
                                        Console.Error.WriteLine("No ARM64EC obj generated.");
                                        return 1;
                                    }

                                    clFilesList.Add(arm64ECObj);
                                }
                                break;
                            }
                        case CLTargetType.Arm64XCoffUponX86Coff:
                            {
                                var file = File.ReadAllBytes(fileArg.Value);
                                var coff = CoffParser.Parse(file, true);

                                {
                                    var arm64Obj = tempFileHelper.GetTempFile($"{objFileName}.ARM64.obj");

                                    File.WriteAllBytes(
                                        arm64Obj,
                                        CoffParser.ReadRawData(
                                            file,
                                            coff.Sections
                                                .Single(it => it.Name == "AA64.obj")
                                        )
                                            .ToArray()
                                    );

                                    clFilesList.Add(arm64Obj);
                                }

                                {
                                    var arm64ECObj = tempFileHelper.GetTempFile($"{objFileName}.ARM64EC.obj");

                                    File.WriteAllBytes(
                                        arm64ECObj,
                                        CoffParser.ReadRawData(
                                            file,
                                            coff.Sections
                                                .Single(it => it.Name == "A641.obj")
                                        )
                                            .ToArray()
                                    );

                                    clFilesList.Add(arm64ECObj);
                                }

                                break;
                            }
                        default:
                            {
                                clFilesList.Add(fileArg.Value);
                                break;
                            }
                    }
                }

                if (commonClArgs.All(it => it.Fe.Length == 0) && clFileArgs.Any())
                {
                    // EXE output file path not given
                    var candidates = clFileArgs
                        .Select(it => (CLTargetType: _decideCLTargetTypeHelper.Decide(it.Value), InputFile: it.Value))
                        .ToArray();

                    var hits = candidates.Where(
                        it => false
                            || it.CLTargetType == CLTargetType.Arm64Coff
                            || it.CLTargetType == CLTargetType.Arm64ECCoff
                            || it.CLTargetType == CLTargetType.Arm64XCoffUponX86Coff
                        );

                    var hit = hits.Any() ? hits.First() : candidates.First();

                    var fileExtension = (commonClArgs.Any(it => it.LD || it.LDd)) ? ".dll" : ".exe";
                    var outputFileName = Path.ChangeExtension(Path.GetFileName(hit.InputFile), fileExtension);

                    clFilesList.Add($"/Fe{outputFileName}");
                }

                exitCode = _clExe.RunCL(
                    new string[0]
                        .Concat(
                            commonClArgs
                                .Select(it => it.Value)
                        )
                        .Concat(clFilesList)
                        .Append("/link")
                        .Concat(
                            linkArgs
                                .Where(it => it.Machine.Length == 0)
                                .Select(it => it.Value)
                                .Append("/machine:ARM64X")
                        )
                        .ToArray()
                );

                return exitCode;
            }
        }
    }
}
