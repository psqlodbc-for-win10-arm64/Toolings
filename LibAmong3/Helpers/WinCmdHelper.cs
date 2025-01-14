using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class WinCmdHelper
    {
        private static readonly char[] _argSep = new char[] { ' ', '"', };

        public string EscapeArg(string arg)
        {
            return (0 <= arg.IndexOfAny(_argSep))
                ? $"\"{arg.Replace("\"", "\"\"")}\""
                : arg;
        }
    }
}
