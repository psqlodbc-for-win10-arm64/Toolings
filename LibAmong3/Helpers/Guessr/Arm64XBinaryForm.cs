using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.Guessr
{
    public enum Arm64XBinaryForm
    {
        None,
        Unknown,
        X64,
        Arm64,
        Arm64EC,
        Arm64X,
        X86,
        Arm64Coff,
        Arm64ECCoff,
        X64Coff,
        X86Coff,
        Arm64XCoffUponX86Coff,
        Arm32,
        Arm32Coff,
        AnonymousCoff,
        Arm64XPureForwarder,
    }
}
