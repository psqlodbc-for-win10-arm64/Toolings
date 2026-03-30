using AsmArm64;
using CommandLine;
using LibAmong3.Helpers.PE32;
using LibAmong3.Helpers.PE32.Deeper;
using System.Buffers.Binary;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

namespace InspectTLSCallback
{
    internal class Program
    {
        [Verb("inspect", HelpText = "Inspect TLS callback of a PE file.")]
        private class InspectOpt
        {
            [Value(0, Required = true, MetaName = "PEInput")]
            public string PEInput { get; set; } = null!;
        }

        [Verb("disasm", HelpText = "Disassemble AArch64 code referenced by virtual address.")]
        private class DisasmOpt
        {
            [Value(0, Required = true, MetaName = "PEInput")]
            public string PEInput { get; set; } = null!;

            [Value(1, Required = true, MetaName = "VirtualAddress")]
            public string VirtualAddress { get; set; } = null!;

            [Value(2, Required = false, MetaName = "Size", Default = "64")]
            public string Size { get; set; } = null!;

            [Option('r', "relative", HelpText = "Use relative addressing instead of absolute.")]
            public bool Relative { get; set; }

            [Option('a', "apply-dvrt", HelpText = "Apply DVRT before disassembly.")]
            public bool AppltDvrt { get; set; }

            [Option('x', "x64", HelpText = "Disassemble as x64 code instead of AArch64.")]
            public bool X64 { get; set; }
        }

        [Verb("dump", HelpText = "Hex dump of a PE file referenced by virtual address.")]
        private class HexDumpOpt
        {
            [Value(0, Required = true, MetaName = "PEInput")]
            public string PEInput { get; set; } = null!;

            [Value(1, Required = true, MetaName = "VirtualAddress")]
            public string VirtualAddress { get; set; } = null!;

            [Value(2, Required = false, MetaName = "Size", Default = "64")]
            public string Size { get; set; } = null!;

            [Option('r', "relative", HelpText = "Use relative addressing instead of absolute.")]
            public bool Relative { get; set; }

            [Option('a', "apply-dvrt", HelpText = "Apply DVRT before dump.")]
            public bool AppltDvrt { get; set; }
        }

        [Verb("dump-file", HelpText = "Dump of a PE file referenced by virtual address.")]
        private class DumpOpt
        {
            [Value(0, Required = true, MetaName = "PEInput")]
            public string PEInput { get; set; } = null!;

            [Value(1, Required = true, MetaName = "VirtualAddress")]
            public string VirtualAddress { get; set; } = null!;

            [Value(2, Required = false, MetaName = "Size", Default = "64")]
            public string Size { get; set; } = null!;

            [Value(3, Required = true, MetaName = "SaveTo")]
            public string SaveTo { get; set; } = null!;

            [Option('r', "relative", HelpText = "Use relative addressing instead of absolute.")]
            public bool Relative { get; set; }

            [Option('a', "apply-dvrt", HelpText = "Apply DVRT before dump.")]
            public bool AppltDvrt { get; set; }
        }

        [Verb("locate", HelpText = "Resolve virtual address and try to print file offset of a PE file.")]
        private class LocateOpt
        {
            [Value(0, Required = true, MetaName = "PEInput")]
            public string PEInput { get; set; } = null!;

            [Value(1, Required = true, MetaName = "VirtualAddress")]
            public string VirtualAddress { get; set; } = null!;

            [Option('r', "relative", HelpText = "Use relative addressing instead of absolute.")]
            public bool Relative { get; set; }

            [Option('a', "apply-dvrt", HelpText = "Apply DVRT before resolving.")]
            public bool AppltDvrt { get; set; }

            [Option('x', "hex", HelpText = "Print file offwset in 64 bits upper hex (e.g. 0123456789ABCDEF).")]
            public bool Hex { get; set; }

            [Option('e', "print-error", HelpText = "Print error messages if applicable.")]
            public bool PrintError { get; set; }
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<InspectOpt, DisasmOpt, HexDumpOpt, DumpOpt, LocateOpt>(args)
                .MapResult<InspectOpt, DisasmOpt, HexDumpOpt, DumpOpt, LocateOpt, int>(
                    DoInspect,
                    DoDisasm,
                    DoHexDump,
                    DoDump,
                    DoLocate,
                    errs => 1
                );
        }

