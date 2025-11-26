using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public static class ReadCStringHelper
    {
        public static string ReadCString(
            ProvideReadOnlySpanDelegate provide,
            int rva
        )
        {
            int length = 0;

            var span = provide(rva, -256);

            while (true)
            {
                if (span[length] == 0)
                {
                    return Encoding.Latin1.GetString(provide(rva, length));
                }
                else
                {
                    length += 1;
                }
            }
        }
    }
}
