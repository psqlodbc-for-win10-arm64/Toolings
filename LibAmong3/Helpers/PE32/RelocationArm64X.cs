using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public record RelocationArm64X(IReadOnlyList<RelocationArm64X.Entry> Entries)
    {
        public record Entry(int Offset, int Meta, byte[] Content);
    }
}
