using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public record PESection(
        string Name,
        int VirtualAddress,
        int SizeOfRawData,
        int PointerToRawData)
    {
    }
}
