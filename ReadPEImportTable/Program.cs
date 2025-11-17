using CommandLine;
using LibAmong3.Helpers.PE32;

namespace ReadPEImportTable
{
    internal class Program
    {
        [Verb("print-import-dll", HelpText = "Print the imported DLLs from a PE file.")]
        private class PrintImportDllOpt
        {
            [Value(0, Required = true, HelpText = "Path to PE file.")]
            public string FilePath { get; set; } = null!;
        }

        [Verb("print-import-entries", HelpText = "Print the imported entries from a PE file.")]
        private class PrintImportEntriesOpt
        {
            [Value(0, Required = true, HelpText = "Path to PE file.")]
            public string FilePath { get; set; } = null!;
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<PrintImportDllOpt, PrintImportEntriesOpt>(args)
                .MapResult<PrintImportDllOpt, PrintImportEntriesOpt, int>(
                    DoPrintImportDll,
                    DoPrintImportEntries,
                    ex => 1
                );
        }

        private static IReadOnlyList<PEIData.Directory> LoadImportTable(string filePath)
        {
            var exe = File.ReadAllBytes(filePath);
            var header = new ParseHeader().Parse(exe);
            var importTable = new ParseImportTable().Parse(
                provide: new VAReadOnlyMemoryProvider(
                    Exe: exe,
                    Sections: header.Sections
                )
                    .Provide,
                virtualAddress: header.ImageDataDirectories[1].VirtualAddress,
                isPE32Plus: header.IsPE32Plus
            );
            return importTable;
        }

        private static int DoPrintImportEntries(PrintImportEntriesOpt o)
        {
            var importTable = LoadImportTable(o.FilePath);
            foreach (var entry in importTable)
            {
                foreach (var func in entry.ImportRefs)
                {
                    Console.WriteLine($"{entry.DLLName},{(func.IsOrdinal ? $"{func.Ordinal}" : func.Name)}");
                }
            }
            return 0;
        }

        private static int DoPrintImportDll(PrintImportDllOpt o)
        {
            var importTable = LoadImportTable(o.FilePath);
            foreach (var entry in importTable)
            {
                Console.WriteLine(entry.DLLName);
            }
            return 0;
        }
    }
}
