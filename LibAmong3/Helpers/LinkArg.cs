using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public record LinkArg(
        string Value,
        string OutputTo = "",
        string Machine = "",
        string At = "",
        string Def = "",
        string DefArm64Native = "",
        bool IsObj = false
    )
    {
    }
}
