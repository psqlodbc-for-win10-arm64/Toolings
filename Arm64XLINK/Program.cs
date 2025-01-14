using CoffReader;
using LibAmong3.Helpers;
using System.Diagnostics;
using System.Text;

namespace Arm64XLINK
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var linkParser = new ParseLinkArgHelper();

            var argList = args
                .Select(linkParser.Parse)
                .ToArray();

            var pwd = Environment.CurrentDirectory;

            var linkExe = @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\link.exe";

            int RunLINK(string[] runArgs)
            {
                var psi = new ProcessStartInfo(
                    linkExe,
                    string.Join(" ", runArgs.Select(WinCmdHelper.EscapeArg))
                )
                {
                    UseShellExecute = false,
                };
                var p = Process.Start(psi) ?? throw new Exception("Failed to start process");
                p.WaitForExit();
                return p.ExitCode;
            }

            using var tempFileHelper = new TempFileHelper();

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
                            .Select(linkParser.Parse)
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

            var exitCode = RunLINK(
                argList
                    .SelectMany(ProcessLinkArg)
                    .Select(WinCmdHelper.EscapeArg)
                    .Append("/MACHINE:ARM64X")
                    .ToArray()
            );

            return exitCode;
        }

        private static string Norm(string path)
            => path
                .Replace("\\", "_")
                .Replace("/", "_")
            ;
    }
}
