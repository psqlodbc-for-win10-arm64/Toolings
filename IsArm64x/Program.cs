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
            var result = new GuessArm64XBinaryHelper().Guess(bytes);
            Console.WriteLine(result);
            return 0;
        }
    }
}
