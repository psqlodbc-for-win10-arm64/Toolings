using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LibAmong3.Helpers.PE32.PEDvrtHeader;

namespace LibAmong3.Helpers.PE32
{
    public record PEDvrtHeader(IReadOnlyList<RelocationSet> RelocationSets)
    {
        public record RelocationSet(ulong Symbol, IReadOnlyList<Relocation> Relocations);
        public record Relocation(int Rva, int BaseRelocSize);
    }
}
