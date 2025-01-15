using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class ParseClArgHelper
    {
        public CLArg Parse(string arg)
        {
            if (arg.StartsWith("-") || arg.StartsWith("/"))
            {
                var opt = arg.Substring(1);

                if (false) { }
                else if (string.Compare(opt, "link", true) == 0)
                {
                    return new CLArg(arg, Link: true);
                }
                else if (string.Compare(opt, "c", true) == 0)
                {
                    return new CLArg(arg, CompileOnly: true);
                }
                else if (string.Compare(opt, "arm64EC", true) == 0)
                {
                    return new CLArg(arg, Arm64EC: true);
                }
                else if (opt.StartsWith("fo:", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new CLArg(arg, Fo: opt.Substring(3));
                }
                else if (opt.StartsWith("fo", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new CLArg(arg, Fo: opt.Substring(2));
                }
                else if (opt.StartsWith("fd:", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new CLArg(arg, Fd: opt.Substring(3));
                }
                else if (opt.StartsWith("fe:", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new CLArg(arg, Fe: opt.Substring(3));
                }
                else if (opt.StartsWith("fd", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new CLArg(arg, Fd: opt.Substring(2));
                }
                else if (opt.StartsWith("fe", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new CLArg(arg, Fe: opt.Substring(2));
                }
                else if (string.Compare(opt, "showIncludes", true) == 0)
                {
                    return new CLArg(arg, ShowIncludes: true);
                }
                else if (string.Compare(opt, "LD", true) == 0)
                {
                    return new CLArg(arg, LD: true);
                }
                else if (string.Compare(opt, "LDd", true) == 0)
                {
                    return new CLArg(arg, LDd: true);
                }
                else if (string.Compare(opt, "E", true) == 0)
                {
                    return new CLArg(arg, E: true);
                }
                else if (string.Compare(opt, "EP", true) == 0)
                {
                    return new CLArg(arg, EP: true);
                }
                else if (string.Compare(opt, "P", true) == 0)
                {
                    return new CLArg(arg, P: true);
                }
                else if (string.Compare(opt, "Zs", true) == 0)
                {
                    return new CLArg(arg, Zs: true);
                }
                else
                {
                    return new CLArg(arg);
                }
            }
            else
            {
                return new CLArg(arg, FileInput: true);
            }
        }
    }
}
