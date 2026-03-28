using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    /// <summary>
    /// An on the fly patchable wrapper of ProvideReadOnlySpanDelegate
    /// </summary>
    /// <param name="ParentProvide"></param>
    public record PatchableVASpanProvider(
        ProvideReadOnlySpanDelegate ParentProvide
        )
    {
        public List<PatchRecord> PatchRecords { get; } = new List<PatchRecord>();

        /// <param name="size">Number of bytes. If positive numbers, it must return the minimum bytes requested. If negative numbers are provided, it returns the requested bytes, which may be lower than specified.</param>
        public ReadOnlySpan<byte> Provide(int rva, int size)
        {
            var pre = ReadOnlySpan<byte>.Empty;
            if (size < 0)
            {
                pre = ParentProvide(rva, size);
                size = pre.Length;
            }

            var any = false;
            foreach (var one in PatchRecords)
            {
                any |= one.Rva <= rva && rva < one.Rva + one.Bytes.Length;
                any |= rva < one.Rva && one.Rva < rva + size;
            }
            if (any)
            {
                var bytes = (pre.Length != 0)
                    ? pre.ToArray()
                    : ParentProvide(rva, size).ToArray();

                foreach (var one in PatchRecords)
                {
                    if (one.Rva <= rva && rva < one.Rva + one.Bytes.Length)
                    {
                        one.Bytes.Slice(
                            rva - one.Rva,
                            Math.Min(one.Rva + one.Bytes.Length - rva, size)
                        )
                            .CopyTo(
                                bytes
                            );
                    }
                    else if (rva < one.Rva && one.Rva < rva + size)
                    {
                        one.Bytes.Slice(
                            0,
                            Math.Min(rva + size - one.Rva, one.Bytes.Length)
                        )
                            .CopyTo(
                                bytes.AsMemory(one.Rva - rva)
                            );
                    }
                }

                return bytes;
            }
            else
            {
                return ParentProvide(rva, size);
            }
        }

        public void Patch(int rva, ReadOnlySpan<byte> content)
        {
            PatchRecords.Add(
                new PatchRecord(
                    rva,
                    content.ToArray().AsMemory()
                )
            );
        }

        public record PatchRecord(int Rva, ReadOnlyMemory<byte> Bytes);
    }
}
