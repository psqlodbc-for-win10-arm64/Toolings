using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public record PEEData
    {
        public record Directory(
            uint ExportFlags,
            uint Timestamp,
            ushort MajorVersion,
            ushort MinorVersion,
            string DLLName,
            uint BaseOrdinal,
            IReadOnlyList<uint> AddrTable,
            IReadOnlyList<string> ExportNames,
            IReadOnlyList<uint> OrdinalTable
        );
    }
}
