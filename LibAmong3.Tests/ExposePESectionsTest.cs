using LibAmong3.Helpers.Guessr;
using LibAmong3.Helpers.PE32;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Tests
{
    public class ExposePESectionsTest
    {
        [Test]
        [TestCase("arm32.dll")]
        [TestCase("arm64.dll")]
        [TestCase("arm64ec.dll")]
        [TestCase("arm64x.dll")]
        [TestCase("x64.dll")]
        [TestCase("x86.dll")]
        public void OnlyExpose(string dllName)
        {
            var bytes = File.ReadAllBytes($@"Files\{dllName}");
            var exposePESections = new ParseHeader();
            exposePESections.Parse(bytes)
                .Sections
                .ToList()
                .ForEach(it => Console.WriteLine(it));
        }

        [Test]
        [TestCase("arm64/ImportExitProcess.exe")]
        [TestCase("arm64/ImportDummy.exe")]
        [TestCase("arm64ec/ImportExitProcess.exe")]
        [TestCase("arm64ec/ImportDummy.exe")]
        [TestCase("arm64x/ImportExitProcess.exe")]
        [TestCase("arm64x/ImportDummy.exe")]
        [TestCase("x64/ImportExitProcess.exe")]
        [TestCase("x64/ImportDummy.exe")]
        [TestCase("x86/ImportExitProcess.exe")]
        [TestCase("x86/ImportDummy.exe")]
        public void ParseImportTableTest(string dllName)
        {
            var bytes = File.ReadAllBytes($@"Files\{dllName}");
            var exposePESections = new ParseHeader();
            var header = exposePESections.Parse(bytes);
            var importTable = header.ImageDataDirectories[1];
            var parseImportTable = new ParseImportTable();
            var directories = parseImportTable.Parse(
                provide: new VAReadOnlyMemoryProvider(
                    bytes,
                    header.Sections
                )
                    .Provide,
                virtualAddress: importTable.VirtualAddress,
                isPE32Plus: header.IsPE32Plus
            );
            foreach (var directory in directories)
            {
                Console.WriteLine($"DLL Name: {directory.DLLName}");

                foreach (var import in directory.ImportRefs)
                {
                    Console.WriteLine($"  Import: {import}");
                }
            }
        }
    }
}
