using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LibAmong3.Helpers.PE32.PEDvrtHeader;

namespace LibAmong3.Helpers.PE32
{
    /// <see cref="https://denuvosoftwaresolutions.github.io/DVRT/dvrt.html"/>
    /// <see cref="https://ffri.github.io/ProjectChameleon/new_reloc_chpev2/#new-dynamic-value-relocation-table-dvrt-image_dynamic_relocation_arm64x"/>
    public class ParseDvrtHeader
    {
        public PEDvrtHeader Parse(
            ProvideReadOnlySpanDelegate provide,
            int virtualAddress)
        {
            var relocationSets = new List<RelocationSet>();

            while (true)
            {
                var version = BinaryPrimitives.ReadInt32LittleEndian(provide(virtualAddress, 4));
                if (version != 1)
                {
                    break;
                }

                var size = BinaryPrimitives.ReadInt32LittleEndian(provide(virtualAddress + 4, 4));
                if (size < 8)
                {
                    break;
                }

                var nextVirtualAddress = virtualAddress + 8 + size;

                var symbol = BinaryPrimitives.ReadUInt64LittleEndian(provide(virtualAddress + 8, 8));
                var baseRelocSize = BinaryPrimitives.ReadInt32LittleEndian(provide(virtualAddress + 16, 4));

                relocationSets.Add(
                    new RelocationSet(
                        Symbol: symbol,
                        Relocations: new Relocation[] { new Relocation(virtualAddress + 20, baseRelocSize), }
                    )
                );

                virtualAddress = nextVirtualAddress;
            }

            return new PEDvrtHeader(
                RelocationSets: relocationSets.AsReadOnly()
            );
        }
    }
}
