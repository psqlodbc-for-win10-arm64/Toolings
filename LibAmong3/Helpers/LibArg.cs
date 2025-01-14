using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public record LibArg(
        string Value,
        string Machine = "",
        string At = "",
        bool IsObj = false
    )
    {
    }
}
