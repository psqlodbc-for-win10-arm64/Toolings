using CoffReader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class Arm64XLINKHelper
    {
        private readonly WinCmdHelper _winCmdHelper;
        private readonly NormHelper _normHelper;
        private readonly Func<TempFileHelper> _newTempFileHelper;
        private readonly RunLINKHelper _linkExe;
        private readonly ParseLinkArgHelper _linkParser;

        public Arm64XLINKHelper(
            ParseLinkArgHelper linkParser,
            RunLINKHelper linkExe,
            NormHelper normHelper,
            Func<TempFileHelper> newTempFileHelper,
            WinCmdHelper winCmdHelper)
        {
            _winCmdHelper = winCmdHelper;
            _normHelper = normHelper;
            _newTempFileHelper = newTempFileHelper;
            _linkExe = linkExe;
            _linkParser = linkParser;
        }

        public int RunLINK(string[] args, bool dualObj)
        {
            var argList = args
                .Select(_linkParser.Parse)
                .ToArray();

            using var tempFileHelper = _newTempFileHelper();

            var parseArgs = new ParseWinArgsHelper();

            IEnumerable<string> ProcessLinkArg(LinkArg arg)
            {
                if (arg.Machine.Length != 0 || arg.DefArm64Native.Length != 0)
                {
                    return [];
                }
                else if (arg.IsObj)
                {
                    if (false
                        || string.Compare(arg.Value, "setargv.obj", true) == 0
                        || string.Compare(arg.Value, "wsetargv.obj", true) == 0
                    )
                    {
                        return [arg.Value];
                    }
                    else if (dualObj && arg.Value.EndsWith(".obj", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var file = File.ReadAllBytes(arg.Value);
                        var coff = CoffParser.Parse(file, true);
                        if (coff.Magic != 0x14c)
                        {
                            throw new Exception("Not a I386 COFF (ARM64/ARM64EC mixed) file!");
                        }

                        var prefix = arg.Value;
                        var arm64Obj = tempFileHelper.GetTempFile(_normHelper.Norm(prefix + ".ARM64.obj"));

                        File.WriteAllBytes(
                            arm64Obj,
                            CoffParser.ReadRawData(
                                file,
                                coff.Sections
                                    .Single(it => it.Name == "AA64.obj")
                            )
                                .ToArray()
                        );

                        var arm64ECObj = tempFileHelper.GetTempFile(_normHelper.Norm(prefix + ".ARM64EC.obj"));

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
                else if (arg.Def.Length != 0)
                {
                    return [arg.Value, $"/defArm64Native:{arg.Def}"];
                }
                else if (arg.At.Length != 0)
                {
                    var atFile = tempFileHelper.GetTempFile("at.txt");
                    File.WriteAllLines(
                        atFile,
                        parseArgs.ParseArgs(File.ReadAllText(arg.At))
                            .Select(_linkParser.Parse)
                            .SelectMany(ProcessLinkArg)
                            .Select(_winCmdHelper.EscapeArg),
                        new UTF8Encoding(true)
                    );

                    return [$"@{atFile}"];
                }
                else
                {
                    return [arg.Value];
                }
            }

            var exitCode = _linkExe.RunLINK(
                argList
                    .SelectMany(ProcessLinkArg)
                    .Append("/MACHINE:ARM64X")
                    .ToArray()
            );

            return exitCode;
        }
    }
}
