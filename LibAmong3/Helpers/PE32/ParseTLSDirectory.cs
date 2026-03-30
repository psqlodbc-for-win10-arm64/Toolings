using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
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

        public IReadOnlyCollection<ulong> ParseCallbacks(
            ProvideReadOnlySpanDelegate provide,
            ulong addressOfCallback,
            ulong imageBase,
            bool isPE32Plus
            )
        {
            var tlsCallbacks = new List<ulong>();

            var rva = addressOfCallback - imageBase;

            for (int x = 0; ; x++)
            {
                var one = isPE32Plus
                    ? BinaryPrimitives.ReadUInt64LittleEndian(
                        provide(Convert.ToInt32(rva + (uint)(8 * x)), 8)
                    )
                    : BinaryPrimitives.ReadUInt32LittleEndian(
                        provide(Convert.ToInt32(rva - imageBase + (uint)(4 * x)), 4)
                    );
                tlsCallbacks.Add(one);
                if (one == 0)
                {
                    break;
                }
            }

            return tlsCallbacks.AsReadOnly();
        }
    }
}
