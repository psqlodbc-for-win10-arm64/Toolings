using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public class PEIData
    {
        public record Directory(
            IReadOnlyList<Import> ImportRefs,
            uint Timestamp,
            uint ForwarderChain,
            string DLLName,
            IReadOnlyList<Import> ImportAddrs);

        public record Import(
            bool IsOrdinal,
            long Ordinal,
            ushort Hint,
            string Name);
    }
}
