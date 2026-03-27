using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32.Deeper
{
    public class LookAtLoadConfig1
    {
        public Func<int> GetCHPEVersion { get; } = () => 0;
        public Func<bool> HasDvrtToMakeX64 { get; } = () => false;
        public Func<ProvideReadOnlySpanDelegate> ApplyDvrt { get; } = () => throw new NotSupportedException();

        public LookAtLoadConfig1(ReadOnlyMemory<byte> exe)
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
                GetCHPEVersion = () =>
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
                };
                ApplyDvrt = () =>
                {
                    var replaces = new List<Replace>();

                    if (loadConfigDir.Header1.DynamicValueRelocTableSection != 0)
                    {
                        var rva = header.Sections[loadConfigDir.Header1.DynamicValueRelocTableSection - 1].VirtualAddress + (int)loadConfigDir.Header1.DynamicValueRelocTableOffset;

                        var dvrtHeader = new ParseDvrtHeader().Parse(
                            provider.Provide,
                            rva
                        );

                        var parseRelocationArm64X = new ParseRelocationArm64X();

                        foreach (var relocationSet in dvrtHeader.RelocationSets
                            .Where(it => it.Symbol == 6) // IMAGE_DYNAMIC_RELOCATION_ARM64X
                        )
                        {
                            foreach (var relocation in relocationSet.Relocations)
                            {
                                var reloc = parseRelocationArm64X.Parse(
                                    provider.Provide(
                                        relocation.Rva,
                                        relocation.BaseRelocSize
                                    )
                                );
                                foreach (var group in reloc.Groups)
                                {
                                    foreach (var entry in group.Entries)
                                    {
                                        var type = (entry.Meta & 3);
                                        if (type == 0)
                                        {
                                            // zero fill
                                            var size = 1 << ((entry.Meta >> 2) & 3);
                                            replaces.Add(
                                                new Replace(
                                                    group.Rva + entry.Offset,
                                                    size,
                                                    new byte[size]
                                                )
                                            );
                                        }
                                        else if (type == 1)
                                        {
                                            // assign
                                            replaces.Add(
                                                new Replace(
                                                    group.Rva + entry.Offset,
                                                    entry.Content.Length,
                                                    entry.Content
                                                )
                                            );
                                        }
                                        else if (type == 2)
                                        {
                                            // binary op
                                            var value = BinaryPrimitives.ReadUInt32LittleEndian(provider.Provide(relocation.Rva + entry.Offset, 4));
                                            var data = BinaryPrimitives.ReadInt16LittleEndian(entry.Content);
                                            var scale = ((entry.Meta & 4) != 0) ? 8 : 4;
                                            var sign = ((entry.Meta & 8) != 0) ? -1 : 1;

                                            var bytes = new byte[4];
                                            BinaryPrimitives.WriteUInt32LittleEndian(
                                                bytes,
                                                value + (uint)(sign * scale * data)
                                            );

                                            replaces.Add(
                                                new Replace(
                                                    group.Rva + entry.Offset,
                                                    4,
                                                    bytes
                                                )
                                            );
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return (rva, size) =>
                    {
                        var any = false;
                        foreach (var one in replaces)
                        {
                            any |= one.Rva <= rva && rva < one.Rva + one.Size;
                            any |= rva < one.Rva && one.Rva < rva + size;
                        }
                        if (any)
                        {
                            var bytes = provider.Provide(rva, size).ToArray();

                            foreach (var one in replaces)
                            {
                                if (one.Rva <= rva && rva < one.Rva + one.Size)
                                {
                                    one.ReplaceWith.Slice(
                                        rva - one.Rva,
                                        Math.Min(one.Rva + one.Size - rva, size)
                                    )
                                        .CopyTo(
                                            bytes
                                        );
                                }
                                else if (rva < one.Rva && one.Rva < rva + size)
                                {
                                    one.ReplaceWith.Slice(
                                        0,
                                        Math.Min(rva + size - one.Rva, one.Size)
                                    )
                                        .CopyTo(
                                            bytes.AsMemory(one.Rva - rva)
                                        );
                                }
                            }

                            return bytes;
                        }
                        else
                        {
                            return provider.Provide(rva, size);
                        }
                    };
                };
                HasDvrtToMakeX64 = () =>
                {
                    var subProvider = ApplyDvrt();
                    var machine = BinaryPrimitives.ReadUInt16LittleEndian(
                        subProvider(
                            header.MachineOffset,
                            2
                        )
                    );
                    return machine == 0x8664;
                };
            }
        }

        private record Replace(int Rva, int Size, ReadOnlyMemory<byte> ReplaceWith);
    }
}
