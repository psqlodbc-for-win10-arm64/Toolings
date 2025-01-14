using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class MakeCoffHelper
    {
        public byte[] Make(IEnumerable<(ushort Magic, string Extension, byte[] Body)> objs)
        {
            int Align4(int value) => (value + 3) & ~3;

            // See https://wiki.osdev.org/COFF

            var size = (2 + 2 + 4 + 4 + 4 + 2 + 2) + (8 + 4 + 4 + 4 + 4 + 4 + 4 + 2 + 2 + 4) * objs.Count() + objs.Sum(it => Align4(it.Body.Length)) + (4);
            var buf = new byte[size];

            var top = buf.AsSpan();
            var span = top;

            // File Header
            BinaryPrimitives.WriteUInt16LittleEndian(span, 0x14c); // f_magic
            span = span.Slice(2);
            BinaryPrimitives.WriteUInt16LittleEndian(span, Convert.ToUInt16(objs.Count())); // f_nscns
            span = span.Slice(2);
            BinaryPrimitives.WriteUInt32LittleEndian(span, 0); // f_timdat
            span = span.Slice(4);
            BinaryPrimitives.WriteUInt32LittleEndian(span, Convert.ToUInt32(size - 4)); // f_symptr
            span = span.Slice(4);
            BinaryPrimitives.WriteUInt32LittleEndian(span, 0); // f_nsyms
            span = span.Slice(4);
            BinaryPrimitives.WriteUInt16LittleEndian(span, 0); // f_opthdr
            span = span.Slice(2);
            BinaryPrimitives.WriteUInt16LittleEndian(span, 0); // f_flags
            span = span.Slice(2);

            var nextOffsetToRaw = 20 + 40 * objs.Count();

            // Section Header
            foreach (var (Magic, Extension, Body) in objs)
            {
                var name = Encoding.ASCII.GetBytes($"{Magic:X04}{Extension}");
                name.CopyTo(span);
                span = span.Slice(8);
                BinaryPrimitives.WriteUInt32LittleEndian(span, 0); // s_paddr
                span = span.Slice(4);
                BinaryPrimitives.WriteUInt32LittleEndian(span, 0); // s_vaddr
                span = span.Slice(4);
                BinaryPrimitives.WriteUInt32LittleEndian(span, Convert.ToUInt32(Body.Length)); // s_size
                span = span.Slice(4);
                BinaryPrimitives.WriteInt32LittleEndian(span, nextOffsetToRaw); // s_scnptr
                span = span.Slice(4);
                BinaryPrimitives.WriteUInt32LittleEndian(span, 0); // s_relptr
                span = span.Slice(4);
                BinaryPrimitives.WriteUInt32LittleEndian(span, 0); // s_lnnoptr
                span = span.Slice(4);
                BinaryPrimitives.WriteUInt16LittleEndian(span, 0); // s_nreloc
                span = span.Slice(2);
                BinaryPrimitives.WriteUInt16LittleEndian(span, 0); // s_nlnno
                span = span.Slice(2);
                BinaryPrimitives.WriteUInt32LittleEndian(span, 0); // s_flags
                span = span.Slice(4);

                Body.CopyTo(top.Slice(nextOffsetToRaw));
                nextOffsetToRaw += Align4(Body.Length);
            }

            // String Table

            return buf;
        }
    }
}
