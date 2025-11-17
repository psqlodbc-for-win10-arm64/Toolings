using CommandLine;
using LibAmong3.Helpers.PE32;
using System.Linq;

namespace ReadPEImportTable
{
    internal class Program
    {
        private interface IFilterable
        {
            IEnumerable<string>? IncludeSets { get; }
            IEnumerable<string>? ExcludeSets { get; }
            IEnumerable<string>? Include { get; }
            IEnumerable<string>? Exclude { get; }
        }

        [Verb("print-import-dll", HelpText = "Print the imported DLLs from a PE file.")]
        private class PrintImportDllOpt : IFilterable
        {
            [Value(0, Required = true, HelpText = "Path to PE file.")]
            public string FilePath { get; set; } = null!;

            [Option("include-sets", HelpText = "Only include sets of DLLs that match these filters.")]
            public IEnumerable<string> IncludeSets { get; set; } = Array.Empty<string>();

            [Option("exclude-sets", HelpText = "Exclude sets of DLLs that match these filters.")]
            public IEnumerable<string> ExcludeSets { get; set; } = Array.Empty<string>();

            [Option("include", HelpText = "Only include names of DLLs that match these filters.")]
            public IEnumerable<string> Include { get; set; } = Array.Empty<string>();

            [Option("exclude", HelpText = "Exclude names of DLLs that match these filters.")]
            public IEnumerable<string> Exclude { get; set; } = Array.Empty<string>();
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
            var filter = BuildFilterFunc(o);
            var importTable = LoadImportTable(o.FilePath);
            foreach (var entry in importTable
                .Where(entry => filter(entry.DLLName)))
            {
                Console.WriteLine(entry.DLLName);
            }
            return 0;
        }

        private static Func<string, bool> BuildFilterFunc(IFilterable filterable)
        {
            var includeDLLNames = new HashSet<string>(
                new string[0]
                    .Concat(GetDLLNamesListOf(filterable.IncludeSets))
                    .Concat(filterable.Include ?? Enumerable.Empty<string>()),
                StringComparer.OrdinalIgnoreCase
            );
            var excludeDLLNames = new HashSet<string>(
                new string[0]
                    .Concat(GetDLLNamesListOf(filterable.ExcludeSets))
                    .Concat(filterable.Exclude ?? Enumerable.Empty<string>()),
                StringComparer.OrdinalIgnoreCase
            );

            return dllName =>
            {
                if (includeDLLNames.Any())
                {
                    bool included = includeDLLNames.Contains(dllName);
                    if (!included)
                    {
                        return false;
                    }
                }

                if (excludeDLLNames.Contains(dllName))
                {
                    return false;
                }

                return true;
            };
        }

        private static IEnumerable<string> GetDLLNamesListOf(IEnumerable<string>? filters)
        {
            if (filters == null)
            {
                return Enumerable.Empty<string>();
            }
            else
            {
                return new string[0]
                    .Concat(filters.Contains("windows10", StringComparer.OrdinalIgnoreCase) ? DLLNames.Windows10_x64_22H2_19045_6575_SystemDLLs : Enumerable.Empty<string>());
            }
        }
    }
}
