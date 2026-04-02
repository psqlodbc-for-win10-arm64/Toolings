using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public record ChpeV2Header(
        int Version,
        int HybridCodeAddressRangeTable,
        int HybridCodeAddressRangeCount,
        int OffsetOfArm64XX64CodeRangesToEntryPointsTable,
        
        int OffsetOfArm64XArm64xRedirectionMetadataTable,
        int OffsetOfArm64XDispatchCallFunctionPointer,
        int OffsetOfArm64XDispatchReturnFunctionPointer,
        int Unk12,
        
        int OffsetOfArm64XDispatchIndirectCallFunctionPointer,
        int OffsetOfArm64XDispatchIndirectCallFunctionPointerWithCFGCheck,
        int OffsetOfArm64XAlternativeEntryPoint,
        int OffsetOfArm64XAuxiliaryImportAddressTable,
        
        int CountOfArm64XX64CodeRangesToEntryPointTableEntries,
        int CountOfArm64XArm64xRedirectionMetadataTableEntries,
        int Unk38,
        int Unk3C,
        
        int OffsetOfArm64XExtraRFETable,
        int CountOfArm64XExtraRFETableSize,
        int OffsetOfArm64XDispatchFunctionPointer,
        int OffsetOfArm64XCopyOfAuxiliaryImportAddressTable,
        
        int OffsetOfArm64XAuxiliaryDelayloadImportAddressTable,
        int OffsetOfArm64XAuxiliaryDelayloadImportAddressTableCopy,
        int ValueOfArm64XHybridImageInfoBitfield)
    {
    }
}
