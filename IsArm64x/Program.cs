using CommandLine;
using LibAmong3.Helpers.Guessr;
using System.Diagnostics;

namespace IsArm64x
{
    internal class Program
    {
        private class Opt
        {
            [Value(0, MetaName = "Input", HelpText = "Specify a file path to DLL/EXE", Required = true)]
            public string? Input { get; set; }

            [Option('d', "disable-arm64x-detection", HelpText = "Disable detection of Arm64X binaries.")]
            public bool DisableArm64XDetection { get; set; }

            [Option('p', "look-at-chpe-metadata-version", HelpText = "Look at CHPE metadata version to help determine the binary form.")]
            public bool LookAtCHPEMetadataVersion { get; set; }

            [Option('v', "look-at-dvrt-header", HelpText = "Look at DVRT header to help determine the binary form.")]
            public bool LookAtDvrtHeader { get; set; }
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
            var bytes = File.ReadAllBytes(opt.Input ?? throw new ArgumentNullException("opt.Input"));
            var result = new GuessArm64XBinaryHelper().Guess(
                bytes,
                new Guess1Options
                {
                    DisableArm64XDetection = opt.DisableArm64XDetection,
                    LookAtCHPEMetadataVersion = opt.LookAtCHPEMetadataVersion,
                    LookAtDvrtHeader = opt.LookAtDvrtHeader,
                }
            );
            Console.WriteLine(result);
            return 0;
        }
    }
}
