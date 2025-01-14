using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class NormHelper
    {
        public string Norm(string path)
            => path
                .Replace("\\", "_")
                .Replace("/", "_")
                .Replace("\"", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("|", "_")
                .Replace("*", "_")
                .Replace("?", "_")
            ;
    }
}
