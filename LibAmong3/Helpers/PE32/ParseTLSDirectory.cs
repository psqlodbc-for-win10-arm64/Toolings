using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public class ParseTLSDirectory
    {
        public PETLSDirectory Parse(
            ProvideReadOnlySpanDelegate provide,
            int virtualAddress,
            bool isPE32Plus
        )
        {
            var span = provide(virtualAddress, isPE32Plus ? 40 : 24);
            var header1 = new PETLSDirectory.HeaderType1(
                StartOfRawData: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                EndOfRawData: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                AddressOfIndex: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                AddressOfCallback: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                SizeOfZeroFill: ReadHelper.U32(ref span),
                Characteristics: ReadHelper.U32(ref span)
            );
            return new PETLSDirectory(header1);
        }
    }
}
