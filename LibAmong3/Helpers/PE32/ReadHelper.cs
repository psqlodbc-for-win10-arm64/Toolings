using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    internal static class ReadHelper
    {
        public static uint U32(ref ReadOnlySpan<byte> span)
        {
            var value = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(0, 4));
            span = span.Slice(4);
            return value;
        }

        public static ushort U16(ref ReadOnlySpan<byte> span)
        {
            var value = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0, 2));
            span = span.Slice(2);
            return value;
        }

        public static ulong U64(ref ReadOnlySpan<byte> span)
        {
            var value = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(0, 8));
            span = span.Slice(8);
            return value;
        }

        public static ulong U64OrZero(ref ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
            {
                return 0;
            }
            else
            {
                var value = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(0, 8));
                span = span.Slice(8);
                return value;
            }
        }

        public static byte[] Bytes(ref ReadOnlySpan<byte> span, int numBytes)
        {
            var value = span.Slice(0, numBytes).ToArray();
            span = span.Slice(numBytes);
            return value;
        }

        public static uint U32OrZero(ref ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
            {
                return 0;
            }
            else
            {
                var value = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(0, 4));
                span = span.Slice(4);
                return value;
            }
        }

        public static byte[] BytesOrEmpty(ref ReadOnlySpan<byte> span, int numBytes)
        {
            if (span.Length == 0)
            {
                return Array.Empty<byte>();
            }
            else
            {
                var value = span.Slice(0, numBytes).ToArray();
                span = span.Slice(numBytes);
                return value;
            }
        }

        public static ushort U16OrZero(ref ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
            {
                return 0;
            }
            else
            {
                var value = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0, 2));
                span = span.Slice(2);
                return value;
            }
        }
    }
}
