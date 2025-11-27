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
            var entries = new List<RelocationArm64X.Entry>();

            while (binary.Length != 0)
            {
                var word = BinaryPrimitives.ReadUInt16LittleEndian(binary);
                binary = binary.Slice(2);

                var meta = (word >> 12) & 15;
                var size = 0;
                if ((meta & 3) == 1)
                {
                    size = 1 << (meta >> 2);
                }

                entries.Add(
                    new RelocationArm64X.Entry(
                        Offset: word & 0x0FFF,
                        Meta: meta,
                        Content: binary.Slice(0, size).ToArray()
                    )
                );

                binary = binary.Slice(size);
            }

            return new RelocationArm64X(
                Entries: entries.AsReadOnly()
            );
        }
    }
}
