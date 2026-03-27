using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    /// <see cref="https://ffri.github.io/ProjectChameleon/new_reloc_chpev2/#new-dynamic-value-relocation-table-dvrt-image_dynamic_relocation_arm64x"/>
    public class ParseRelocationArm64X
    {
        public RelocationArm64X Parse(
            ReadOnlySpan<byte> binary
        )
        {
            var entryGroups = new List<RelocationArm64X.EntryGroup>();

            while (binary.Length != 0)
            {
                var entries = new List<RelocationArm64X.Entry>();

                var rva = BinaryPrimitives.ReadInt32LittleEndian(binary);
                binary = binary.Slice(4);
                var sizeOfBlock = BinaryPrimitives.ReadInt32LittleEndian(binary);
                binary = binary.Slice(4);

                var block = binary.Slice(0, sizeOfBlock - 8);
                binary = binary.Slice(sizeOfBlock - 8);

                while (block.Length != 0)
                {
                    var word = BinaryPrimitives.ReadUInt16LittleEndian(block);
                    block = block.Slice(2);

                    if (word == 0)
                    {
                        // padding?
                        break;
                    }

                    var meta = (word >> 12) & 15;
                    var contentSize = 0;
                    if ((meta & 3) == 1)
                    {
                        contentSize = 1 << (meta >> 2);
                    }
                    else if ((meta & 3) == 2)
                    {
                        contentSize = 2;
                    }

                    entries.Add(
                        new RelocationArm64X.Entry(
                            Offset: word & 0x0FFF,
                            Meta: meta,
                            Content: block.Slice(0, contentSize).ToArray()
                        )
                    );

                    block = block.Slice(contentSize);
                }

                entryGroups.Add(
                    new RelocationArm64X.EntryGroup(
                        Rva: rva,
                        Entries: entries.AsReadOnly()
                    )
                );
            }

            return new RelocationArm64X(
                Groups: entryGroups.AsReadOnly()
            );
        }
    }
}
