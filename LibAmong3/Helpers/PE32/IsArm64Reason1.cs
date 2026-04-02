using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.PE32
{
    public record IsArm64Reason1(
        IsArm64Reason1.Arch1 Arch,
        bool Arm64ECBinary = false
        )
    {
        public enum Arch1
        {
            Unknown = 0,
            X64,
            Arm64,
            Arm64EC,
        }
    }
}
