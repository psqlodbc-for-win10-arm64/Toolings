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

            var tmpObjDir = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(tmpObjDir);

            int number = 0;

            string GetTemp(string suffix)
            {
                var path = Path.Combine(
                    tmpObjDir,
                    $"{++number:00000}_{suffix}"
                );
                return path;
            }

            var libArgs = args
                .Select(libParser.Parse)
                .ToArray();

            IEnumerable<string> ProcessLibArg(LibArg arg)
            {
                if (arg.Machine.Length != 0)
                {
                    return [];
                }
                else if (arg.IsObj)
                {
                    if (arg.Value.EndsWith(".obj", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var file = File.ReadAllBytes(arg.Value);
                        var coff = CoffParser.Parse(file, true);
                        if (coff.Magic != 0x14c)
                        {
                            throw new Exception("Not a I386 COFF (ARM64/ARM64EC mixed) file!");
                        }

                        var prefix = GetTemp(Norm(arg.Value));
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
                    var atFile = GetTemp(".tmp");
                    File.WriteAllLines(
                        atFile,
                        File.ReadAllLines(arg.At)
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

        private static string Norm(string path)
            => path
                .Replace("\\", "_")
                .Replace("/", "_")
            ;
    }
}
