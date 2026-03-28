using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32.Deeper
{
    public class EditSectionHelper
    {
        public (Memory<byte> PeMod, int PointerToWrite)? TryToGlowSection(
            ReadOnlyMemory<byte> pe,
            int sectionIndex,
            int bytesToGlow
            )
        {
            var peMod = pe.ToArray().AsMemory();

            var parseHeader = new ParseHeader();
            var header = parseHeader.Parse(peMod);

            var sect = header.Sections[sectionIndex];

            var rva0 = sect.VirtualAddress + sect.SizeOfRawData;
            var rva1 = rva0 = bytesToGlow;

            // Any intersections in virtual address space?

            if (header.Sections.Any(
                it => false
                    || it.VirtualAddress <= rva0 && rva0 < it.VirtualAddress + it.SizeOfRawData
                    || rva0 <= it.VirtualAddress && it.VirtualAddress < rva1
            ))
            {
                return null;
            }

            var insertPoint = sect.PointerToRawData + sect.SizeOfRawData;

            peMod = InsertAt(peMod, insertPoint, bytesToGlow);

            int pointer = -1;

            for (int y = 0, cy = header.Sections.Count; y < cy; y++)
            {
                var sect1 = MapSection(peMod, header.PEOffset, header.OptHeaderSize, y);
                if (sectionIndex == y)
                {
                    pointer = sect1.PointerToRawData + sect1.SizeOfRawData;
                    sect1.SizeOfRawData += bytesToGlow;
                }
                else if (insertPoint <= sect1.PointerToRawData)
                {
                    sect1.PointerToRawData += bytesToGlow;
                }
            }

            return (peMod, pointer);
        }

        private MappedSection MapSection(Memory<byte> pe, int peOffset, ushort optHeaderSize, int sectionIndex)
        {
            var sectionRva = peOffset + 24 + optHeaderSize + 0x28 * sectionIndex;
            return new MappedSection(pe.Slice(sectionRva, 0x28));
        }

        private record MappedSection(
            Memory<byte> Buf)
        {
            public int VirtualAddress
            {
                get => BinaryPrimitives.ReadInt32LittleEndian(Buf.Slice(12, 4).Span);
                set => BinaryPrimitives.WriteInt32LittleEndian(Buf.Slice(12, 4).Span, value);
            }

            public int SizeOfRawData
            {
                get => BinaryPrimitives.ReadInt32LittleEndian(Buf.Slice(16, 4).Span);
                set => BinaryPrimitives.WriteInt32LittleEndian(Buf.Slice(16, 4).Span, value);
            }

            public int PointerToRawData
            {
                get => BinaryPrimitives.ReadInt32LittleEndian(Buf.Slice(20, 4).Span);
                set => BinaryPrimitives.WriteInt32LittleEndian(Buf.Slice(20, 4).Span, value);
            }
        }

        private Memory<byte> InsertAt(ReadOnlyMemory<byte> source, int at, int length)
        {
            var buf = new byte[source.Length + length].AsMemory();
            source.Slice(0, at).CopyTo(buf);
            source.Slice(at).CopyTo(buf.Slice(at + length));
            return buf;
        }

        public (Memory<byte> PeMod, int PointerToWrite) AddNewSection(
            ReadOnlyMemory<byte> pe,
            string sectionName,
            int sizeOfRawData
            )
        {
            var peMod = pe.ToArray().AsMemory();

            var parseHeader = new ParseHeader();
            var header = parseHeader.Parse(peMod);

            var numOfSectionsSpan = peMod.Span.Slice(header.PEOffset + 6);
            var numOfSections = BinaryPrimitives.ReadUInt16LittleEndian(numOfSectionsSpan);

            var sizeBefore = header.PEOffset + 24 + header.OptHeaderSize + 0x28 * numOfSections;
            var sizeAfter = sizeBefore + 0x28;
            var sizePushed = 0;
            if ((sizeBefore & ~1023) != (sizeAfter & ~1023))
            {
                sizePushed = 1024;
                peMod = InsertAt(peMod, header.PEOffset + 24 + header.OptHeaderSize + 0x28 * numOfSections, sizePushed);
            }

            var numOfSectionsAfter = Convert.ToUInt16(numOfSections + 1);
            BinaryPrimitives.WriteUInt16LittleEndian(numOfSectionsSpan, numOfSectionsAfter);

            if (sizePushed != 0)
            {
                for (int y = 0; y < numOfSections; y++)
                {
                    var sect1 = MapSection(peMod, header.PEOffset, header.OptHeaderSize, y);
                    sect1.PointerToRawData += sizePushed;
                }
            }

            {
                var pointerToWrite = peMod.Length;

                var sect1 = MapSection(peMod, header.PEOffset, header.OptHeaderSize, numOfSections);
                sect1.Buf.Span.Clear();
                Encoding.Latin1.GetBytes(sectionName).CopyTo(sect1.Buf.Slice(0, 8));
                sect1.PointerToRawData = pointerToWrite;
                sect1.SizeOfRawData = sizeOfRawData;
                sect1.VirtualAddress = header.Sections
                    .Max(it => (it.VirtualAddress + it.SizeOfRawData + 15) & ~15);

                peMod = InsertAt(peMod, pointerToWrite, sizeOfRawData);

                return (peMod, pointerToWrite);
            }
        }
    }
}
