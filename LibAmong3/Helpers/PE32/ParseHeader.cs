using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public class ParseHeader
    {
        public PEHeader Parse(ReadOnlyMemory<byte> exe)
        {
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
                && ReadSections(exe.Span.Slice(peOffset + 24 + optHeaderSize, 0x28 * numOfSections), numOfSections) is PESection[] peSections
            )
            {
                var optionalHeader = exe.Slice(peOffset + 24, optHeaderSize);
                var isPE32Plus = false;
                IReadOnlyList<PEImageDataDirectory> imageDataDirectories = Array.Empty<PEImageDataDirectory>();

                if (24 <= optHeaderSize)
                {
                    var magic = BinaryPrimitives.ReadUInt16LittleEndian(optionalHeader.Span.Slice(0, 2));
                    if (magic != 0x10B && magic != 0x20B)
                    {
                        throw new InvalidDataException();
                    }

                    isPE32Plus = magic == 0x20B;

                    var numberOfRvaAndSizes = BinaryPrimitives.ReadInt32LittleEndian(
                        isPE32Plus
                            ? optionalHeader.Span.Slice(108, 4)
                            : optionalHeader.Span.Slice(92, 4)
                    );

                    var imageDataDirectoryPtr = isPE32Plus
                        ? optionalHeader.Slice(112)
                        : optionalHeader.Slice(96);

                    imageDataDirectories = Enumerable.Range(0, numberOfRvaAndSizes)
                        .Select(
                            index => new PEImageDataDirectory(
                                VirtualAddress: BinaryPrimitives.ReadInt32LittleEndian(
                                    imageDataDirectoryPtr.Span.Slice(8 * index, 4)
                                ),
                                Size: BinaryPrimitives.ReadInt32LittleEndian(
                                    imageDataDirectoryPtr.Span.Slice(8 * index + 4, 4)
                                )
                            )
                        )
                        .ToImmutableArray();
                }

                return new PEHeader(
                    Machine: machine,
                    Sections: peSections,
                    IsPE32Plus: isPE32Plus,
                    ImageDataDirectories: imageDataDirectories
                );
            }
            else
            {
                throw new InvalidDataException();
            }
        }

        private PESection[] ReadSections(ReadOnlySpan<byte> sectionHeader, int numOfSections)
        {
            var list = new PESection[numOfSections];
            for (int index = 0; index < numOfSections; index++)
            {
                list[index] = new PESection(
                    Name: Encoding.Latin1.GetString(
                        sectionHeader.Slice(0x28 * index, 8)
                    )
                        .TrimEnd('\0'),
                    VirtualAddress: BinaryPrimitives.ReadInt32LittleEndian(sectionHeader.Slice(0x28 * index + 12, 4)),
                    SizeOfRawData: BinaryPrimitives.ReadInt32LittleEndian(sectionHeader.Slice(0x28 * index + 16, 4)),
                    PointerToRawData: BinaryPrimitives.ReadInt32LittleEndian(sectionHeader.Slice(0x28 * index + 20, 4))
                );
            }
            return list;
        }
    }
}
