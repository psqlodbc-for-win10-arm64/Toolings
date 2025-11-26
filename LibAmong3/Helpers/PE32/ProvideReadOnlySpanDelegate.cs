using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    /// <param name="size">Number of bytes. If positive numbers, it must return the minimum bytes requested. If negative numbers are provided, it returns the requested bytes, which may be lower than specified.</param>
    public delegate ReadOnlySpan<byte> ProvideReadOnlySpanDelegate(int virtualAddress, int size);
}
