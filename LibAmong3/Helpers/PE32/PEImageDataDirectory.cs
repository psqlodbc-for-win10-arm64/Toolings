using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public record PEImageDataDirectory(
        int VirtualAddress,
        int Size)
    {
        public static readonly PEImageDataDirectory Empty = new PEImageDataDirectory(0, 0);
    }
}
