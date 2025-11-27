using LibAmong3.Helpers.Guessr;
using LibAmong3.Helpers.PE32;
using NUnit.Framework;
using System;
using System.Buffers.Binary;
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
            var bytes = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.WorkDirectory, "Files", dllName));
            var exposePESections = new ParseHeader();
            var header = exposePESections.Parse(bytes);
            var importTable = header.GetImageDirectoryOrEmpty(1);
            var parseImportTable = new ParseImportTable();
            var directories = parseImportTable.Parse(
                provide: new VAReadOnlySpanProvider(
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

        [Test]
        [TestCase("arm64/ExportDummy.dll")]
        [TestCase("arm64ec/ExportDummy.dll")]
        [TestCase("arm64x/ExportDummy.dll")]
        [TestCase("x64/ExportDummy.dll")]
        [TestCase("x86/ExportDummy.dll")]
        public void ParseExportTableTest(string dllName)
        {
            var bytes = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.WorkDirectory, "Files", dllName));
            var exposePESections = new ParseHeader();
            var header = exposePESections.Parse(bytes);
            var exportTable = header.GetImageDirectoryOrEmpty(0);
            var parseExportTable = new ParseExportTable();
            var provider = new VAReadOnlySpanProvider(
                bytes,
                header.Sections
            );
            var directory = parseExportTable.Parse(
                provide: provider.Provide,
                virtualAddress: exportTable.VirtualAddress,
                isPE32Plus: header.IsPE32Plus
            );
            {
                Console.WriteLine($"DLL Name: {directory.DLLName}");
                {
                    foreach (var (export, ordinal) in directory.ExportNames.Zip(directory.OrdinalTable))
                    {
                        Console.WriteLine($"  Export: {export} (Ordinal: {directory.BaseOrdinal + ordinal})");
                    }
                }
            }
        }

        [Test]
        [TestCase("arm64ec/ExportDummy.dll")]
        [TestCase("arm64x/ExportDummy.dll")]
        [TestCase("x64/ExportDummy.dll")]
        [TestCase("x86/ExportDummy.dll")]
        //[TestCase(@"V:\psqlodbc-for-win10-arm64\openssl-arm64x-release-2-inst\Program Files\OpenSSL\bin\libssl-3-arm64.dll")]
        //[TestCase(@"C:\BUFFALO\kokiinst_200\Win\Launcher.exe")] // 0x014C,72
        //[TestCase(@"C:\Program Files (x86)\Common Files\Microsoft Shared\Phone Tools\12.0\Debugger\target\x86\clrcompression.dll")] // 0x014C,92
        //[TestCase(@"C:\Program Files (x86)\Common Files\Microsoft Shared\Phone Tools\14.0\Debugger\target\x86\dxcap.exe")] // 0x014C,104
        //[TestCase(@"C:\Windows\SysWOW64\mrt100.dll")] // 0x014C,124
        //[TestCase(@"C:\Program Files (x86)\Windows Kits\10\bin\10.0.14393.0\x86\GenXBF.dll")] // 0x014C,128
        //[TestCase(@"C:\Program Files (x86)\Common Files\Microsoft Shared\Windows Phone Sirep\8.1\SirepClient.dll")] // 0x014C,152
        //[TestCase(@"C:\BUFFALO\kokiinst_200\Win\driver\U3866D\Win10X86\RTUWPSrvcLib.dll")] // 0x014C,160
        //[TestCase(@"C:\Program Files (x86)\Common Files\Adobe\ARM\1.0\AdobeARM.exe")] // 0x014C,164
        //[TestCase(@"C:\Program Files (x86)\Common Files\Microsoft Shared\Filters\tifffilt.dll")] // 0x014C,172
        //[TestCase(@"C:\Program Files (x86)\DB Browser for SQLite\Qt5Concurrent.dll")] // 0x014C,184
        //[TestCase(@"C:\Program Files (x86)\Adobe\Acrobat Reader DC\Reader\acrocef_3\RdrCEF.exe")] // 0x014C,188
        //[TestCase(@"C:\Program Files (x86)\Common Files\MariaDBShared\HeidiSQL\libmariadb.dll")] // 0x014C,192
        //[TestCase(@"C:\Program Files (x86)\Application Verifier\vrfauto.dll")] // 0x014C,196
        //[TestCase(@"C:\BUFFALO\kokiinst_200\Win\driver\U2866D\Win8\WdfCoInstaller01011920064.dll")] // 0x8664,112
        //[TestCase(@"H:\Tmp\Windows NT Workstation 3.5\I386\kernel32.dll")] // 0x014C,0,64
        public void ParseLoadConfigDirTest(string dllName)
        {
            var bytes = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.WorkDirectory, "Files", dllName));
            var exposePESections = new ParseHeader();
            var header = exposePESections.Parse(bytes);
            var loadConfigDir = header.GetImageDirectoryOrEmpty(10);
            var parser = new ParseLoadConfigDir();
            var provider = new VAReadOnlySpanProvider(
                bytes,
                header.Sections
            );
            File.WriteAllBytes(Path.GetFileName(dllName.Replace("/", "_")) + ".bin", provider.Provide(loadConfigDir.VirtualAddress, loadConfigDir.Size).ToArray());
            var parsed = parser.Parse(
                provide: provider.Provide,
                virtualAddress: loadConfigDir.VirtualAddress,
                isPE32Plus: header.IsPE32Plus
            );
            Console.WriteLine(parsed);

            if (parsed.Header1.CHPEMetadataPointer != 0)
            {
                var that = provider.Provide(Convert.ToInt32(parsed.Header1.CHPEMetadataPointer - header.ImageBase), 4).ToArray();
                Console.WriteLine(BitConverter.ToString(that).Replace("-", " "));
            }
        }

        [Test]
        [Ignore("Private use")]
        public void Scan()
        {
            void ScanDir(string dir)
            {
                foreach (var subDir in Directory.GetDirectories(dir))
                {
                    try
                    {
                        ScanDir(subDir);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[!] Could not access directory: {subDir}: {ex.Message}");
                    }
                }

                foreach (var file in Directory.GetFiles(dir)
                    .Where(
                        name => false
                            || name.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)
                            || name.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase)
                            || name.EndsWith(".ocx", StringComparison.InvariantCultureIgnoreCase)
                    )
                )
                {
                    try
                    {
                        if (1024 * 1024 * 256 <= new FileInfo(file).Length)
                        {
                            continue;
                        }

                        var bytes = File.ReadAllBytes(file);
                        var exposePESections = new ParseHeader();
                        var header = exposePESections.Parse(bytes);
                        var loadConfigDir = header.GetImageDirectoryOrEmpty(10);
                        if (4 <= loadConfigDir.Size)
                        {
                            var provider = new VAReadOnlySpanProvider(
                                bytes,
                                header.Sections
                            );
                            var size = BinaryPrimitives.ReadInt32LittleEndian(provider.Provide(loadConfigDir.VirtualAddress, 4));
                            Console.WriteLine($"0x{header.Machine:X4},{size},{loadConfigDir.Size},\"{file}\"");
                        }
                    }
                    catch
                    {
                        // Ignore errors
                    }
                }
            }

            ScanDir(@"H:\Tmp\Windows XP SP2 JA [OEM]");
        }
    }
}