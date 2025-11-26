using Microsoft.VisualBasic;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public class ParseLoadConfigDir
    {
        public PELoadConfigDir Parse(
            ProvideReadOnlySpanDelegate provide,
            int virtualAddress,
            bool isPE32Plus
        )
        {
            var size = BinaryPrimitives.ReadInt32LittleEndian(provide(virtualAddress, 4));
            var span = provide(virtualAddress, size).Slice(4);
            var header1 = new PELoadConfigDir.HeaderType1(
                Size: (uint)size,
                TimeDateStamp: ReadHelper.U32(ref span),
                MajorVersion: ReadHelper.U16(ref span),
                MinorVersion: ReadHelper.U16(ref span),
                GlobalFlagsClear: ReadHelper.U32(ref span),
                GlobalFlagsSet: ReadHelper.U32(ref span),
                CriticalSectionDefaultTimeout: ReadHelper.U32(ref span),
                DeCommitFreeBlockThreshold: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                DeCommitTotalFreeThreshold: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                LockPrefixTable: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                MaximumAllocationSize: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                VirtualMemoryThreshold: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                ProcessAffinityMask: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                ProcessHeapFlags: ReadHelper.U32(ref span),
                CSDVersion: ReadHelper.U16(ref span),
                DependentLoadFlags: ReadHelper.U16(ref span),
                EditList: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                SecurityCookie: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                SEHandlerTable: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                SEHandlerCount: isPE32Plus ? ReadHelper.U64(ref span) : ReadHelper.U32(ref span),
                GuardCFCheckFunctionPointer: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardCFDispatchFunctionPointer: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardCFFunctionTable: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardCFFunctionCount: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardFlags: isPE32Plus ? ReadHelper.U32OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                CodeIntegrity: isPE32Plus ? ReadHelper.BytesOrEmpty(ref span, 12) : ReadHelper.BytesOrEmpty(ref span, 12),
                GuardAddressTakenIatEntryTable: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardAddressTakenIatEntryCount: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardLongJumpTargetTable: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardLongJumpTargetCount: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                DynamicValueRelocTable: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                CHPEMetadataPointer: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardRFFailureRoutine: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardRFFailureRoutineFunctionPointer: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                DynamicValueRelocTableOffset: isPE32Plus ? ReadHelper.U32OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                DynamicValueRelocTableSection: isPE32Plus ? ReadHelper.U16OrZero(ref span) : ReadHelper.U16OrZero(ref span),
                Reserved2: isPE32Plus ? ReadHelper.U16OrZero(ref span) : ReadHelper.U16OrZero(ref span),
                GuardRFVerifyStackPointerFunctionPointer: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                HotPatchTableOffset: isPE32Plus ? ReadHelper.U32OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                Reserved3: isPE32Plus ? ReadHelper.U32OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                EnclaveConfigurationPointer: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                VolatileMetadataPointer: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardEHContinuationTable: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardEHContinuationCount: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardXFGCheckFunctionPointer: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardXFGDispatchFunctionPointer: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardXFGTableDispatchFunctionPointer: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                CastGuardOsDeterminedFailureMode: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                GuardMemcpyFunctionPointer: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span),
                UmaFunctionPointers: isPE32Plus ? ReadHelper.U64OrZero(ref span) : ReadHelper.U32OrZero(ref span)
            );
            return new PELoadConfigDir(header1);
        }
    }
}
