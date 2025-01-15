using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class CLCmdHelper
    {
        private static readonly char[] _argSep = new char[] { ' ', '"', };

        /// <summary>
        /// Escape an argument suitable for passing to CL.exe.
        /// </summary>
        /// <remarks>
        /// - Accepting such as `"-DENGINESDIR=\"C:\\Program Files\\OpenSSL\\lib\\engines-3\""`
        /// - `\\` is already escaped.
        /// </remarks>
        public string EscapeArg(string arg)
        {
            return (0 <= arg.IndexOfAny(_argSep))
                ? $"\"{arg.Replace("\"", "\\\"")}\""
                : arg;
        }
    }
}
