using CommandLine;
using LibAmong3.Helpers.PE32;
using LibAmong3.Helpers.PE32.Deeper;
using System.Reflection.PortableExecutable;

namespace EditPE
{
    internal class Program
    {
        [Verb("apply-dvrt")]
        private class ApplyDvrtOpt
        {
            [Value(0, Required = true, MetaName = "PEFileLoadFrom")]
            public string PEFileLoadFrom { get; set; } = null!;

            [Value(1, Required = true, MetaName = "PEFileSaveTo")]
            public string PEFileSaveTo { get; set; } = null!;
        }

        [Verb("print-dvrt-patch")]
        private class PrintDvrtPatchOpt
        {
            [Value(0, Required = true, MetaName = "PEFile")]
            public string PEFile { get; set; } = null!;

            [Option('f', "form", HelpText = "empty, `bp`, `bp1`")]
            public string? Form { get; set; }
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<PrintDvrtPatchOpt, ApplyDvrtOpt>(args)
                .MapResult<PrintDvrtPatchOpt, ApplyDvrtOpt, int>(
                    DoPrintDvrtPatch,
                    DoApplyDvrt,
                    errs => 1
                );
        }

        private static int DoApplyDvrt(ApplyDvrtOpt opt)
        {
            var exe = File.ReadAllBytes(opt.PEFileLoadFrom);
            var lookAtLoadConfig = LookAtLoadConfig1.Create(exe);
            if (lookAtLoadConfig != null)
            {
                var parseHeader = new ParseHeader();
                var header = parseHeader.Parse(exe);
                var provider = new VAReadOnlySpanProvider(
                    exe,
                    header.Sections
                );
                var patchableVASpanProvider = new PatchableVASpanProvider(provider.Provide);

                lookAtLoadConfig.ApplyDvrt(patchableVASpanProvider);

                foreach (var one in patchableVASpanProvider.PatchRecords)
                {
                    var pair = provider.Locate(one.Rva, one.Bytes.Length);

                    one.Bytes.CopyTo(exe.AsMemory(pair.Start, pair.Length));
                }

                File.WriteAllBytes(opt.PEFileSaveTo, exe);
                return 0;
            }
            else
            {
                Console.Error.WriteLine("Error: loadConfigDirEntry not found!");
                return 1;
            }
        }

        private static int DoPrintDvrtPatch(PrintDvrtPatchOpt opt)
        {
            var exe = File.ReadAllBytes(opt.PEFile);
            var lookAtLoadConfig = LookAtLoadConfig1.Create(exe);
            if (lookAtLoadConfig != null)
            {
                var parseHeader = new ParseHeader();
                var header = parseHeader.Parse(exe);
                var provider = new VAReadOnlySpanProvider(
                    exe,
                    header.Sections
                );
                var patchableVASpanProvider = new PatchableVASpanProvider(provider.Provide);

                lookAtLoadConfig.ApplyDvrt(patchableVASpanProvider);

                if (string.IsNullOrEmpty(opt.Form))
                {
                    PrintRawStyle();
                }
                else if (opt.Form == "bp")
                {
                    PrintBytePatchStyle(printUnchangedBytesToo: false);
                }
                else if (opt.Form == "bp1")
                {
                    PrintBytePatchStyle(printUnchangedBytesToo: true);
                }

                void PrintRawStyle()
                {
                    foreach (var one in patchableVASpanProvider.PatchRecords)
                    {
                        Console.WriteLine($"{one.Rva:X8}  {BitConverter.ToString(one.Bytes.ToArray()).Replace("-", " ")}");
                    }
                }

                void PrintBytePatchStyle(bool printUnchangedBytesToo)
                {
                    var rvaToNewValue = new Dictionary<long, byte>();

                    foreach (var one in patchableVASpanProvider.PatchRecords)
                    {
                        var bytes = one.Bytes.ToArray();

                        for (int x = 0, cx = bytes.Length; x < cx; x++)
                        {
                            rvaToNewValue[one.Rva + x] = bytes[x];
                        }
                    }

                    foreach (var pair in rvaToNewValue)
                    {
                        var before = provider.Provide(Convert.ToInt32(pair.Key), 1)[0];
                        var after = pair.Value;
                        if (printUnchangedBytesToo || before != after)
                        {
                            Console.WriteLine($"{pair.Key:X8} {before:X2} {after:X2}");
                        }
                    }
                }

                return 0;
            }
            else
            {
                Console.Error.WriteLine("Error: loadConfigDirEntry not found!");
                return 1;
            }
        }
    }
}
