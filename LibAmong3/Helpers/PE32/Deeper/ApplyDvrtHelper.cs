using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32.Deeper
{
    public class ApplyDvrtHelper
    {
        public record DvrtApplier(
            bool HasLoadConfig = false,
            int NumPatchedRecords = 0,
            Func<ApplyPatchResult>? ApplyPatches = null
            );

        public record ApplyPatchResult();

        public DvrtApplier CreateDvrtApplier(Memory<byte> dll)
        {
            var lookAtLoadConfig = LookAtLoadConfig1.Create(dll);
            if (lookAtLoadConfig != null)
            {
                var parseHeader = new ParseHeader();
                var header = parseHeader.Parse(dll);
                var provider = new VAReadOnlySpanProvider(
                    dll,
                    header.Sections
                );
                var patchableVASpanProvider = new PatchableVASpanProvider(provider.Provide);

                lookAtLoadConfig.ApplyDvrt(patchableVASpanProvider);

                if (patchableVASpanProvider.PatchRecords.Any())
                {
                    return new DvrtApplier(
                        true,
                        patchableVASpanProvider.PatchRecords.Count,
                        () =>
                        {
                            foreach (var one in patchableVASpanProvider.PatchRecords)
                            {
                                var pair = provider.Locate(one.Rva, one.Bytes.Length);

                                one.Bytes.CopyTo(dll.Slice(pair.Start, pair.Length));
                            }

                            return new ApplyPatchResult();
                        }
                    );
                }
                else
                {
                    return new DvrtApplier(true);
                }
            }
            else
            {
                return new DvrtApplier();
            }
        }

        /// <returns>Return false if `Error: loadConfigDirEntry not found!`</returns>
        public bool ApplyDvrt(Memory<byte> dll)
        {
            var applier = CreateDvrtApplier(dll);
            if (applier.HasLoadConfig)
            {
                applier.ApplyPatches?.Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
