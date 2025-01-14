using LibAmong3.Helpers;

namespace Arm64XSingleLibLIB
{
    internal class Program
    {
        static int Main(string[] args)
        {
            return new Arm64XLIBHelper(
                libParser: new ParseLibArgHelper(),
                parseArgs: new ParseWinArgsHelper(),
                libExe: new RunLIBHelper(
                    new WinCmdHelper(), 
                    @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\lib.exe"
                ),
                newTempFileHelper: TempFileHelperProvider.GetDefault(),
                normHelper: new NormHelper()
            )
                .RunLIB(args: args, dualObj: false);
        }
    }
}
