using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public record RunLIBHelper(WinCmdHelper WinCmdHelper, LibExe Lib)
    {
        public int RunLIB(string[] runArgs)
        {
            var psi = new ProcessStartInfo(
                Lib.Lib,
                string.Join(" ", runArgs.Select(WinCmdHelper.EscapeArg))
            )
            {
                UseShellExecute = false,
            };
            var p = Process.Start(psi) ?? throw new Exception("Failed to start process");
            p.WaitForExit();
            return p.ExitCode;
        }

    }
}
