using CoffReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.CLTargetHelpers
{
    public class DecideCLTargetTypeHelper
    {
        public CLTargetType? Decide(string input)
        {
            if (File.Exists(input))
            {
                using (var fileStream = File.OpenRead(input))
                {
                    var buf = new byte[8];
                    int numRead = fileStream.Read(buf, 0, 8);
                    if (false) { }
                    else if (2 <= numRead && buf[0] == 0x4D && buf[1] == 0x5A)
                    {
                        // EXE DLL
                        return CLTargetType.Other;
                    }
                    else if (2 <= numRead && buf[0] == 0x64 && buf[1] == 0xAA)
                    {
                        // Maybe ARM64 COFF
                        return CLTargetType.Other;
                    }
                    else if (2 <= numRead && buf[0] == 0x4C && buf[1] == 0x01)
                    {
                        // Maybe x86 COFF
                        var obj = CoffParser.Parse(File.ReadAllBytes(input), true);
                        if (true
                            && obj.Sections.Count == 2
                            && obj.Sections[0].Name == "AA64.obj"
                            && obj.Sections[1].Name == "A641.obj"
                        )
                        {
                            return CLTargetType.Arm64XCoffUponX86Coff;
                        }
                        else
                        {
                            return CLTargetType.Other;
                        }
                    }
                    else if (8 <= numRead
                        && buf[0] == 0x21
                        && buf[1] == 0x3C
                        && buf[2] == 0x61
                        && buf[3] == 0x72
                        && buf[4] == 0x63
                        && buf[5] == 0x68
                        && buf[6] == 0x3E
                        && buf[7] == 0x0A
                    )
                    {
                        // LIB
                        return CLTargetType.Other;
                    }
                    else
                    {
                        return CLTargetType.SourceFile;
                    }
                }
            }
            else
            {
                return null;
            }
        }
    }
}
