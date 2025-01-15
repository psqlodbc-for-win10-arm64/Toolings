using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.CLTargetHelpers
{
    public enum CLTargetType
    {
        None,
        SourceFile,
        Arm64XCoffUponX86Coff,
        Arm64Coff,
        Arm64ECCoff,
        Other,
    }
}