        private static int DoLocate(LocateOpt opt)
        {
            var pe = File.ReadAllBytes(opt.PEInput).AsMemory();

            if (opt.AppltDvrt)
            {
                new ApplyDvrtHelper().ApplyDvrt(pe);
            }

            var header = new ParseHeader().Parse(pe);
            var provider = new VAReadOnlySpanProvider(
                pe,
                header.Sections
            );

            try
            {
                var va = PaseUInt64(opt.VirtualAddress);

                var filePos = provider.Locate(
                    rva: Convert.ToInt32(opt.Relative ? va : va - header.ImageBase),
                    size: 0
                )
                    .Start;

                Console.Write(opt.Hex ? $"{filePos:X16}" : $"{filePos}");
                return 0;
            }
            catch (FormatException)
            {
                if (opt.PrintError)
                {
                    throw;
                }
                else
                {
                    return 1;
                }
            }
            catch (OverflowException)
            {
                if (opt.PrintError)
                {
                    throw;
                }
                else
                {
                    return 1;
                }
            }
            catch (EndOfStreamException)
            {
                if (opt.PrintError)
                {
                    throw;
                }
                else
                {
                    return 1;
                }
            }
            catch (ArgumentException)
            {
                if (opt.PrintError)
                {
                    throw;
                }
                else
                {
                    return 1;
                }
            }
        }

        private static int DoDump(DumpOpt opt)
        {
            var pe = File.ReadAllBytes(opt.PEInput).AsMemory();

            if (opt.AppltDvrt)
            {
                new ApplyDvrtHelper().ApplyDvrt(pe);
            }

            var header = new ParseHeader().Parse(pe);
            var provider = new VAReadOnlySpanProvider(
                pe,
                header.Sections
            );

            var va = PaseUInt64(opt.VirtualAddress);

            var bytes = provider.Provide(
                rva: Convert.ToInt32(opt.Relative ? va : va - header.ImageBase),
                size: Convert.ToInt32(PaseInt64(opt.Size))
            );

            using (var stream = File.Create(opt.SaveTo))
            {
                stream.Write(bytes);
            }

            Console.WriteLine($"Dumped {bytes.Length} bytes to {opt.SaveTo}");
            return 0;
        }

        private static int DoHexDump(HexDumpOpt opt)
        {
            var pe = File.ReadAllBytes(opt.PEInput).AsMemory();

            if (opt.AppltDvrt)
            {
                new ApplyDvrtHelper().ApplyDvrt(pe);
            }

            var header = new ParseHeader().Parse(pe);
            var provider = new VAReadOnlySpanProvider(
                pe,
                header.Sections
            );

            var va = PaseUInt64(opt.VirtualAddress);

            var bytes = provider.Provide(
                rva: Convert.ToInt32(opt.Relative ? va : va - header.ImageBase),
                size: Convert.ToInt32(PaseInt64(opt.Size))
            );

            DumpHex(bytes, va, "");

            return 0;
        }

        private static int DoDisasm(DisasmOpt opt)
        {
            var pe = File.ReadAllBytes(opt.PEInput).AsMemory();

            if (opt.AppltDvrt)
            {
                new ApplyDvrtHelper().ApplyDvrt(pe);
            }

            var header = new ParseHeader().Parse(pe);
            var provider = new VAReadOnlySpanProvider(
                pe,
                header.Sections
            );

            var va = PaseUInt64(opt.VirtualAddress);

            var bytes = provider.Provide(
                rva: Convert.ToInt32(opt.Relative ? va : va - header.ImageBase),
                size: Convert.ToInt32(PaseInt64(opt.Size))
            )
                .ToArray()
                    .AsSpan(); // AsmArm64 requests writable Span<byte>

            var disasm = opt.X64
                ? CreateX64Disasm()
                : CreateAArch64Disasm();

            disasm(
                bytes: bytes,
                addr: va,
                prefix: "",
                showBanner: false
            );

            return 0;
        }

        private static ulong PaseUInt64(string addr)
        {
            if (addr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToUInt64(addr.Substring(2), 16);
            }
            else if (addr.Length == 16)
            {
                return Convert.ToUInt64(addr, 16);
            }
            else
            {
                return Convert.ToUInt64(addr);
            }
        }

        private static long PaseInt64(string addr)
        {
            if (addr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt64(addr.Substring(2), 16);
            }
            else if (addr.Length == 16)
            {
                return Convert.ToInt64(addr, 16);
            }
            else
            {
                return Convert.ToInt64(addr);
            }
        }

        private static int DoInspect(InspectOpt opt)
        {
            var pe = File.ReadAllBytes(opt.PEInput).AsMemory();

            var disasmAArch64 = CreateAArch64Disasm();

            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 0)
                {
                    Console.WriteLine("# Before apply Dvrt");
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("# After apply Dvrt");

                    var applier = new ApplyDvrtHelper().CreateDvrtApplier(pe);
                    if (!applier.HasLoadConfig)
                    {
                        Console.WriteLine();
                        Console.WriteLine("(Omit due to lack of LoadConfig)");
                        break;
                    }
                    if (applier.NumPatchedRecords == 0 || applier.ApplyPatches == null)
                    {
                        Console.WriteLine();
                        Console.WriteLine("(Omit due to lack of applicable patches)");
                        break;
                    }

                    applier.ApplyPatches();
                }

