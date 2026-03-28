using CommandLine;
using LibAmong3.Helpers.PE32;
using LibAmong3.Helpers.PE32.Deeper;
using System.Buffers.Binary;
using System.Reflection.PortableExecutable;

namespace EditPE
{
    internal class Program
    {
        [Verb("apply-dvrt", HelpText = "Apply the Dynamic value relocation table to the new PE file")]
        private class ApplyDvrtOpt
        {
            [Value(0, Required = true, MetaName = "PEFileLoadFrom")]
            public string PEFileLoadFrom { get; set; } = null!;

            [Value(1, Required = true, MetaName = "PEFileSaveTo")]
            public string PEFileSaveTo { get; set; } = null!;
        }

        [Verb("print-dvrt-patch", HelpText = "Print out the patched bytes applied from Dynamic value relocation table")]
        private class PrintDvrtPatchOpt
        {
            [Value(0, Required = true, MetaName = "PEFile")]
            public string PEFile { get; set; } = null!;

            [Option('f', "form", HelpText = "empty, `bp`, `bp1`")]
            public string? Form { get; set; }
        }

        [Verb("nullify-dvrt", HelpText = "Nullify the pointer to the Dynamic value relocation table")]
        private class NullifyDvrtOpt
        {
            [Value(0, Required = true, MetaName = "PEFileLoadFrom")]
            public string PEFileLoadFrom { get; set; } = null!;

            [Value(1, Required = true, MetaName = "PEFileSaveTo")]
            public string PEFileSaveTo { get; set; } = null!;
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<PrintDvrtPatchOpt, ApplyDvrtOpt, NullifyDvrtOpt>(args)
                .MapResult<PrintDvrtPatchOpt, ApplyDvrtOpt, NullifyDvrtOpt, int>(
                    DoPrintDvrtPatch,
                    DoApplyDvrt,
                    DoNullifyDvrt,
                    errs => 1
                );
        }

        private static int DoNullifyDvrt(NullifyDvrtOpt opt)
        {
            var exe = File.ReadAllBytes(opt.PEFileLoadFrom);

            var parseHeader = new ParseHeader();
            var header = parseHeader.Parse(exe);
            var loadConfigDirEntry = header.GetImageDirectoryOrEmpty(10);
            if (loadConfigDirEntry.Size != 0)
            {
                var parseLoadConfigDir = new ParseLoadConfigDir();
                var provider = new VAReadOnlySpanProvider(
                    exe,
                    header.Sections
                );

                var isPE32Plus = header.IsPE32Plus;
                var virtualAddress = loadConfigDirEntry.VirtualAddress;

                var size = BinaryPrimitives.ReadInt32LittleEndian(provider.Provide(virtualAddress, 4));
                var sizeFixedUp = (!isPE32Plus && size == 0)
                    ? 64
                    : size;

                // GuardRFFailureRoutineFunctionPointer      | 136 | 224
                // DynamicValueRelocTableOffset              | 140 | 228
                // DynamicValueRelocTableSection             | 142 | 230

                if (isPE32Plus)
                {
                    if (230 <= sizeFixedUp)
                    {
                        var pair = provider.Locate(virtualAddress + 224, 6);
                        exe.AsMemory(pair.Start, pair.Length).Span.Clear();
                    }
                    else
                    {
                        Console.Error.WriteLine("Warn: DynamicValueRelocTableOffset and Section isn't included.");
                    }
                }
                else
                {
                    if (142 <= sizeFixedUp)
                    {
                        var pair = provider.Locate(virtualAddress + 136, 6);
                        exe.AsMemory(pair.Start, pair.Length).Span.Clear();
                    }
                    else
                    {
                        Console.Error.WriteLine("Warn: DynamicValueRelocTableOffset and Section isn't included.");
                    }
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

        private static int DoApplyDvrt(ApplyDvrtOpt opt)
        {
            var exe = File.ReadAllBytes(opt.PEFileLoadFrom);
            if (new ApplyDvrtHelper().ApplyDvrt(exe))
            {
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
