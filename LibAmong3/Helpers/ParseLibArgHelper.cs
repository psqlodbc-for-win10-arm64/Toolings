using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class ParseLibArgHelper
    {
        public LibArg Parse(string arg)
        {
            if (arg.StartsWith("-") || arg.StartsWith("/"))
            {
                var opt = arg.Substring(1);
                if (false) { }
                else if (opt.StartsWith("machine:", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new LibArg(arg, Machine: opt.Substring(8));
                }
                else
                {
                    return new LibArg(arg);
                }
            }
            else if (arg.StartsWith("@"))
            {
                return new LibArg(arg, At: arg.Substring(1));
            }
            else
            {
                return new LibArg(arg, IsObj: true);
            }
        }
    }
}
