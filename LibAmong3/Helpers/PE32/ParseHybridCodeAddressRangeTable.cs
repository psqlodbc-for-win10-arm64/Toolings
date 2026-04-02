using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public class ParseHybridCodeAddressRangeTable
    {
        public HybridCodeAddressRangeTable Parse(
            ProvideReadOnlySpanDelegate provide,
            int hybridCodeAddressRangeTable,
            int hybridCodeAddressRangeCount)
        {
            var span = provide(hybridCodeAddressRangeTable, 8 * hybridCodeAddressRangeCount);
            // Reading output from `dumpbin.exe /all ...`

            var list = new List<HybridCodeAddressRangeTable.HybridCodeAddressRangeEntry>();

            for (int y = 0; y < hybridCodeAddressRangeCount; y++)
            {
                int typeNum = span[8 * y] & 3;

                var rvaFrom = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(8 * y)) & ~3;
                var size = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(8 * y + 4));

                list.Add(new HybridCodeAddressRangeTable.HybridCodeAddressRangeEntry(
                    RvaFrom: rvaFrom,
                    AbiType: typeNum,
                    Size: size
                ));
            }
            return new HybridCodeAddressRangeTable(
                Entries: list.AsReadOnly()
            );
        }
    }
}
