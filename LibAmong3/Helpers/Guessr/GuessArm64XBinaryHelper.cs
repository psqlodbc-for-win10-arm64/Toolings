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
                    return Arm64XBinaryForm.X86Coff;
                }
            }

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
                var a64xrm = true
                    && sectionNames.Contains(".a64xrm")
                    && sectionNames.Contains(".hexpthk");

                if (false) { }
                else if (machine == 0x8664)
                {
                    return a64xrm ? Arm64XBinaryForm.Arm64EC : Arm64XBinaryForm.X64;
                }
                else if (machine == 0xAA64)
                {
                    return a64xrm ? Arm64XBinaryForm.Arm64X : Arm64XBinaryForm.Arm64;
                }
                else if (machine == 0x14C)
                {
                    return Arm64XBinaryForm.X86;
                }
            }

            return Arm64XBinaryForm.Unknown;
        }
    }
}
