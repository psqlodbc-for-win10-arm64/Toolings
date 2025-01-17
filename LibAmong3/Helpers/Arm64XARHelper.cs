using CoffReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class Arm64XARHelper
    {
        private readonly NormHelper _normHelper;
        private readonly Func<TempFileHelper> _newTempFileHelper;
        private readonly RunLIBHelper _runLib;

        public Arm64XARHelper(
            Func<TempFileHelper> newTempFileHelper,
            NormHelper normHelper,
            RunLIBHelper runLib)
        {
            _normHelper = normHelper;
            _newTempFileHelper = newTempFileHelper;
            _runLib = runLib;
        }

        public int RunAR(string[] args, bool dualObj)
        {
            if (args.Length == 1 && args[0] == "--version")
            {
                return _runLib.RunLIB(
                    new string[] { "/?" }
                );
            }

            if (args.Length < 2)
            {
                Console.Error.WriteLine("Need at least 2 parameters");
                return 1;
            }
            if (args[0] != "csr")
            {
                Console.Error.WriteLine("First parameter must be csr");
                return 1;
            }

            using var tempFileHelper = _newTempFileHelper();

            var libArgs = new List<string>();

            foreach (var arg in args.Skip(2))
            {
                if (false) { }
                else if (dualObj && arg.EndsWith(".obj", StringComparison.InvariantCultureIgnoreCase))
                {
                    var file = File.ReadAllBytes(arg);
                    var coff = CoffParser.Parse(file, true);
                    if (coff.Magic == 0x14c)
                    {
                        var prefix = arg;
                        {
                            var arm64Obj = tempFileHelper.GetTempFile(_normHelper.Norm(prefix + ".ARM64.obj"));

                            File.WriteAllBytes(
                                arm64Obj,
                                CoffParser.ReadRawData(
                                    file,
                                    coff.Sections
                                        .Single(it => it.Name == "AA64.obj")
                                )
                                    .ToArray()
                            );

                            libArgs.Add(arm64Obj);
                        }

                        {
                            var arm64ECObj = tempFileHelper.GetTempFile(_normHelper.Norm(prefix + ".ARM64EC.obj"));

                            File.WriteAllBytes(
                                arm64ECObj,
                                CoffParser.ReadRawData(
                                    file,
                                    coff.Sections
                                        .Single(it => it.Name == "A641.obj")
                                )
                                    .ToArray()
                            );

                            libArgs.Add(arm64ECObj);
                        }
                    }
                    else
                    {
                        libArgs.Add(arg);
                    }
                }
                else
                {
                    libArgs.Add(arg);
                }
            }

            var exitCode = _runLib.RunLIB(
                new string[] { "/machine:arm64x", $"/out:{args[1]}" }
                .Concat(libArgs.ToArray())
                .ToArray()
            );

            return exitCode;
        }
    }
}
