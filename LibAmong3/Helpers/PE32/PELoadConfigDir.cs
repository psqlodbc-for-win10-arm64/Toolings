using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public record PELoadConfigDir(
        PELoadConfigDir.HeaderType1 Header1)
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// For known trunction patterns:
        /// 
        /// - 0x014C,72
        /// - 0x014C,92
        /// - 0x014C,104
        /// - 0x014C,124
        /// - 0x014C,128
        /// - 0x014C,152
        /// - 0x014C,160
        /// - 0x014C,164
        /// - 0x014C,172
        /// - 0x014C,184
        /// - 0x014C,188
        /// - 0x014C,192
        /// - 0x014C,196
        /// - 0x8664,112
        /// - 0x8664,148
        /// - 0x8664,160
        /// - 0x8664,200
        /// - 0x8664,208
        /// - 0x8664,244
        /// - 0x8664,248
        /// - 0x8664,256
        /// - 0x8664,264
        /// - 0x8664,280
        /// - 0x8664,304
        /// - 0x8664,312
        /// - 0x8664,320
        /// - 0x8664,328
        /// - 0xAA64,148
        /// - 0xAA64,160
        /// - 0xAA64,208
        /// - 0xAA64,244
        /// - 0xAA64,248
        /// - 0xAA64,256
        /// - 0xAA64,264
        /// - 0xAA64,280
        /// - 0xAA64,304
        /// - 0xAA64,312
        /// - 0xAA64,320
        /// - 0xAA64,328
        /// 
        /// Member                                    | x86 | x64
        /// ------------------------------------------|-----|-----
        /// Size                                      |   4 |   4
        /// TimeDateStamp                             |   8 |   8
        /// MajorVersion                              |  10 |  10
        /// MinorVersion                              |  12 |  12
        /// GlobalFlagsClear                          |  16 |  16
        /// GlobalFlagsSet                            |  20 |  20
        /// CriticalSectionDefaultTimeout             |  24 |  24
        /// DeCommitFreeBlockThreshold                |  28 |  32
        /// DeCommitTotalFreeThreshold                |  32 |  40
        /// LockPrefixTable                           |  36 |  48
        /// MaximumAllocationSize                     |  40 |  56
        /// VirtualMemoryThreshold                    |  44 |  64
        /// ProcessAffinityMask                       |  48 |  72
        /// ProcessHeapFlags                          |  52 |  76
        /// CSDVersion                                |  54 |  78
        /// DependentLoadFlags                        |  56 |  80
        /// EditList                                  |  60 |  88
        /// SecurityCookie                            |  64 |  96
        /// SEHandlerTable                            |  68 | 104
        /// SEHandlerCount                            |  72 | 112
        /// GuardCFCheckFunctionPointer               |  76 | 120
        /// GuardCFDispatchFunctionPointer            |  80 | 128
        /// GuardCFFunctionTable                      |  84 | 136
        /// GuardCFFunctionCount                      |  88 | 144
        /// GuardFlags                                |  92 | 148
        /// CodeIntegrity                             | 104 | 160
        /// GuardAddressTakenIatEntryTable            | 108 | 168
        /// GuardAddressTakenIatEntryCount            | 112 | 176
        /// GuardLongJumpTargetTable                  | 116 | 184
        /// GuardLongJumpTargetCount                  | 120 | 192
        /// DynamicValueRelocTable                    | 124 | 200
        /// CHPEMetadataPointer                       | 128 | 208
        /// GuardRFFailureRoutine                     | 132 | 216
        /// GuardRFFailureRoutineFunctionPointer      | 136 | 224
        /// DynamicValueRelocTableOffset              | 140 | 228
        /// DynamicValueRelocTableSection             | 142 | 230
        /// Reserved2                                 | 144 | 232
        /// GuardRFVerifyStackPointerFunctionPointer  | 148 | 240
        /// HotPatchTableOffset                       | 152 | 244
        /// Reserved3                                 | 156 | 248
        /// EnclaveConfigurationPointer               | 160 | 256
        /// VolatileMetadataPointer                   | 164 | 264
        /// GuardEHContinuationTable                  | 168 | 272
        /// GuardEHContinuationCount                  | 172 | 280
        /// GuardXFGCheckFunctionPointer              | 176 | 288
        /// GuardXFGDispatchFunctionPointer           | 180 | 296
        /// GuardXFGTableDispatchFunctionPointer      | 184 | 304
        /// CastGuardOsDeterminedFailureMode          | 188 | 312
        /// GuardMemcpyFunctionPointer                | 192 | 320
        /// UmaFunctionPointers                       | 196 | 328
        /// 
        /// </remarks>
        /// <see cref="https://learn.microsoft.com/en-us/windows/win32/debug/pe-format"/>
        /// <see cref="https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-image_load_config_directory32"/>
        /// <see cref="https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-image_load_config_directory64"/>
        public record HeaderType1(
            uint Size,
            uint TimeDateStamp,
            ushort MajorVersion,
            ushort MinorVersion,
            uint GlobalFlagsClear,
            uint GlobalFlagsSet,
            uint CriticalSectionDefaultTimeout,
            ulong DeCommitFreeBlockThreshold,
            ulong DeCommitTotalFreeThreshold,
            ulong LockPrefixTable,
            ulong MaximumAllocationSize,
            ulong VirtualMemoryThreshold,
            ulong ProcessAffinityMask,
            uint ProcessHeapFlags,
            ushort CSDVersion,
            ushort DependentLoadFlags,
            ulong EditList,
            ulong SecurityCookie,
            ulong SEHandlerTable,
            ulong SEHandlerCount,
            ulong GuardCFCheckFunctionPointer,
            ulong GuardCFDispatchFunctionPointer,
            ulong GuardCFFunctionTable,
            ulong GuardCFFunctionCount,
            uint GuardFlags,
            ReadOnlyMemory<byte> CodeIntegrity,
            ulong GuardAddressTakenIatEntryTable,
            ulong GuardAddressTakenIatEntryCount,
            ulong GuardLongJumpTargetTable,
            ulong GuardLongJumpTargetCount,
            ulong DynamicValueRelocTable,
            ulong CHPEMetadataPointer,
            ulong GuardRFFailureRoutine,
            ulong GuardRFFailureRoutineFunctionPointer,
            uint DynamicValueRelocTableOffset,
            ushort DynamicValueRelocTableSection,
            ushort Reserved2,
            ulong GuardRFVerifyStackPointerFunctionPointer,
            uint HotPatchTableOffset,
            uint Reserved3,
            ulong EnclaveConfigurationPointer,
            ulong VolatileMetadataPointer,
            ulong GuardEHContinuationTable,
            ulong GuardEHContinuationCount,
            ulong GuardXFGCheckFunctionPointer,
            ulong GuardXFGDispatchFunctionPointer,
            ulong GuardXFGTableDispatchFunctionPointer,
            ulong CastGuardOsDeterminedFailureMode,
            ulong GuardMemcpyFunctionPointer,
            ulong UmaFunctionPointers);
    }
}
