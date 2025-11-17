using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public class ParseImportTable
    {
        public IReadOnlyList<PEIData.Directory> Parse(
            ProvideReadOnlyMemoryDelegate provide,
            int virtualAddress,
            bool isPE32Plus)
        {
            int cy = 0;
            while (true)
            {
                var ptr = provide(virtualAddress + 20 * cy, 20);

                if (BinaryPrimitives.ReadInt32LittleEndian(ptr.Span) == 0)
                {
                    break;
                }

                cy += 1;
            }

            var directories = new PEIData.Directory[cy];

            for (int y = 0; y < cy; y++)
            {
                var span = provide(virtualAddress + 20 * y, 20).Span;

                var dllNameOffset = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(12, 4));

                directories[y] = new PEIData.Directory(
                    ImportRefs: ReadImport(
                        provide: provide,
                        virtualAddress: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(0, 4)),
                        isPE32Plus: isPE32Plus
                    ),
                    Timestamp: BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4, 4)),
                    ForwarderChain: BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8, 4)),
                    DLLName: ReadCString(provide, dllNameOffset),
                    ImportAddrs: ReadImport(
                        provide: provide,
                        virtualAddress: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(16, 4)),
                        isPE32Plus: isPE32Plus
                    )
                );
            }

            return directories;
        }

        private IReadOnlyList<PEIData.Import> ReadImport(
            ProvideReadOnlyMemoryDelegate provide,
            int virtualAddress,
            bool isPE32Plus
        )
        {
            if (isPE32Plus)
            {
                int cx = 0;

                while (true)
                {
                    int entryOffset = virtualAddress + 8 * cx;
                    if (BinaryPrimitives.ReadInt64LittleEndian(provide(entryOffset, 8).Span) == 0)
                    {
                        break;
                    }
                    cx += 1;
                }

                var imports = new PEIData.Import[cx];

                for (int x = 0; x < cx; x++)
                {
                    int entryOffset = virtualAddress + 8 * x;
                    var importEntry = BinaryPrimitives.ReadInt64LittleEndian(provide(entryOffset, 8).Span);
                    if (importEntry < 0)
                    {
                        imports[x] = new PEIData.Import(
                            IsOrdinal: true,
                            Ordinal: importEntry & 0xFFFF,
                            Hint: 0,
                            Name: string.Empty
                        );
                    }
                    else
                    {
                        int hintNameOffset = (int)importEntry;
                        ushort hint = BinaryPrimitives.ReadUInt16LittleEndian(provide(hintNameOffset, 2).Span);
                        string name = ReadCString(provide, hintNameOffset + 2);
                        imports[x] = new PEIData.Import(
                            IsOrdinal: false,
                            Ordinal: 0,
                            Hint: hint,
                            Name: name
                        );
                    }
                }

                return imports;
            }
            else
            {
                int cx = 0;

                while (true)
                {
                    int entryOffset = virtualAddress + 4 * cx;

                    if (BinaryPrimitives.ReadInt32LittleEndian(provide(entryOffset, 4).Span) == 0)
                    {
                        break;
                    }

                    cx += 1;
                }

                var imports = new PEIData.Import[cx];

                for (int x = 0; x < cx; x++)
                {
                    int entryOffset = virtualAddress + 4 * x;
                    var importEntry = BinaryPrimitives.ReadInt32LittleEndian(provide(entryOffset, 4).Span);
                    if (importEntry < 0)
                    {
                        imports[x] = new PEIData.Import(
                            IsOrdinal: true,
                            Ordinal: importEntry & 0xFFFF,
                            Hint: 0,
                            Name: string.Empty
                        );
                    }
                    else
                    {
                        int hintNameOffset = importEntry;
                        ushort hint = BinaryPrimitives.ReadUInt16LittleEndian(provide(hintNameOffset, 2).Span);
                        string name = ReadCString(provide, hintNameOffset + 2);
                        imports[x] = new PEIData.Import(
                            IsOrdinal: false,
                            Ordinal: 0,
                            Hint: hint,
                            Name: name
                        );
                    }
                }

                return imports;
            }
        }

        private string ReadCString(
            ProvideReadOnlyMemoryDelegate provide,
            int offset,
            int maxLength = 256
        )
        {
            int length = 0;

            var span = provide(offset, -maxLength).Span;

            while (true)
            {
                if (span[length] == 0)
                {
                    return Encoding.Latin1.GetString(provide(offset, length).Span);
                }
                else
                {
                    length += 1;
                }
            }
        }
    }
}
