using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public record VAReadOnlyMemoryProvider(
        byte[] Exe,
        IReadOnlyList<PESection> Sections)
    {
        /// <param name="size">Number of bytes. If positive numbers, it must return the minimum bytes requested. If negative numbers are provided, it returns the requested bytes, which may be lower than specified.</param>
        public ReadOnlyMemory<byte> Provide(int virtualAddress, int size)
        {
            var section = Sections
                .SingleOrDefault(it => true
                    && it.VirtualAddress <= virtualAddress
                    && virtualAddress < it.VirtualAddress + it.SizeOfRawData
                );

            if (section == null)
            {
                throw new ArgumentException("Invalid virtual address");
            }

            var offset = virtualAddress - section.VirtualAddress;

            if (size < 0)
            {
                var availableSize = section.SizeOfRawData - offset;
                var returnSize = Math.Min(availableSize, -size);
                return Exe.AsMemory(section.PointerToRawData + offset, returnSize);
            }
            else
            {
                return Exe.AsMemory(section.PointerToRawData + offset, size);
            }
        }
    }
}
