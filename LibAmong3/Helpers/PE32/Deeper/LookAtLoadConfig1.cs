using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32.Deeper
{
    public record LookAtLoadConfig1(
        Func<int> GetCHPEVersion,
        Func<bool> HasDvrtToMakeX64,
        Func<PatchableVASpanProvider, LookAtLoadConfig1.ApplyDvrtResult> ApplyDvrt,
        Func<ulong> GetCHPEMetadataPointer)
    {
        public static LookAtLoadConfig1? Create(ReadOnlyMemory<byte> exe)
        {
            var parseHeader = new ParseHeader();
            var header = parseHeader.Parse(exe);
            var loadConfigDirEntry = header.GetImageDirectoryOrEmpty(10);
            if (loadConfigDirEntry.Size != 0)
            {
                var parseLoadConfigDir = new ParseLoadConfigDir();
                var provider = new VAReadOnlySpanProvider(
                    exe,
                    header.Sections
                );
                var loadConfigDir = parseLoadConfigDir.Parse(
                    provide: provider.Provide,
                    virtualAddress: loadConfigDirEntry.VirtualAddress,
                    isPE32Plus: header.IsPE32Plus
                );

                ulong GetCHPEMetadataPointer()
                {
                    return loadConfigDir.Header1.CHPEMetadataPointer;
                }

                int GetCHPEVersion()
                {
                    var chpeMetadataPointer = loadConfigDir.Header1.CHPEMetadataPointer;
                    if (chpeMetadataPointer != 0)
                    {
                        var versionSpan = provider.Provide(Convert.ToInt32(chpeMetadataPointer - header.ImageBase), 4);
                        return BinaryPrimitives.ReadInt32LittleEndian(versionSpan);
                    }
                    else
                    {
                        return 0;
                    }
                }

                ApplyDvrtResult ApplyDvrt(PatchableVASpanProvider subProvider)
                {
                    if (loadConfigDir.Header1.DynamicValueRelocTableSection != 0)
                    {
                        var rvaStart = header.Sections[loadConfigDir.Header1.DynamicValueRelocTableSection - 1].VirtualAddress + (int)loadConfigDir.Header1.DynamicValueRelocTableOffset;
                        var rvaEnd = Parse_IMAGE_DYNAMIC_RELOCATION_TABLEs(
                            rva: rvaStart
                        );

                        return new ApplyDvrtResult(RvaStart: rvaStart, RvaEnd: rvaEnd);

                        int Parse_IMAGE_DYNAMIC_RELOCATION_TABLEs(int rva)
                        {
                            while (true)
                            {
                                // https://ffri.github.io/ProjectChameleon/new_reloc_chpev2/#new-dynamic-value-relocation-table-dvrt-image_dynamic_relocation_arm64x

                                // typedef struct {
                                //   DWORD Version; // = 1
                                //   DWORD Size;
                                // } IMAGE_DYNAMIC_RELOCATION_TABLE;

                                var IMAGE_DYNAMIC_RELOCATION_TABLE = subProvider.Provide(rva, 8);
                                rva += 8;

                                var version = BinaryPrimitives.ReadInt32LittleEndian(IMAGE_DYNAMIC_RELOCATION_TABLE);
                                if (version != 1)
                                {
                                    break;
                                }

                                var size = BinaryPrimitives.ReadInt32LittleEndian(IMAGE_DYNAMIC_RELOCATION_TABLE.Slice(4));
                                if (size < 8)
                                {
                                    break;
                                }

                                Parse_IMAGE_DYNAMIC_RELOCATION_ARM64X_HEADER(rva);

                                rva += size;
                                continue;

                            }

                            return rva;
                        }

                        void Parse_IMAGE_DYNAMIC_RELOCATION_ARM64X_HEADER(int rva)
                        {
                            // https://ffri.github.io/ProjectChameleon/new_reloc_chpev2/#new-dynamic-value-relocation-table-dvrt-image_dynamic_relocation_arm64x

                            // typedef struct {
                            //   ULONGLONG Symbol; // = 6
                            //   DWORD FixupInfoSize;
                            // } IMAGE_DYNAMIC_RELOCATION_ARM64X_HEADER;

                            var IMAGE_DYNAMIC_RELOCATION_ARM64X_HEADER = subProvider.Provide(rva, 12);
                            rva += 12;

                            var symbol = BinaryPrimitives.ReadInt64LittleEndian(IMAGE_DYNAMIC_RELOCATION_ARM64X_HEADER);
                            if (symbol == 6)
                            {
                                var baseRelocSize = BinaryPrimitives.ReadInt32LittleEndian(IMAGE_DYNAMIC_RELOCATION_ARM64X_HEADER.Slice(8));

                                Parse_IMAGE_DYNAMIC_RELOCATION_ARM64X_BLOCK(rva, rva + baseRelocSize);
                            }
                        }

                        void Parse_IMAGE_DYNAMIC_RELOCATION_ARM64X_BLOCK(int rva, int rvaEnd)
                        {
                            while (rva < rvaEnd)
                            {
                                // https://ffri.github.io/ProjectChameleon/new_reloc_chpev2/#new-dynamic-value-relocation-table-dvrt-image_dynamic_relocation_arm64x

                                // typedef struct {
                                //   DWORD VirtualAddress; // 4,096 bytes page aligned
                                //   DWORD SizeOfBlock;
                                // } IMAGE_DYNAMIC_RELOCATION_ARM64X_BLOCK;

                                var IMAGE_DYNAMIC_RELOCATION_ARM64X_BLOCK = subProvider.Provide(rva, 8);
                                rva += 8;

                                var targetRva = BinaryPrimitives.ReadInt32LittleEndian(IMAGE_DYNAMIC_RELOCATION_ARM64X_BLOCK);
                                var sizeOfBlock = BinaryPrimitives.ReadInt32LittleEndian(IMAGE_DYNAMIC_RELOCATION_ARM64X_BLOCK.Slice(4));

                                var blockRva = rva;
                                rva += sizeOfBlock - 8;
                                var blockRvaEnd = rva;

                                ParseEntries(blockRva, blockRvaEnd, targetRva);
                            }
                        }

                        void ParseEntries(int rva, int rvaEnd, int targetRva)
                        {
                            while (rva < rvaEnd)
                            {
                                var word = BinaryPrimitives.ReadUInt16LittleEndian(subProvider.Provide(rva, 2));
                                rva += 2;

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

                                            subProvider.Patch(
                                                rva: targetRva + offset,
                                                content: new byte[size]
                                            );
                                        }
                                        break;
                                    case 1: // assign
                                        {
                                            var offset = word & 0x0FFF;
                                            var size = 1 << ((meta >> 2) & 3);

                                            subProvider.Patch(
                                                rva: targetRva + offset,
                                                content: subProvider.Provide(rva, size)
                                            );

                                            rva += size;
                                        }
                                        break;
                                    case 2: // add or sub
                                        {
                                            var offset = word & 0x0FFF;
                                            var sign = ((meta & 4) != 0) ? -1 : 1;
                                            var scale = ((meta & 8) != 0) ? 8 : 4;

                                            var rwAt = targetRva + offset;

                                            var content = subProvider.Provide(rwAt, 4).ToArray();

                                            var data = BinaryPrimitives.ReadUInt16LittleEndian(subProvider.Provide(rva, 2));

                                            BinaryPrimitives.WriteUInt32LittleEndian(
                                                content,
                                                (uint)(
                                                    BinaryPrimitives.ReadUInt32LittleEndian(content)
                                                    + sign * scale * data
                                                )
                                            );

                                            subProvider.Patch(
                                                rva: rwAt,
                                                content: content
                                            );

                                            rva += 2;
                                        }
                                        break;
                                    case 3:
                                        //TODO: untested
                                        throw new NotImplementedException();
                                }
                            }
                        }
                    }
                    else
                    {
                        return new ApplyDvrtResult(RvaStart: 0, RvaEnd: 0);
                    }
                }

                bool HasDvrtToMakeX64()
                {
                    var subProvider = new PatchableVASpanProvider(provider.Provide);
                    ApplyDvrt(subProvider);

                    var machine = BinaryPrimitives.ReadUInt16LittleEndian(
                        subProvider.Provide(
                            header.MachineOffset,
                            2
                        )
                    );
                    return machine == 0x8664;
                }

                return new LookAtLoadConfig1(
                    GetCHPEVersion: GetCHPEVersion,
                    HasDvrtToMakeX64: HasDvrtToMakeX64,
                    ApplyDvrt: ApplyDvrt,
                    GetCHPEMetadataPointer: GetCHPEMetadataPointer);
            }
            else
            {
                return null;
            }
        }

        public record ApplyDvrtResult(int RvaStart, int RvaEnd);
    }
}
