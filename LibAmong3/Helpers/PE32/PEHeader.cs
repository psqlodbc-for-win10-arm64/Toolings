using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public record PEHeader(
        ushort Machine,
        IReadOnlyList<PESection> Sections,
        bool IsPE32Plus,
        IReadOnlyList<PEImageDataDirectory> ImageDataDirectories)
    {
    }
}
