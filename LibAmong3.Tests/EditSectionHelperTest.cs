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
        public void Grow(string dllName)
        {
            var pe = File.ReadAllBytes($@"Files\{dllName}").AsMemory();

            var parseHeader = new ParseHeader();

            var headerBefore = parseHeader.Parse(pe);

            var sectionBinBefore = headerBefore.Sections
                .Select(
                    it => (
                        Bin: pe.Slice(it.PointerToRawData, Math.Min(it.VirtualSize, it.SizeOfRawData)).ToArray(),
                        AppendedAt: AlignBy(it.VirtualSize, 4)
                    )
                )
                .ToArray();

            var editSectionHelper = new EditSectionHelper();

            var bytesAppended = new byte[128];
            for (int x = 0; x < 128; x++)
            {
                bytesAppended[x] = (byte)x;
            }

            for (int y = 0; y < headerBefore.Sections.Count; y++)
            {
                Console.WriteLine("Append to section #{0} \"{1}\"", y, headerBefore.Sections[y].Name);

                var pair = editSectionHelper.TryToGlowSection(pe, y, bytesAppended.Length);
                if (pair.HasValue)
                {
                    Console.WriteLine("  Grow mode");
                    var peMod = pair.Value.PeMod;
                    bytesAppended.CopyTo(peMod.Slice(pair.Value.PointerToWrite));

                    var headerAfter = parseHeader.Parse(peMod);

                    var sectionBinAfter = headerAfter.Sections
                        .Select(it => peMod.Slice(it.PointerToRawData, Math.Min(it.VirtualSize, it.SizeOfRawData)).ToArray())
                        .ToArray();

                    Assert.That(headerAfter.Sections.Count, Is.EqualTo(headerBefore.Sections.Count));

                    for (int t = 0; t < headerBefore.Sections.Count; t++)
                    {
                        Console.WriteLine("  Compare {0} ; {1,5} {2,5}  {3,5}", t, sectionBinBefore[t].Bin.Length, sectionBinBefore[t].AppendedAt, sectionBinAfter[t].Length);
                        if (y == t)
                        {
                            Assert.That(sectionBinAfter[t].AsMemory(0, sectionBinBefore[t].Bin.Length).ToArray(), Is.EqualTo(sectionBinBefore[t].Bin));
                            Assert.That(sectionBinAfter[t].AsMemory(sectionBinBefore[t].AppendedAt).ToArray(), Is.EqualTo(bytesAppended));
                        }
                        else
                        {
                            Assert.That(sectionBinAfter[t], Is.EqualTo(sectionBinBefore[t].Bin));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("  New section mode");

                    pair = editSectionHelper.AddNewSection(pe, ".sect1", bytesAppended.Length);
                    var peMod = pair.Value.PeMod;
                    bytesAppended.CopyTo(peMod.Slice(pair.Value.PointerToWrite));

                    var headerAfter = parseHeader.Parse(pair.Value.PeMod);

                    var sectionBinAfter = headerAfter.Sections
                        .Select(it => peMod.Slice(it.PointerToRawData, it.VirtualSize).ToArray())
                        .ToArray();

                    Assert.That(headerAfter.Sections.Count, Is.EqualTo(headerBefore.Sections.Count + 1));

                    int t = 0;

                    for (; t < headerBefore.Sections.Count; t++)
                    {
                        Console.WriteLine("  Compare {0} ; {1,5} {2,5}  {3,5}", t, sectionBinBefore[t].Bin.Length, "", sectionBinAfter[t].Length);

                        Assert.That(sectionBinAfter[t], Is.EqualTo(sectionBinBefore[t].Bin));
                    }

                    {
                        Console.WriteLine("  Compare {0} ; {1,5} {2,5}  {3,5}", t, bytesAppended.Length, "", sectionBinAfter[t].Length);

                        Assert.That(sectionBinAfter[t], Is.EqualTo(bytesAppended));
                    }
                }
            }
        }

        private static int AlignBy(int value, int by)
        {
            var mask = by - 1;
            return (value + mask) & (~mask);
        }
    }
}
