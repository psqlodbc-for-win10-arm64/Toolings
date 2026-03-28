using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32.Deeper
{
    public class ApplyDvrtHelper
    {
        /// <returns>Return false if `Error: loadConfigDirEntry not found!`</returns>
        public bool ApplyDvrt(Memory<byte> dll)
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

                foreach (var one in patchableVASpanProvider.PatchRecords)
                {
                    var pair = provider.Locate(one.Rva, one.Bytes.Length);

                    one.Bytes.CopyTo(dll.Slice(pair.Start, pair.Length));
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
