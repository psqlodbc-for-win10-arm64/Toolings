using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public record CLArg(
        string Value,
        bool Link = false,
        bool CompileOnly = false,
        bool Arm64EC = false,
        string Fo = "",
        string Pdb = "",
        bool ShowIncludes = false
    )
    {

    }

}
