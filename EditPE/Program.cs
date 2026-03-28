using CommandLine;
using LibAmong3.Helpers.PE32;
using LibAmong3.Helpers.PE32.Deeper;
using LibAmong3.Helpers.PE32.DvrtModel1;
using System;
using System.Buffers.Binary;
using System.Drawing;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Xml.Serialization;

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

        [Verb("dvrt-to-xml", HelpText = "Convert dvrt binary into XML")]
        private class DvrtToXmlOpt
        {
            [Value(0, Required = true, MetaName = "DvrtLoadFrom")]
            public string DvrtLoadFrom { get; set; } = null!;

            [Value(1, Required = true, MetaName = "XmlSaveTo")]
            public string XmlSaveTo { get; set; } = null!;
        }

        [Verb("xml-to-dvrt", HelpText = "Convert dvrt binary from XML")]
        private class XmlToDvrtOpt
        {
            [Value(0, Required = true, MetaName = "XmlLoadFrom")]
            public string XmlLoadFrom { get; set; } = null!;

            [Value(1, Required = true, MetaName = "DvrtSaveTo")]
            public string DvrtSaveTo { get; set; } = null!;
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<PrintDvrtPatchOpt, ApplyDvrtOpt, NullifyDvrtOpt, ExportDvrtOpt, ImportDvrtOpt, PrintSectionsOpt, DvrtToXmlOpt, XmlToDvrtOpt>(args)
                .MapResult<PrintDvrtPatchOpt, ApplyDvrtOpt, NullifyDvrtOpt, ExportDvrtOpt, ImportDvrtOpt, PrintSectionsOpt, DvrtToXmlOpt, XmlToDvrtOpt, int>(
                    DoPrintDvrtPatch,
                    DoApplyDvrt,
                    DoNullifyDvrt,
                    DoExportDvrt,
                    DoImportDvrt,
                    DoPrintSections,
                    DoDvrtToXml,
                    DoXmlToDvrt,
                    errs => 1
                );
        }

        private static int DoXmlToDvrt(XmlToDvrtOpt opt)
        {
            var xs = new XmlSerializer(typeof(DynamicRelocations));

            var root = (DynamicRelocations)(xs.Deserialize(
                new MemoryStream(
                    File.ReadAllBytes(opt.XmlLoadFrom)
                )
            ) ?? throw new Exception());

            var dvrt = BuildDynamicRelocations(root);

            File.WriteAllBytes(opt.DvrtSaveTo, dvrt);
            return 0;

            byte[] BuildDynamicRelocations(DynamicRelocations dynamicRelocations)
            {
                var arm64xBin = (dynamicRelocations.Arm64X is Arm64X arm64X)
                    ? BuildArm64X(arm64X)
                    : Array.Empty<byte>();

                var buf = new byte[4 + 4 + arm64xBin.Length + 4 + 4];
                var span = buf.AsSpan();
                BinaryPrimitives.WriteInt32LittleEndian(span, 1);
                BinaryPrimitives.WriteInt32LittleEndian(span.Slice(4), arm64xBin.Length);
                arm64xBin.CopyTo(span.Slice(4 + 4));
                return buf;
            }

            byte[] BuildArm64X(Arm64X arm64X)
            {
                var ms = new MemoryStream();
                foreach (var group in arm64X.Group ?? Array.Empty<Group>())
                {
                    var groupBin = BuildGroup(group);
                    ms.Write(groupBin);
                }

                var inLen = Align4(Convert.ToInt32(ms.Length));
                var buf = new byte[8 + 4 + inLen];
                var span = buf.AsSpan();
                BinaryPrimitives.WriteInt64LittleEndian(span, 6);
                BinaryPrimitives.WriteInt32LittleEndian(span.Slice(8), inLen);
                ms.ToArray().CopyTo(span.Slice(8 + 4));
                return buf;
            }

            byte[] BuildGroup(Group group)
            {
                var ms = new MemoryStream();
                foreach (var entry in group.Entry ?? Array.Empty<Entry>())
                {
                    var entryBin = BuildEntry(entry);
                    ms.Write(entryBin);
                }

                var inLen = Align4(Convert.ToInt32(ms.Length));
                var buf = new byte[4 + 4 + inLen];
                var span = buf.AsSpan();
                BinaryPrimitives.WriteInt32LittleEndian(span, ToInt32(group.Rva ?? "0"));
                BinaryPrimitives.WriteInt32LittleEndian(span.Slice(4), inLen + 8);
                ms.ToArray().CopyTo(span.Slice(4 + 4));
                return buf;
            }

            byte[] BuildEntry(Entry entry)
            {
                switch (entry.Type)
                {
                    case "ZEROFILL":
                        {
                            var offset = ToInt32(entry.Offset ?? "0");
                            var size = ToInt32(entry.Size ?? "0");

                            if (offset < 0 || 4095 < offset)
                            {
                                throw new ArgumentOutOfRangeException("offset");
                            }

                            ushort word = (ushort)offset;

                            switch (size)
                            {
                                case 1:
                                    break;
                                case 2:
                                    word |= 0x4000;
                                    break;
                                case 4:
                                    word |= 0x8000;
                                    break;
                                case 8:
                                    word |= 0xC000;
                                    break;
                                default:
                                    throw new ArgumentException("size");
                            }

                            var buf = new byte[2];
                            BinaryPrimitives.WriteUInt16LittleEndian(buf, word);
                            return buf;
                        }
                    case "VALUE":
                        {
                            var offset = ToInt32(entry.Offset ?? "0");
                            var size = ToInt32(entry.Size ?? "0");
                            var data = ToBytes(entry.Value ?? "");

                            if (offset < 0 || 4095 < offset)
                            {
                                throw new ArgumentOutOfRangeException("offset");
                            }

                            ushort word = (ushort)(0x1000 | offset);

                            switch (size)
                            {
                                case 1:
                                    break;
                                case 2:
                                    word |= 0x4000;
                                    break;
                                case 4:
                                    word |= 0x8000;
                                    break;
                                case 8:
                                    word |= 0xC000;
                                    break;
                                default:
                                    throw new ArgumentException("size");
                            }

                            var buf = new byte[2 + size];
                            BinaryPrimitives.WriteUInt16LittleEndian(buf, word);
                            data.CopyTo(buf.AsSpan(2));
                            return buf;
                        }
                    case "DELTA":
                        {
                            var offset = ToInt32(entry.Offset ?? "0");
                            var sign = ToInt32(entry.Sign ?? "0");
                            var scale = ToInt32(entry.Scale ?? "0");
                            var data = ToBytes(entry.Value ?? "");

                            if (offset < 0 || 4095 < offset)
                            {
                                throw new ArgumentOutOfRangeException("offset");
                            }

                            ushort word = (ushort)(0x2000 | offset);

                            switch (sign)
                            {
                                case 1:
                                    break;
                                case -1:
                                    word |= 0x4000;
                                    break;
                                default:
                                    throw new ArgumentException("sign");
                            }

                            switch (scale)
                            {
                                case 4:
                                    break;
                                case 8:
                                    word |= 0x8000;
                                    break;
                                default:
                                    throw new ArgumentException("scale");
                            }

                            var buf = new byte[2 + 2];
                            BinaryPrimitives.WriteUInt16LittleEndian(buf, word);
                            data.CopyTo(buf.AsSpan(2));
                            return buf;
                        }
                    default:
                        throw new NotSupportedException();
                }
            }

            int ToInt32(string hexOrDigit)
            {
                if (hexOrDigit.StartsWith("0x"))
                {
                    return Convert.ToInt32(hexOrDigit.Substring(2), 16);
                }
                else
                {
                    return Convert.ToInt32(hexOrDigit);
                }
            }

            byte[] ToBytes(string hex)
            {
                return System.Text.RegularExpressions.Regex.Split(hex.Trim(), "\\s+")
                    .Select(it => Convert.ToByte(it, 16))
                    .ToArray();
            }

            int Align4(int value) => (value + 3) & (~3);
        }

        private static int DoDvrtToXml(DvrtToXmlOpt opt)
        {
            var dvrt = File.ReadAllBytes(opt.DvrtLoadFrom).AsSpan();

            var root = ParseDynamicRelocations(dvrt);

            var xs = new XmlSerializer(typeof(DynamicRelocations));
            using (var writer = new StreamWriter(opt.XmlSaveTo))
            {
                xs.Serialize(writer, root);
            }

            return 0;

            DynamicRelocations ParseDynamicRelocations(ReadOnlySpan<byte> span)
            {
                if (BinaryPrimitives.ReadInt32LittleEndian(span) != 1)
                {
                    throw new Exception("DynamicRelocations version must be 1!");
                }

                var one = ParseArm64X(span.Slice(8));

                return new DynamicRelocations
                {
                    Arm64X = one,
                };
            }

            Arm64X ParseArm64X(ReadOnlySpan<byte> span)
            {
                if (BinaryPrimitives.ReadInt64LittleEndian(span) != 6)
                {
                    throw new Exception("Arm64X symbol must be 6!");
                }

                span = span.Slice(8);

                var size = BinaryPrimitives.ReadInt32LittleEndian(span);

                span = span.Slice(4).Slice(0, size);

                var ones = ParseGroups(span);

                return new Arm64X
                {
                    Group = ones,
                };
            }

            Group[] ParseGroups(ReadOnlySpan<byte> span)
            {
                var groups = new List<Group>();

                while (span.Length != 0)
                {
                    int rva = BinaryPrimitives.ReadInt32LittleEndian(span);
                    span = span.Slice(4);
                    int size = BinaryPrimitives.ReadInt32LittleEndian(span);
                    span = span.Slice(4);

                    var subSpan = span.Slice(0, size - 8);

                    var ones = ReadEntries(subSpan);
                    groups.Add(
                        new Group
                        {
                            Rva = $"0x{rva:X8}",
                            Entry = ones,
                        }
                    );

                    span = span.Slice(size - 8);
                }

                return groups.ToArray();
            }

            Entry[] ReadEntries(ReadOnlySpan<byte> span)
            {
                var entries = new List<Entry>();

                while (span.Length != 0)
                {
                    var word = BinaryPrimitives.ReadUInt16LittleEndian(span);
                    span = span.Slice(2);

                    if (word == 0)
                    {
                        // term padding?
                        break;
                    }

                    var meta = (word >> 12) & 15;
                    switch (meta & 3)
                    {
                        case 0: // zero fill
                            {
                                var offset = word & 0x0FFF;
                                var size = 1 << ((meta >> 2) & 3);

                                entries.Add(
                                    new Entry
                                    {
                                        Offset = $"0x{offset:X3}",
                                        Type = "ZEROFILL",
                                        Size = $"0x{size:X1}",
                                        Value = "",
                                    }
                                );
                            }
                            break;
                        case 1: // assign
                            {
                                var offset = word & 0x0FFF;
                                var size = 1 << ((meta >> 2) & 3);
                                var data = span.Slice(0, size).ToArray();

                                entries.Add(
                                    new Entry
                                    {
                                        Offset = $"0x{offset:X3}",
                                        Type = "VALUE",
                                        Size = $"0x{size:X1}",
                                        Value = BitConverter.ToString(data).Replace("-", " "),
                                    }
                                );

                                span = span.Slice(size);
                            }
                            break;
                        case 2: // add or sub
                            {
                                var offset = word & 0x0FFF;
                                var sign = ((meta & 4) != 0) ? -1 : 1;
                                var scale = ((meta & 8) != 0) ? 8 : 4;
                                var data = span.Slice(0, 2).ToArray();

                                entries.Add(
                                    new Entry
                                    {
                                        Offset = $"0x{offset:X3}",
                                        Type = "DELTA",
                                        Sign = $"{sign}",
                                        Scale = $"{scale}",
                                        Value = BitConverter.ToString(data).Replace("-", " "),
                                    }
                                );

                                span = span.Slice(2);
                            }
                            break;
                        case 3:
                            throw new NotImplementedException();
                    }
                }

                return entries.ToArray();
            }
        }

        private static void PrintPESections(IEnumerable<PESection> sections)
        {
            {
                Console.WriteLine("Name     | VirtualAddress      | AtFile   | Size     | VirtSize ");
                Console.WriteLine("---------|---------------------|----------|----------|----------");
            }
            foreach (var sect in sections)
            {
                Console.WriteLine("{0,-8} | {1:X8} - {2:X8} | {3:X8} | {4:X8} | {5:X8} "
                    , sect.Name
                    , sect.VirtualAddress
                    , sect.VirtualAddress + sect.SizeOfRawData - 1
                    , sect.PointerToRawData
                    , sect.SizeOfRawData
                    , sect.VirtualSize
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

                    ushort section;
                    int offset;

                    if (editSectionHelper.TryToGlowSection(pe, sectIdx, bytesToAdd) is var pair && pair != null)
                    {
                        // acquired

                        Console.WriteLine("Info: Glow the existing section \"{0}\", and then write to the appended space.", sectAt.Name);

                        offset = sectAt.SizeOfRawData;
                        section = Convert.ToUInt16(1 + sectIdx);
                    }
                    else
                    {
                        pair = editSectionHelper.AddNewSection(pe, ".sect1", bytesToAdd);

                        Console.WriteLine("Info: Add new section \"{0}\", and then write to there.", ".sect1");

                        offset = 0;
                        section = Convert.ToUInt16(1 + header.Sections.Count);
                    }

                    pe = pair.Value.PeMod;
                    dvrt.CopyTo(pe.Slice(pair.Value.PointerToWrite));

                    var ok = new ModifyDvrtPointerHelper().ModifyDvrtPointer(
                        exe: pair.Value.PeMod,
                        offset: offset,
                        section: section,
                        logWarn: s => Console.Error.WriteLine("Warn: {0}", s),
                        logError: s => Console.Error.WriteLine("Error: {0}", s)
                    );

                    if (!ok)
                    {
                        return 1;
                    }
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
