using AsmArm64;
using CommandLine;
using LibAmong3.Helpers.PE32;
using LibAmong3.Helpers.PE32.Deeper;
using System.Buffers.Binary;
using System.IO;
using System.Text.RegularExpressions;

namespace InspectTLSCallback
{
    internal class Program
    {
        [Verb("inspect")]
        private class InspectOpt
        {
            [Value(0, Required = true, MetaName = "PEInput")]
            public string PEInput { get; set; } = null!;
        }

        [Verb("dummy")]
        private class DummyOpt
        {

        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<InspectOpt, DummyOpt>(args)
                .MapResult<InspectOpt, DummyOpt, int>(
                    DoInspect,
                    DoDummy,
                    errs => 1
                );
        }

        private static int DoDummy(DummyOpt opt)
        {
            throw new NotImplementedException();
        }

        private static int DoInspect(InspectOpt opt)
        {
            var pe = File.ReadAllBytes(opt.PEInput).AsMemory();

            var disasmAArch64 = new Arm64Disassembler(
                new Arm64DisassemblerOptions
                {
                    PrintLabelBeforeFirstInstruction = false,
                }
            );

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
                    new ApplyDvrtHelper().ApplyDvrt(pe);
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

                                void DumpHex(ReadOnlySpan<byte> bytes, ulong addr, string prefix)
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

                                DumpHex(bytes, rva, "    ");

                                if (header.Machine == 0xAA64 || arm64EC)
                                {
                                    void DisasmAArch64(Span<byte> bytes, ulong addr, string prefix)
                                    {
                                        Console.WriteLine();
                                        Console.WriteLine(prefix + "AArch64 disassembly by AsmArm64");
                                        Console.WriteLine(prefix + "---");

                                        for (int y = 0; y < bytes.Length / 4; y++)
                                        {
                                            Console.WriteLine(prefix + $"{addr + (uint)(4 * y):X16}   {disasmAArch64.Disassemble(bytes.Slice(4 * y, 4)).Trim()}");
                                        }
                                    }

                                    DisasmAArch64(bytes, rva, "    ");

                                    if (TryToGetJumpDestination(bytes, disasmAArch64, rva, out ulong next))
                                    {
                                        Console.WriteLine();
                                        Console.WriteLine($"    Navigate to the destination at {next:X16}");

                                        var nextBytes = provider.Provide(Convert.ToInt32(next - header.ImageBase), -64)
                                            .ToArray()
                                            .AsSpan();

                                        DumpHex(nextBytes, next, "      ");
                                        DisasmAArch64(nextBytes, next, "      ");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return 0;
        }

        private static bool TryToGetJumpDestination(Span<byte> bytes, Arm64Disassembler disassembler, ulong rva, out ulong next)
        {
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
    }
}
