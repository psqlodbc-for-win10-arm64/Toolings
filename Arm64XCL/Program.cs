using LibAmong3.Helpers;
using System.Diagnostics;

namespace Arm64XCL
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var clParser = new ParseClArgHelper();
            var linkParser = new ParseLinkArgHelper();

            var argList = args
                .Select(clParser.Parse)
                .ToArray();

            var linkAt = Array.FindIndex(argList, it => it.Link);

            var clArgs = (linkAt < 0)
                ? argList
                : argList.Take(linkAt).ToArray();
            var linkArgs = (linkAt < 0)
                ? Array.Empty<LinkArg>()
                : argList
                    .Skip(linkAt + 1)
                    .Select(it => linkParser.Parse(it.Value))
                    .ToArray();

            var pwd = Environment.CurrentDirectory;

            var clExe = @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\cl.exe";
            var linkExe = @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\link.exe";
            var libExe = @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\lib.exe";

            int RunCL(string[] runArgs)
            {
                var psi = new ProcessStartInfo(
                    clExe,
                    string.Join(" ", runArgs.Select(WinCmdHelper.EscapeArg))
                )
                {
                    UseShellExecute = false,
                };
                var p = Process.Start(psi) ?? throw new Exception("Failed to start process");
                p.WaitForExit();
                return p.ExitCode;
            }

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

            using var tempFileHelper = new TempFileHelper();

            var exitCode = 0;

            if (clArgs.Any(it => it.CompileOnly))
            {
                // Build ARM64 COFF
                var arm64Obj = tempFileHelper.GetTempFile("arm64.obj");
                exitCode = RunCL(
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
                var arm64ECObj = tempFileHelper.GetTempFile("arm64ec.obj");
                exitCode = RunCL(
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

                // Save ARM64X COFF as native ARM64X LIB
                // This ARM64X LIB contains special `/<ECSYMBOLS>/` file so it cannot be made easily.

                var arm64xObj = clArgs
                    .FirstOrDefault(it => it.Fo.Length != 0)?
                    .Fo ?? throw new ArgumentException("Need /FoFILENAME.OBJ !");

                RunLIB(
                    ["/machine:ARM64X", $"/out:{arm64xObj}", arm64Obj, arm64ECObj]
                );

                return 0;
            }
            else
            {
                // Build ARM64 COFF
                var arm64Obj = tempFileHelper.GetTempFile("arm64.obj");
                exitCode = RunCL(
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
                exitCode = RunCL(
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

                exitCode = RunLINK(
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
