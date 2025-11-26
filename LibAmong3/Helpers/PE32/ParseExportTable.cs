using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public class ParseExportTable
    {
        public PEEData.Directory Parse(
            ProvideReadOnlySpanDelegate provide,
            int virtualAddress,
            bool isPE32Plus)
        {
            var headerSpan = provide(virtualAddress, 40);
            var dllNameOffset = BinaryPrimitives.ReadInt32LittleEndian(headerSpan.Slice(12, 4));
            var numOfAddrTableEntries = BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(20, 4));
            var numOfNamePointers = BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(24, 4));

            var addrTableEntries = new uint[numOfAddrTableEntries];
            {
                var addrTableEntriesRVA = BinaryPrimitives.ReadInt32LittleEndian(headerSpan.Slice(28, 4));
                var addrTableEntriesSpan = provide(addrTableEntriesRVA, Convert.ToInt32(numOfAddrTableEntries) * 4);
                for (uint x = 0; x < numOfAddrTableEntries; x++)
                {
                    addrTableEntries[x] = BinaryPrimitives.ReadUInt32LittleEndian(addrTableEntriesSpan);
                    addrTableEntriesSpan = addrTableEntriesSpan.Slice(4);
                }
            }

            var names = new string[numOfNamePointers];
            {
                var namePointerRVA = BinaryPrimitives.ReadInt32LittleEndian(headerSpan.Slice(32, 4));
                var namePointersSpan = provide(namePointerRVA, Convert.ToInt32(numOfNamePointers) * 4);
                for (uint x = 0; x < numOfNamePointers; x++)
                {
                    names[x] = ReadCStringHelper.ReadCString(
                        provide,
                        BinaryPrimitives.ReadInt32LittleEndian(namePointersSpan)
                    );
                    namePointersSpan = namePointersSpan.Slice(4);
                }
            }

            var ordTable = new uint[numOfNamePointers];
            {
                var ordTableRVA = BinaryPrimitives.ReadInt32LittleEndian(headerSpan.Slice(36, 4));
                var ordTableSpan = provide(ordTableRVA, Convert.ToInt32(numOfNamePointers) * 4);
                for (uint x = 0; x < numOfNamePointers; x++)
                {
                    ordTable[x] = BinaryPrimitives.ReadUInt16LittleEndian(ordTableSpan);
                    ordTableSpan = ordTableSpan.Slice(2);
                }
            }

            return new PEEData.Directory(
                ExportFlags: BinaryPrimitives.ReadUInt32LittleEndian(headerSpan),
                Timestamp: BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(4, 4)),
                MajorVersion: BinaryPrimitives.ReadUInt16LittleEndian(headerSpan.Slice(8, 2)),
                MinorVersion: BinaryPrimitives.ReadUInt16LittleEndian(headerSpan.Slice(10, 2)),
                DLLName: ReadCStringHelper.ReadCString(provide, dllNameOffset),
                BaseOrdinal: BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(16, 4)),
                AddrTable: addrTableEntries,
                ExportNames: names,
                OrdinalTable: ordTable
                );
        }
    }
}
