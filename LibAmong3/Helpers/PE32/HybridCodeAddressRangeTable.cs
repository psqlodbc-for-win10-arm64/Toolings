using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LibAmong3.Helpers.PE32.HybridCodeAddressRangeTable;

namespace LibAmong3.Helpers.PE32
{
    public record HybridCodeAddressRangeTable(
        IReadOnlyList<HybridCodeAddressRangeEntry> Entries
        )
    {
        public static string GetAbiType(int abiType)
        {
            switch (abiType)
            {
                case 0: return "arm64";
                case 1: return "arm64ec";
                case 2: return "x64";
                default: return abiType.ToString();
            }
        }

        public record HybridCodeAddressRangeEntry(
            int RvaFrom,
            int AbiType,
            int Size);
    }
}
