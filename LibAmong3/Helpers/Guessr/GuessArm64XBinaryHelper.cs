using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.Guessr
{
    public class GuessArm64XBinaryHelper
    {
        public Arm64XBinaryForm Guess(ReadOnlyMemory<byte> exe)
        {
            if (true
                && 2 <= exe.Length
                && BinaryPrimitives.ReadUInt16LittleEndian(exe.Span) is ushort coffMagic
            )
            {
                if (false) { }
                else if (coffMagic == 0x01C4)
                {
                    return Arm64XBinaryForm.Arm32Coff;
                }
                else if (coffMagic == 0xAA64)
                {
                    return Arm64XBinaryForm.Arm64Coff;
                }
                else if (coffMagic == 0xA641)
                {
                    return Arm64XBinaryForm.Arm64ECCoff;
                }
                else if (coffMagic == 0x8664)
                {
                    return Arm64XBinaryForm.X64Coff;
                }
                else if (coffMagic == 0x014C)
                {
                    string[] ReadSectionNames(ReadOnlySpan<byte> sectionHeader, int numOfSections)
                    {
                        var list = new string[numOfSections];
                        for (int index = 0; index < numOfSections; index++)
                        {
                            list[index] = Encoding.Latin1.GetString(
                                sectionHeader.Slice(40 * index, 8)
                            )
                                .TrimEnd('\0', ' ');
                        }
                        return list;
                    }

                    if (true
                        && 20 <= exe.Length
                        && BinaryPrimitives.ReadUInt16LittleEndian(exe.Span.Slice(2, 2)) is ushort numOfSections
                        && numOfSections == 2
                        && BinaryPrimitives.ReadUInt16LittleEndian(exe.Span.Slice(16, 2)) is ushort optHeaderSize
                        && 20 + optHeaderSize + 40 * numOfSections <= exe.Length
                        && ReadSectionNames(exe.Span.Slice(20 + optHeaderSize, 40 * numOfSections), numOfSections) is string[] sectionNames
                        && sectionNames.Contains("AA64.obj")
                        && sectionNames.Contains("A641.obj")
                    )
                    {
                        return Arm64XBinaryForm.Arm64XCoffUponX86Coff;
                    }
                    else
                    {
                        return Arm64XBinaryForm.X86Coff;
                    }
                }

                if (true
                    && coffMagic == 0x0000
                    && 20 <= exe.Length
                    && BinaryPrimitives.ReadUInt16LittleEndian(exe.Span.Slice(2, 2)) is ushort word2
                    && word2 == 0xFFFF
                    && BinaryPrimitives.ReadUInt16LittleEndian(exe.Span.Slice(4, 2)) is ushort word4
                    && word4 == 0x0001
                )
                {
                    return Arm64XBinaryForm.AnonymousCoff; // Skip leading 6 bytes to crop out an inner obj file.
                }
            }

            {
                string[] ReadSectionNames(ReadOnlySpan<byte> sectionHeader, int numOfSections)
                {
                    var list = new string[numOfSections];
                    for (int index = 0; index < numOfSections; index++)
                    {
                        list[index] = Encoding.Latin1.GetString(
                            sectionHeader.Slice(0x28 * index, 8)
                        )
                            .TrimEnd('\0');
                    }
                    return list;
                }

                if (true
                    && 0x40 <= exe.Length
                    && exe.Span[0] == 0x4D
                    && exe.Span[1] == 0x5A
                    && BinaryPrimitives.ReadInt32LittleEndian(exe.Span.Slice(0x3C, 4)) is int peOffset
                    && 0 <= peOffset
                    && peOffset + 24 <= exe.Length
                    && exe.Span[peOffset + 0] == 0x50
                    && exe.Span[peOffset + 1] == 0x45
                    && BinaryPrimitives.ReadUInt16LittleEndian(exe.Span.Slice(peOffset + 4, 2)) is ushort machine
                    && BinaryPrimitives.ReadUInt16LittleEndian(exe.Span.Slice(peOffset + 6, 2)) is ushort numOfSections
                    && BinaryPrimitives.ReadUInt16LittleEndian(exe.Span.Slice(peOffset + 0x14, 2)) is ushort optHeaderSize
                    && peOffset + 24 + optHeaderSize + 0x28 * numOfSections <= exe.Length
                    && ReadSectionNames(exe.Span.Slice(peOffset + 24 + optHeaderSize, 0x28 * numOfSections), numOfSections) is string[] sectionNames
                )
                {
                    var a64xrm = sectionNames.Contains(".a64xrm");
                    var hexpthk = sectionNames.Contains(".hexpthk");

                    if (false) { }
                    else if (machine == 0x8664)
                    {
                        return (a64xrm && hexpthk) ? Arm64XBinaryForm.Arm64EC : Arm64XBinaryForm.X64;
                    }
                    else if (machine == 0xAA64)
                    {
                        if (a64xrm)
                        {
                            if (hexpthk)
                            {
                                return Arm64XBinaryForm.Arm64X;
                            }
                            else
                            {
                                return Arm64XBinaryForm.Arm64XPureForwarder;
                            }
                        }
                        else
                        {
                            return Arm64XBinaryForm.Arm64;
                        }
                    }
                    else if (machine == 0x14C)
                    {
                        return Arm64XBinaryForm.X86;
                    }
                    else if (machine == 0x1C4)
                    {
                        return Arm64XBinaryForm.Arm32;
                    }
                }
            }

            return Arm64XBinaryForm.Unknown;
        }
    }
}
