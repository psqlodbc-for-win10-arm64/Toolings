using CommandLine;
using LibAmong3.Helpers.PE32;
using System.Linq;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ReadPEExportTable
{
    internal class Program
    {
        private interface IOutputToFile
        {
            string? OutputToFile { get; }
        }

        [Verb("print-export", HelpText = "Print the export table from a PE file.")]
        private class PrintExportDllOpt : IOutputToFile
        {
            [Value(0, Required = true, HelpText = "Path to PE file.")]
            public string FilePath { get; set; } = null!;

            [Option('o', "output-to-file", HelpText = "Output the result to a file.")]
            public string? OutputToFile { get; set; }
        }

        [Verb("print-def", HelpText = "Print the export table as a .def file from a PE file.")]
        private class PrintDefDllOpt : IOutputToFile
        {
            [Value(0, Required = true, HelpText = "Path to PE file.")]
            public string FilePath { get; set; } = null!;

            [Option('o', "output-to-file", HelpText = "Output the result to a file.")]
            public string? OutputToFile { get; set; }

            [Option('a', "append-ordinal", HelpText = "Append ordinal to export names.")]
            public bool AppendOrdinal { get; set; }

            [Option('n', "include-hidden", HelpText = "Include only exports that are by ordinal.")]
            public bool IncludeHidden { get; set; }

            [Option("no-forwarders", HelpText = "Do not include forwarders in the output.")]
            public bool NoForwarders { get; set; }
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<PrintExportDllOpt, PrintDefDllOpt>(args)
                .MapResult<PrintExportDllOpt, PrintDefDllOpt, int>(
                    DoPrintExportDll,
                    DoPrintDefDll,
                    ex => 1
                );
        }

        private static int DoPrintDefDll(PrintDefDllOpt opt)
        {
            using var writer = GetWriter(opt);
            var loaded = LoadDirectory(opt.FilePath);
            if (loaded != null)
            {
                writer.WriteLine($"LIBRARY {Path.GetFileNameWithoutExtension(loaded.Directory.DLLName)}");
                writer.WriteLine("EXPORTS");

                foreach (var entry in ComposeExportEntries(loaded, opt.IncludeHidden, opt.NoForwarders))
                {
                    writer.WriteLine(string.Concat(
                        $" {entry.ExportName}",
                        (entry.Forwarder.Length != 0)
                            ? $"={entry.Forwarder}"
                            : "",
                        (opt.AppendOrdinal)
                            ? $" @{entry.Ordinal}"
                            : ""
                    ));
                }
            }

            return 0;
        }

        private static int DoPrintExportDll(PrintExportDllOpt opt)
        {
            using var writer = GetWriter(opt);
            var loaded = LoadDirectory(opt.FilePath);
            if (loaded != null)
            {
                writer.WriteLine($"DLL Name: {loaded.Directory.DLLName}");

                foreach (var entry in ComposeExportEntries(loaded))
                {
                    writer.WriteLine($"  Export: {entry.ExportName} (Ordinal: {entry.Ordinal})");
                }
            }

            return 0;
        }

        private record ExportEntry(string ExportName, uint Ordinal, string Forwarder);

        private delegate string GetForwarderFromRVADelegate(uint rva);

        private static IEnumerable<ExportEntry> ComposeExportEntries(
            LoadedDirectory loaded,
            bool includeHidden = false,
            bool noForwarders = false
        )
        {
            var directory = loaded.Directory;
            var getForwarderFromRVA = noForwarders ? null : loaded.GetForwarderFromRVA;

            var revOrdinalTable = directory.OrdinalTable
                .Select((ord, idx) => (ord, idx))
                .ToDictionary(pair => pair.ord, pair => pair.idx);

            return Enumerable.Range(0, directory.AddrTable.Count)
                .Select(
                    index =>
                    {
                        if (revOrdinalTable.TryGetValue((uint)index, out var symbolIdx))
                        {
                            return new ExportEntry(
                                ExportName: directory.ExportNames[symbolIdx],
                                Ordinal: (uint)(directory.BaseOrdinal + index),
                                Forwarder: getForwarderFromRVA?.Invoke(directory.AddrTable[index]) ?? ""
                            );
                        }
                        else if (includeHidden)
                        {
                            return new ExportEntry(
                                ExportName: $"Ordinal_{directory.BaseOrdinal + index}",
                                Ordinal: (uint)(directory.BaseOrdinal + index),
                                Forwarder: getForwarderFromRVA?.Invoke(directory.AddrTable[index]) ?? ""
                            );
                        }
                        else
                        {
                            return null;
                        }
                    }
                )
                .OfType<ExportEntry>();
        }

        private static TextWriter GetWriter(IOutputToFile opt)
        {
            if (opt.OutputToFile != null)
            {
                return new StreamWriter(
                    path: opt.OutputToFile,
                    append: false,
                    encoding: Encoding.Latin1
                );
            }
            else
            {
                return Console.Out;
            }
        }

        private record LoadedDirectory(
            PEEData.Directory Directory,
            GetForwarderFromRVADelegate? GetForwarderFromRVA
        );

        private static LoadedDirectory? LoadDirectory(string filePath)
        {
            var dll = File.ReadAllBytes(filePath);
            var header = new ParseHeader().Parse(dll);
            var exportTable = header.GetImageDirectoryOrEmpty(0);
            if (exportTable == PEImageDataDirectory.Empty)
            {
                return null;
            }
            var parseExportTable = new ParseExportTable();
            var provider = new VAReadOnlySpanProvider(
                dll,
                header.Sections
            );
            var directory = parseExportTable.Parse(
                provide: provider.Provide,
                virtualAddress: exportTable.VirtualAddress,
                isPE32Plus: header.IsPE32Plus
            );
            string GetForwarderFromRVA(uint rva)
            {
                if (true
                    && exportTable.VirtualAddress <= rva
                    && rva < exportTable.VirtualAddress + exportTable.Size
                )
                {
                    return ReadCStringHelper.ReadCString(
                        provide: provider.Provide,
                        rva: (int)rva
                    );
                }
                else
                {
                    return "";
                }
            }
            return new LoadedDirectory(directory, GetForwarderFromRVA);
        }
    }
}
