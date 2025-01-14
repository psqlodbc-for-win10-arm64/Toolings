﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public record RunCLHelper(WinCmdHelper WinCmdHelper, string CL)
    {
        public int RunCL(string[] runArgs)
        {
            var psi = new ProcessStartInfo(
                CL,
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
