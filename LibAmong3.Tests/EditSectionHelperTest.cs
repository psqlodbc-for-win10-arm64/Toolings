using LibAmong3.Helpers.PE32;
using LibAmong3.Helpers.PE32.Deeper;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Tests
{
    public class EditSectionHelperTest
    {
        [Test]
        [TestCase("arm32.dll")]
        [TestCase("arm64.dll")]
        [TestCase("arm64ec.dll")]
        [TestCase("arm64x.dll")]
        [TestCase("x64.dll")]
        [TestCase("x86.dll")]
        public void Glow(string dllName)
        {
            var pe = File.ReadAllBytes($@"Files\{dllName}").AsMemory();

            var parseHeader = new ParseHeader();

            var headerBefore = parseHeader.Parse(pe);

            var sectionBinBefore = headerBefore.Sections
                .Select(it => pe.Slice(it.PointerToRawData, it.SizeOfRawData).ToArray())
                .ToArray();

            var editSectionHelper = new EditSectionHelper();

            var bytesAppended = new byte[128];
            for (int x = 0; x < 128; x++)
            {
                bytesAppended[x] = (byte)x;
            }

            for (int y = 0; y < headerBefore.Sections.Count; y++)
            {
                Console.WriteLine("{0} \"{1}\"", y, headerBefore.Sections[y].Name);

                var pair = editSectionHelper.TryToGlowSection(pe, y, bytesAppended.Length);
                if (pair.HasValue)
                {
                    var peMod = pair.Value.PeMod;
                    bytesAppended.CopyTo(peMod.Slice(pair.Value.PointerToWrite));

                    var headerAfter = parseHeader.Parse(peMod);

                    var sectionBinAfter = headerAfter.Sections
                        .Select(it => peMod.Slice(it.PointerToRawData, it.SizeOfRawData).ToArray())
                        .ToArray();

                    Assert.That(headerAfter.Sections.Count, Is.EqualTo(headerBefore.Sections.Count));

                    for (int t = 0; t < headerBefore.Sections.Count; t++)
                    {
                        Console.WriteLine("{0} {1}", y, t);
                        if (y == t)
                        {
                            Assert.That(sectionBinAfter[t].AsMemory(0, sectionBinBefore[t].Length).ToArray(), Is.EqualTo(sectionBinBefore[t]));
                            Assert.That(sectionBinAfter[t].AsMemory(sectionBinBefore[t].Length).ToArray(), Is.EqualTo(bytesAppended));
                        }
                        else
                        {
                            Assert.That(sectionBinAfter[t], Is.EqualTo(sectionBinBefore[t]));
                        }
                    }
                }
                else
                {
                    pair = editSectionHelper.AddNewSection(pe, ".sect1", bytesAppended.Length);
                    var peMod = pair.Value.PeMod;
                    bytesAppended.CopyTo(peMod.Slice(pair.Value.PointerToWrite));

                    var headerAfter = parseHeader.Parse(pair.Value.PeMod);

                    var sectionBinAfter = headerAfter.Sections
                        .Select(it => peMod.Slice(it.PointerToRawData, it.SizeOfRawData).ToArray())
                        .ToArray();

                    Assert.That(headerAfter.Sections.Count, Is.EqualTo(headerBefore.Sections.Count + 1));

                    for (int t = 0; t < headerBefore.Sections.Count; t++)
                    {
                        if (y == t)
                        {
                            Assert.That(sectionBinAfter[t], Is.EqualTo(sectionBinBefore[t]));
                        }
                        else
                        {
                            Assert.That(sectionBinAfter[t], Is.EqualTo(sectionBinBefore[t]));
                        }
                    }

                    Assert.That(sectionBinAfter[sectionBinAfter.Length - 1], Is.EqualTo(bytesAppended));
                }
            }
        }
    }
}
