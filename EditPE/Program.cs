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

        [Verb("export-dvrt", HelpText = "Export the Dynamic value relocation table to a new file")]
        private class ExportDvrtOpt
        {
            [Value(0, Required = true, MetaName = "PEFileLoadFrom")]
            public string PEFileLoadFrom { get; set; } = null!;

            [Value(1, Required = true, MetaName = "DvrtSaveTo")]
            public string DvrtSaveTo { get; set; } = null!;
        }

        [Verb("import-dvrt", HelpText = "Import the Dynamic value relocation table from a file")]
        private class ImportDvrtOpt
        {
            [Value(0, Required = true, MetaName = "PEFileLoadFrom")]
            public string PEFileLoadFrom { get; set; } = null!;

            [Value(1, Required = true, MetaName = "DvrtLoadFrom")]
            public string DvrtLoadFrom { get; set; } = null!;

            [Value(2, Required = true, MetaName = "PEFileSaveTo")]
            public string PEFileSaveTo { get; set; } = null!;
        }

        [Verb("print-sections", HelpText = "Print sections from a PE file")]
        private class PrintSectionsOpt
        {
            [Value(0, Required = true, MetaName = "PEFileLoadFrom")]
            public string PEFileLoadFrom { get; set; } = null!;
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<PrintDvrtPatchOpt, ApplyDvrtOpt, NullifyDvrtOpt, ExportDvrtOpt, ImportDvrtOpt, PrintSectionsOpt>(args)
                .MapResult<PrintDvrtPatchOpt, ApplyDvrtOpt, NullifyDvrtOpt, ExportDvrtOpt, ImportDvrtOpt, PrintSectionsOpt, int>(
                    DoPrintDvrtPatch,
                    DoApplyDvrt,
                    DoNullifyDvrt,
                    DoExportDvrt,
                    DoImportDvrt,
                    DoPrintSections,
                    errs => 1
                );
        }

        private static void PrintPESections(IEnumerable<PESection> sections)
        {
            {
                Console.WriteLine("Name     | VirtualAddress      | AtFile   | Size     ");
                Console.WriteLine("---------|---------------------|----------|----------");
            }
            foreach (var sect in sections)
            {
                Console.WriteLine("{0,-8} | {1:X8} - {2:X8} | {3:X8} | {4:X8} "
                    , sect.Name
                    , sect.VirtualAddress
                    , sect.VirtualAddress + sect.SizeOfRawData - 1
                    , sect.PointerToRawData
                    , sect.SizeOfRawData
                    );
            }
        }

        private static int DoPrintSections(PrintSectionsOpt opt)
        {
            var pe = File.ReadAllBytes(opt.PEFileLoadFrom);

            var parseHeader = new ParseHeader();
            var header = parseHeader.Parse(pe);

            PrintPESections(header.Sections);

            return 0;
        }

        private static int DoImportDvrt(ImportDvrtOpt opt)
        {
            var pe = File.ReadAllBytes(opt.PEFileLoadFrom).AsMemory();

            var dvrt = File.ReadAllBytes(opt.DvrtLoadFrom);

            var lookAtLoadConfig = LookAtLoadConfig1.Create(pe);
            if (lookAtLoadConfig != null)
            {
                var parseHeader = new ParseHeader();
                var header = parseHeader.Parse(pe);

                PrintPESections(header.Sections);

                Console.WriteLine();

                var provider = new VAReadOnlySpanProvider(
                    pe,
                    header.Sections
                );
                var patchableVASpanProvider = new PatchableVASpanProvider(provider.Provide);

                var result = lookAtLoadConfig.ApplyDvrt(patchableVASpanProvider);

                Console.WriteLine("Info: Current DVRT is ranging rva from {0:X8} to {1:X8} ({2} bytes)", result.RvaStart, result.RvaEnd - 1, dvrt.Length);

                var sectIdx = Array.FindIndex(header.Sections.ToArray(), it => it.VirtualAddress <= result.RvaStart && result.RvaStart < it.VirtualAddress + it.SizeOfRawData);
                if (sectIdx < 0)
                {
                    Console.Error.WriteLine("Error: DVRT is not located in any of listed sections!");
                    throw new Exception();
                }
                var sectAt = header.Sections[sectIdx];
                Console.WriteLine("Info: Current DVRT is located at section \"{0}\"", sectAt.Name);

                {
                    var bytesToAdd = dvrt.Length;

                    var editSectionHelper = new EditSectionHelper();

                    if (editSectionHelper.TryToGlowSection(pe, sectIdx, bytesToAdd) is var pair && pair != null)
                    {
                        // acquired

                        Console.WriteLine("Info: Glow the existing section \"{0}\", and then write to the appended space.", sectAt.Name);
                    }
                    else
                    {
                        pair = editSectionHelper.AddNewSection(pe, ".sect1", bytesToAdd);

                        Console.WriteLine("Info: Add new section \"{0}\", and then write to there.", ".sect1");
                    }

                    pe = pair.Value.PeMod;
                    dvrt.CopyTo(pe.Slice(pair.Value.PointerToWrite));
                }

                Console.WriteLine();

                {
                    var headerAfter = parseHeader.Parse(pe);

                    PrintPESections(headerAfter.Sections);
                }

                using (var stream = File.Create(opt.PEFileSaveTo))
                {
                    stream.Write(pe.Span);
                }
                return 0;
            }
            else
            {
                Console.Error.WriteLine("Error: loadConfigDirEntry not found!");
                return 1;
            }
        }

        private static int DoExportDvrt(ExportDvrtOpt opt)
        {
            var pe = File.ReadAllBytes(opt.PEFileLoadFrom);

            var lookAtLoadConfig = LookAtLoadConfig1.Create(pe);
            if (lookAtLoadConfig != null)
            {
                var parseHeader = new ParseHeader();
                var header = parseHeader.Parse(pe);
                var provider = new VAReadOnlySpanProvider(
                    pe,
                    header.Sections
                );
                var patchableVASpanProvider = new PatchableVASpanProvider(provider.Provide);

                var result = lookAtLoadConfig.ApplyDvrt(patchableVASpanProvider);

                var dvrt = provider.Provide(result.RvaStart, result.RvaEnd - result.RvaStart);

                Console.Error.WriteLine("Info: DVRT rva from {0:X8} to {1:X8} ({2} bytes)", result.RvaStart, result.RvaEnd - 1, dvrt.Length);

                using (var stream = File.Create(opt.DvrtSaveTo))
                {
                    stream.Write(dvrt);
                }
                return 0;
            }
            else
            {
                Console.Error.WriteLine("Error: loadConfigDirEntry not found!");
                return 1;
            }
        }

        private static int DoNullifyDvrt(NullifyDvrtOpt opt)
        {
            var exe = File.ReadAllBytes(opt.PEFileLoadFrom);

            var ok = new ModifyDvrtPointerHelper().ModifyDvrtPointer(
                exe: exe,
                offset: 0,
                section: 0,
                logWarn: s => Console.Error.WriteLine("Warn: {0}", s),
                logError: s => Console.Error.WriteLine("Error: {0}", s)
            );

            if (ok)
            {
                File.WriteAllBytes(opt.PEFileSaveTo, exe);
                return 0;
            }
            else
            {
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
