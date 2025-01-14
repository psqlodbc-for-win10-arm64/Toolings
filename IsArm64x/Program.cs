using CommandLine;
using System.Diagnostics;

namespace IsArm64x
{
    internal class Program
    {
        private class Opt
        {
            [Value(0, MetaName = "Input", HelpText = "Specify a file path to DLL/EXE", Required = true)]
            public string? Input { get; set; }
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Opt>(args)
                .MapResult<Opt, int>(
                    Do,
                    ex => 1
                );
        }

        private static int Do(Opt opt)
        {
            var psi = new ProcessStartInfo(
                @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\dumpbin.exe",
                $" /headers \"{opt.Input}\" "
            )
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            var p = Process.Start(psi) ?? throw new System.Exception("Failed to start process");
            var stdOutAsync = p.StandardOutput.ReadToEndAsync();
            p.WaitForExit();
            var stdOut = stdOutAsync.Result;
            if (false) { }
            else if (stdOut.Contains("AA64 machine (ARM64) (ARM64X)"))
            {
                Console.WriteLine("ARM64X");
                return 0;
            }
            else
            {
                return 1;
            }
        }
    }
}
