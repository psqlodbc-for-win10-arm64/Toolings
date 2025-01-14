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
        private readonly Func<TempFileHelper> _newTempFileHelper;
        private readonly RunLINKHelper _linkExe;
        private readonly ParseLinkArgHelper _linkParser;

        public Arm64XLINKHelper(
            ParseLinkArgHelper linkParser,
            RunLINKHelper linkExe,
            Func<TempFileHelper> newTempFileHelper)
        {
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
                            .SelectMany(ProcessLinkArg),
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
