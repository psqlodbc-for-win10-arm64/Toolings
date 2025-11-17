using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Tests
{
    public class ListGenerator
    {
        [Test]
        [Ignore("Private use")]
        public void GenerateListOfMicrosoftWindowsOperatingSystemDLLs()
        {
            bool Test(string dllName)
            {
                try
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(dllName);
                    var productName = versionInfo.ProductName ?? "";
                    return true
                        && productName.Contains("Microsoft")
                        && productName.Contains("Windows")
                        && productName.Contains("Operating")
                        && productName.Contains("System")
                        ;
                }
                catch
                {
                    return false;
                }
            }

            foreach (var file in Directory.GetFiles(Environment.SystemDirectory, "*.dll")
                .Where(it => Test(it))
            )
            {
                Console.WriteLine(Path.GetFileName(file));
            }
        }
    }
}
