using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    /// <param name="Fo">obj output</param>
    /// <param name="Fe">exe output</param>
    /// <param name="Fd">pdb output</param>
    /// <param name="E">pre process to stdout</param>
    /// <param name="EP">pre process to stdout w/o #line</param>
    /// <param name="P">pre process</param>
    public record CLArg(
        string Value,
        bool Link = false,
        bool CompileOnly = false,
        bool Arm64EC = false,
        string Fo = "",
        string Fe = "",
        string Fd = "",
        bool ShowIncludes = false,
        bool FileInput = false,
        bool LD = false,
        bool LDd = false,
        bool E = false,
        bool EP = false,
        bool P = false
    )
    {

    }

}