                var header = new ParseHeader().Parse(pe);
                Console.WriteLine();
                Console.WriteLine($"  {header.Machine,16:X4} machine");

                var arm64EC = false;

                var lookAtLoadConfig = LookAtLoadConfig1.Create(pe);
                if (lookAtLoadConfig != null)
                {
                    var chpeVersion = lookAtLoadConfig.GetCHPEVersion();
                    Console.WriteLine($"  {chpeVersion,16} CHPE Version");
                    if (chpeVersion == 2 && header.Machine == 0x8664)
                    {
                        Console.WriteLine($"  {"",16} This is an Arm64EC! (chpeVersion == 2 && header.Machine == 0x8664)");
                        arm64EC = true;
                    }
                }

                var tlsDirectoryEntry = header.GetImageDirectoryOrEmpty(9);
                if (tlsDirectoryEntry.Size != 0)
                {
                    var provider = new VAReadOnlySpanProvider(
                        pe,
                        header.Sections
                    );
                    var parseTLSDirectory = new ParseTLSDirectory();
                    var tlsHeader = parseTLSDirectory.Parse(
                        provider.Provide,
                        tlsDirectoryEntry.VirtualAddress,
                        header.IsPE32Plus
                    )
                        .Header1;

                    Console.WriteLine();
                    Console.WriteLine($"  {tlsHeader.StartOfRawData:X16} Start of raw data");
                    Console.WriteLine($"  {tlsHeader.EndOfRawData:X16} End of raw data");
                    Console.WriteLine($"  {tlsHeader.AddressOfIndex:X16} Address of index");
                    Console.WriteLine($"  {tlsHeader.AddressOfCallback:X16} Address of callbacks");
                    Console.WriteLine($"  {tlsHeader.SizeOfZeroFill,16:X} Size of zero fill");
                    Console.WriteLine($"  {tlsHeader.Characteristics,16:X8} Characteristics");

                    if (tlsHeader.AddressOfCallback != 0)
                    {
                        var tlsCallbacks = parseTLSDirectory.ParseCallbacks(
                            provide: provider.Provide,
                            addressOfCallback: tlsHeader.AddressOfCallback,
                            imageBase: header.ImageBase,
                            isPE32Plus: header.IsPE32Plus
                        );

                        Console.WriteLine();
                        Console.WriteLine("    TLS Callbacks");
                        Console.WriteLine();
                        Console.WriteLine("    Address");
                        Console.WriteLine("    ----------------");

                        foreach (var one in tlsCallbacks)
                        {
                            Console.WriteLine($"    {one:X16}");
                        }

                        foreach (var rva in tlsCallbacks)
                        {
                            if (rva != 0)
                            {
                                var bytes = provider.Provide(Convert.ToInt32(rva - header.ImageBase), -64)
                                    .ToArray()
                                    .AsSpan(); // AsmArm64 requests writable Span<byte>

                                DumpHex(bytes, rva, "    ");

                                if (header.Machine == 0xAA64 || arm64EC)
                                {
                                    disasmAArch64(bytes, rva, "    ");

                                    if (TryToGetJumpDestination(bytes, rva, out ulong next))
                                    {
                                        Console.WriteLine();
                                        Console.WriteLine($"    Navigate to the destination at {next:X16}");

                                        var nextBytes = provider.Provide(Convert.ToInt32(next - header.ImageBase), -64)
                                            .ToArray()
                                            .AsSpan();

                                        DumpHex(nextBytes, next, "      ");
                                        disasmAArch64(nextBytes, next, "      ");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return 0;
        }

        private static void DumpHex(ReadOnlySpan<byte> bytes, ulong addr, string prefix)
        {
            Console.WriteLine();
            Console.WriteLine(prefix + "VirtualAddress   | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
            Console.WriteLine(prefix + "-----------------|------------------------------------------------");
            for (int y = 0; y < bytes.Length; y += 16)
            {
                Console.Write(prefix + $"{addr + (uint)y:X16} |");
                for (int x = 0; x < 16; x++)
                {
                    if (y + x < bytes.Length)
                    {
                        Console.Write($" {bytes[y + x]:X2}");
                    }
                    else
                    {
                        Console.Write("   ");
                    }
                }
                Console.WriteLine();
            }
        }

        private static bool TryToGetJumpDestination(Span<byte> bytes, ulong rva, out ulong next)
        {
            var disassembler = new Arm64Disassembler(
                new Arm64DisassemblerOptions
                {
                    PrintLabelBeforeFirstInstruction = false,
                }
            );

            if (12 <= bytes.Length)
            {
                // adrp x16, #-4096
                var match1 = Regex.Match(
                    disassembler.Disassemble(bytes.Slice(0, 4)),
                    "^\\s*adrp\\s+x16\\s*,\\s*#(?<imm>-?\\d+)\\s*$"
                );
                if (match1.Success)
                {
                    var imm = Convert.ToInt64(match1.Groups["imm"].Value);
                    // add x16, x16, #2168
                    var match2 = Regex.Match(
                        disassembler.Disassemble(bytes.Slice(4, 4)),
                        "^\\s*add\\s*x16\\s*,\\s*x16\\s*,\\s*#(?<imm>-?\\d+)\\s*$"
                    );
                    if (match2.Success)
                    {
                        var imm2 = Convert.ToInt32(match2.Groups["imm"].Value);
                        // br x16
                        var match3 = Regex.Match(
                            disassembler.Disassemble(bytes.Slice(8, 4)),
                            "^\\s*br\\s*x16\\s*$"
                        );
                        if (match3.Success)
                        {
                            next = (ulong)((long)(rva & ~4095UL) + imm + imm2);
                            return true;
                        }
                    }
                }
            }

            next = default;
            return false;
        }

        private delegate void DisasmDelegate(
            Span<byte> bytes,
            ulong addr,
            string prefix,
            bool showBanner = true);

        private static DisasmDelegate CreateAArch64Disasm()
        {
            var disasmAArch64 = new Arm64Disassembler(
                new Arm64DisassemblerOptions
                {
                    PrintLabelBeforeFirstInstruction = false,
                }
            );

            return (bytes, addr, prefix, showBanner) =>
            {
                if (showBanner)
                {
                    Console.WriteLine();
                    Console.WriteLine(prefix + "AArch64 disassembly by AsmArm64");
                    Console.WriteLine(prefix + "---");
                }

                for (int y = 0; y < bytes.Length / 4; y++)
                {
                    var span = bytes.Slice(4 * y, 4);
                    // `dumpbin /disasm` compatible output.
                    var disasm = disasmAArch64.Disassemble(span).Trim();

                    // Reformat `LL_01:     adrp x10, LL_01` → `adrp x10, #0`
                    if (disasm.Contains("LL_01:"))
                    {
                        disasm = disasm.Split('\n').Last().Replace("LL_01", "#0").Trim();
                    }

                    Console.WriteLine(prefix + $"{addr + (uint)(4 * y):X16}: {BinaryPrimitives.ReadUInt32LittleEndian(span):X8}  {disasm}");
                }
            };
        }

        private static DisasmDelegate CreateX64Disasm()
        {
            return (bytes, addr, prefix, showBanner) =>
            {
                if (showBanner)
                {
                    Console.WriteLine();
                    Console.WriteLine(prefix + "x64 disassembly by Iced");
                    Console.WriteLine(prefix + "---");
                }

                var decoder = Iced.Intel.Decoder.Create(
                    bitness: 64,
                    data: bytes.ToArray(),
                    ip: addr,
                    options: Iced.Intel.DecoderOptions.None
                );

                foreach (var instruction in decoder)
                {
                    // `dumpbin /disasm` compatible output.
                    //   0000000000000058: FF 15 00 00 00 00  call        qword ptr [__imp___stdio_common_vfprintf]
                    var lines = FormatX64InstructionBytes(
                        bytes.Slice(
                            Convert.ToInt32(instruction.IP - addr),
                            instruction.Length
                        )
                    )
                        .ToArray();

                    Console.WriteLine(prefix + $"{instruction.IP:X16}:{lines[0]}  {instruction.ToString()}");

                    for (int y = 1; y < lines.Length; y++)
                    {
                        Console.WriteLine(prefix + $"{"",16} {lines[y]}");
                    }
                }
            };
        }

        private static IEnumerable<string> FormatX64InstructionBytes(Span<byte> span)
        {
            var list = new List<string>();
            for (int y = 0; y < span.Length; y += 6)
            {
                int x = 0;
                var line = "";
                for (; x < 6 && y + x < span.Length; x++)
                {
                    line += ($" {span[y + x]:X2}");
                }
                for (; x < 6; x++)
                {
                    line += ("   ");
                }
                list.Add(line);
            }
            return list.AsReadOnly();
        }
    }
}
