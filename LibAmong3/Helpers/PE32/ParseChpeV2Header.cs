using LibAmong3.Helpers.PE32.DvrtModel1;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static LibAmong3.Helpers.PE32.PEIData;
using static LibAmong3.Helpers.PE32.RelocationArm64X;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LibAmong3.Helpers.PE32
{
    public class ParseChpeV2Header
    {
        public ChpeV2Header Parse(
            ProvideReadOnlySpanDelegate provide,
            int virtualAddress)
        {
            var span = provide(virtualAddress, 0x5C);
            // Reading output from `dumpbin.exe /all ...`
            return new ChpeV2Header(
                Version: BinaryPrimitives.ReadInt32LittleEndian(span),
                HybridCodeAddressRangeTable: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(4)),
                HybridCodeAddressRangeCount: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(8)),
                OffsetOfArm64XX64CodeRangesToEntryPointsTable: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(12)),

                OffsetOfArm64XArm64xRedirectionMetadataTable: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(16)),
                OffsetOfArm64XDispatchCallFunctionPointer: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(20)),
                OffsetOfArm64XDispatchReturnFunctionPointer: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(24)),
                Unk12: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(28)),

                OffsetOfArm64XDispatchIndirectCallFunctionPointer: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(32)),
                OffsetOfArm64XDispatchIndirectCallFunctionPointerWithCFGCheck: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(36)),
                OffsetOfArm64XAlternativeEntryPoint: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(40)),
                OffsetOfArm64XAuxiliaryImportAddressTable: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(44)),

                CountOfArm64XX64CodeRangesToEntryPointTableEntries: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(48)),
                CountOfArm64XArm64xRedirectionMetadataTableEntries: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(52)),
                Unk38: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(56)),
                Unk3C: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(60)),

                OffsetOfArm64XExtraRFETable: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(64)),
                CountOfArm64XExtraRFETableSize: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(68)),
                OffsetOfArm64XDispatchFunctionPointer: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(72)),
                OffsetOfArm64XCopyOfAuxiliaryImportAddressTable: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(76)),

                OffsetOfArm64XAuxiliaryDelayloadImportAddressTable: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(80)),
                OffsetOfArm64XAuxiliaryDelayloadImportAddressTableCopy: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(84)),
                ValueOfArm64XHybridImageInfoBitfield: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(88))
            );
        }
    }
}
