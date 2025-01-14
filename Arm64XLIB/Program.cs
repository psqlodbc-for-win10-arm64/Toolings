using CoffReader;
using LibAmong3.Helpers;
using System.Diagnostics;
using System.Text;

namespace Arm64XLIB
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var libExe = @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\lib.exe";

            int RunLIB(string[] runArgs)
            {
                var psi = new ProcessStartInfo(
                    libExe,
                    string.Join(" ", runArgs.Select(WinCmdHelper.EscapeArg))
                )
                {
                    UseShellExecute = false,
                };
                var p = Process.Start(psi) ?? throw new Exception("Failed to start process");
                p.WaitForExit();
                return p.ExitCode;
            }

            var libParser = new ParseLibArgHelper();

            using var tempFileHelper = new TempFileHelper();

            var libArgs = args
                .Select(libParser.Parse)
                .ToArray();

            var parseArgs = new ParseWinArgsHelper();

            IEnumerable<string> ProcessLibArg(LibArg arg)
            {
                if (arg.Machine.Length != 0)
                {
                    return [];
                }
                else if (arg.At.Length != 0)
                {
                    var atFile = tempFileHelper.GetTempFile("at.txt");
                    File.WriteAllLines(
                        atFile,
                        parseArgs.ParseArgs(File.ReadAllText(arg.At))
                            .Select(libParser.Parse)
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

            var exitCode = RunLIB(
                libArgs
                    .SelectMany(ProcessLibArg)
                    .Select(it => WinCmdHelper.EscapeArg(it))
                    .Append("/MACHINE:ARM64X")
                    .ToArray()
            );

            return exitCode;
        }
    }
}
