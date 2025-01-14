using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class ParseLinkArgHelper
    {
        public LinkArg Parse(string arg)
        {
            if (arg.StartsWith("-") || arg.StartsWith("/"))
            {
                var opt = arg.Substring(1);

                if (false) { }
                else if (opt.StartsWith("out:", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new LinkArg(arg, OutputTo: opt.Substring(4));
                }
                else if (opt.StartsWith("machine:", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new LinkArg(arg, Machine: opt.Substring(8));
                }
                else if (opt.StartsWith("def:", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new LinkArg(arg, Def: opt.Substring(4));
                }
                else if (opt.StartsWith("defArm64Native:", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new LinkArg(arg, DefArm64Native: opt.Substring(15));
                }
                else
                {
                    return new LinkArg(arg);
                }
            }
            else if (arg.StartsWith("@"))
            {
                return new LinkArg(arg, At: arg.Substring(1));
            }
            else
            {
                return new LinkArg(arg, IsObj: true);
            }
        }
    }
}
