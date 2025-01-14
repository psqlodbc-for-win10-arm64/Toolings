using LibAmong3.Helpers;

namespace Arm64XDualObjCL
{
    internal class Program
    {
        static int Main(string[] args)
        {
            return new Arm64XCLHelper(
                clParser: new ParseClArgHelper(),
                linkParser: new ParseLinkArgHelper(),
                clExe: new RunCLHelper(
                    new WinCmdHelper(), 
                    @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\cl.exe"
                ),
                linkExe: new RunLINKHelper(
                    new WinCmdHelper(), 
                    @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\link.exe"
                ),
                libExe: new RunLIBHelper(
                    new WinCmdHelper(), 
                    @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\lib.exe"
                ),
                normHelper: new NormHelper(),
                newTempFileHelper: TempFileHelperProvider.GetDefault(),
                makeCoffHelper: new MakeCoffHelper()
            )
                .RunCL(args: args, dualObj: true);
        }
    }
}
