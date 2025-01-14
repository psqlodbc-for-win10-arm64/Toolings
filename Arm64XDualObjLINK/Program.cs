using LibAmong3.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Arm64XDualObjLINK
{
    internal class Program
    {
        static int Main(string[] args)
        {
            using (var resolver = new ServiceCollection()
                .AddLibAmong3()
                .Add3Exes()
                .BuildServiceProvider()
            )
            {
                return resolver.GetRequiredService<Arm64XLINKHelper>()
                    .RunLINK(args: args, dualObj: true);
            }
        }
    }
}
