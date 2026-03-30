using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LibAmong3.Helpers.PE32
{
    /// <see href="https://learn.microsoft.com/ja-jp/windows/win32/debug/pe-format#the-tls-section"/>
    public record PETLSDirectory(
        PETLSDirectory.HeaderType1 Header1)
    {
        public record HeaderType1(
            ulong StartOfRawData,
            ulong EndOfRawData,
            ulong AddressOfIndex,
            ulong AddressOfCallback,
            uint SizeOfZeroFill,
            uint Characteristics);
    }
}
