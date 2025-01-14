using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class TempFileHelperProvider
    {
        public static Func<TempFileHelper> GetDefault()
        {
            return () => new TempFileHelper();
        }
    }
}
