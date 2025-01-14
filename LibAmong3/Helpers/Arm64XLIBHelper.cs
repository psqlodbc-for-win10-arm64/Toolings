using CoffReader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class Arm64XLIBHelper
    {
        private readonly RunLIBHelper _libExe;
        private readonly NormHelper _normHelper;
        private readonly Func<TempFileHelper> _newTempFileHelper;
        private readonly ParseLibArgHelper _libParser;
        private readonly ParseWinArgsHelper _parseArgs;

        public Arm64XLIBHelper(
            ParseLibArgHelper libParser,
            ParseWinArgsHelper parseArgs,
            RunLIBHelper libExe,
            NormHelper normHelper,
            Func<TempFileHelper> newTempFileHelper)
        {
            _normHelper = normHelper;
            _newTempFileHelper = newTempFileHelper;
            _libParser = libParser;
            _parseArgs = parseArgs;
            _libExe = libExe;
        }

        public int RunLIB(string[] args, bool dualObj)
        {
            using var tempFileHelper = _newTempFileHelper();

            var libArgs = args
                .Select(_libParser.Parse)
                .ToArray();

            IEnumerable<string> ProcessLibArg(LibArg arg)
            {
                if (arg.Machine.Length != 0)
                {
                    return [];
                }
                else if (dualObj && arg.IsObj)
                {
                    if (arg.Value.EndsWith(".obj", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var file = File.ReadAllBytes(arg.Value);
                        var coff = CoffParser.Parse(file, true);
                        if (coff.Magic != 0x14c)
                        {
                            throw new Exception("Not a I386 COFF (ARM64/ARM64EC mixed) file!");
                        }

                        var prefix = (_normHelper.Norm(arg.Value));
                        var arm64Obj = prefix + ".ARM64.obj";

                        File.WriteAllBytes(
                            arm64Obj,
                            CoffParser.ReadRawData(
                                file,
                                coff.Sections
                                    .Single(it => it.Name == "AA64.obj")
                            )
                                .ToArray()
                        );

                        var arm64ECObj = prefix + ".ARM64EC.obj";

                        File.WriteAllBytes(
                            arm64ECObj,
                            CoffParser.ReadRawData(
                                file,
                                coff.Sections
                                    .Single(it => it.Name == "A641.obj")
                            )
                                .ToArray()
                        );

                        return [arm64Obj, arm64ECObj,];
                    }
                    else
                    {
                        return [arg.Value];
                    }
                }
                else if (arg.At.Length != 0)
                {
                    var atFile = tempFileHelper.GetTempFile("at.txt");
                    File.WriteAllLines(
                        atFile,
                        _parseArgs.ParseArgs(File.ReadAllText(arg.At))
                            .Select(_libParser.Parse)
                            .SelectMany(ProcessLibArg),
                        new UTF8Encoding(true)
                    );

                    return [$"@{atFile}"];
                }
                else
                {
                    return [arg.Value];
                }
            }

            var exitCode = _libExe.RunLIB(
                libArgs
                    .SelectMany(ProcessLibArg)
                    .Append("/MACHINE:ARM64X")
                    .ToArray()
            );

            return exitCode;
        }
    }
}
