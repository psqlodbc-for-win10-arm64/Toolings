using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public record VAReadOnlySpanProvider(
        ReadOnlyMemory<byte> Exe,
        IReadOnlyList<PESection> Sections)
    {
        /// <param name="size">Number of bytes. If positive numbers, it must return the minimum bytes requested. If negative numbers are provided, it returns the requested bytes, which may be lower than specified.</param>
        public ReadOnlySpan<byte> Provide(int rva, int size)
        {
            var section = Sections
                .SingleOrDefault(it => true
                    && it.VirtualAddress <= rva
                    && rva < it.VirtualAddress + it.SizeOfRawData
                );

            if (section == null)
            {
                throw new ArgumentException($"Invalid virtual address {rva:X8}");
            }

            var offset = rva - section.VirtualAddress;

            if (size < 0)
            {
                var availableSize = section.SizeOfRawData - offset;
                var returnSize = Math.Min(availableSize, -size);
                return Exe.Span.Slice(section.PointerToRawData + offset, returnSize);
            }
            else
            {
                return Exe.Span.Slice(section.PointerToRawData + offset, size);
            }
        }
    }
}
