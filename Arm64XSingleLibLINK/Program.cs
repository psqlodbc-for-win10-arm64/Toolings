using LibAmong3.Helpers;

namespace Arm64XSingleLibLINK
{
    internal class Program
    {
        static int Main(string[] args)
        {
            return new Arm64XLINKHelper(
                linkParser: new ParseLinkArgHelper(),
                linkExe: new RunLINKHelper(
                    new WinCmdHelper(),
                    @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\link.exe"
                ),
                newTempFileHelper: TempFileHelperProvider.GetDefault()
            )
                .RunLINK(args: args, dualObj: false);
        }
    }
}
