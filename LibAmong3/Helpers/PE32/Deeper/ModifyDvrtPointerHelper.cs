using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32.Deeper
{
    public class ModifyDvrtPointerHelper
    {
        public bool ModifyDvrtPointer(
            Memory<byte> exe,
            int offset,
            ushort section,
            Action<string> logWarn,
            Action<string> logError)
        {
            var parseHeader = new ParseHeader();
            var header = parseHeader.Parse(exe);
            var loadConfigDirEntry = header.GetImageDirectoryOrEmpty(10);
            if (loadConfigDirEntry.Size != 0)
            {
                var parseLoadConfigDir = new ParseLoadConfigDir();
                var provider = new VAReadOnlySpanProvider(
                    exe,
                    header.Sections
                );

                var isPE32Plus = header.IsPE32Plus;
                var virtualAddress = loadConfigDirEntry.VirtualAddress;

                var size = BinaryPrimitives.ReadInt32LittleEndian(provider.Provide(virtualAddress, 4));
                var sizeFixedUp = (!isPE32Plus && size == 0)
                    ? 64
                    : size;

                // GuardRFFailureRoutineFunctionPointer      | 136 | 224
                // DynamicValueRelocTableOffset              | 140 | 228
                // DynamicValueRelocTableSection             | 142 | 230

                if (isPE32Plus)
                {
                    if (230 <= sizeFixedUp)
                    {
                        var pair = provider.Locate(virtualAddress + 224, 6);
                        var span = exe.Slice(pair.Start, pair.Length).Span;
                        BinaryPrimitives.WriteInt32LittleEndian(span, offset);
                        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(4), section);
                    }
                    else
                    {
                        logWarn("DynamicValueRelocTableOffset and Section isn't included.");
                    }
                }
                else
                {
                    if (142 <= sizeFixedUp)
                    {
                        var pair = provider.Locate(virtualAddress + 136, 6);
                        var span = exe.Slice(pair.Start, pair.Length).Span;
                        BinaryPrimitives.WriteInt32LittleEndian(span, offset);
                        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(4), section);
                    }
                    else
                    {
                        logWarn("DynamicValueRelocTableOffset and Section isn't included.");
                    }
                }

                return true;
            }
            else
            {
                logError("Error: loadConfigDirEntry not found!");
                return false;
            }
        }
    }
}
