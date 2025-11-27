using LibAmong3.Helpers.Guessr;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Tests
{
    public class GuessArm64XBinaryHelperTest
    {
        [Test]
        [TestCase("arm32.dll", Arm64XBinaryForm.Arm32)]
        [TestCase("arm64.dll", Arm64XBinaryForm.Arm64)]
        [TestCase("arm64ec.dll", Arm64XBinaryForm.Arm64EC)]
        [TestCase("arm64x.dll", Arm64XBinaryForm.Arm64X)]
        [TestCase("foo.dll", Arm64XBinaryForm.Arm64XPureForwarder)]
        [TestCase("x64.dll", Arm64XBinaryForm.X64)]
        [TestCase("x86.dll", Arm64XBinaryForm.X86)]
        [TestCase("dllmain-arm32.obj", Arm64XBinaryForm.AnonymousCoff)]
        [TestCase("dllmain-arm64.obj", Arm64XBinaryForm.Arm64Coff)]
        [TestCase("dllmain-arm64ec.obj", Arm64XBinaryForm.Arm64ECCoff)]
        [TestCase("dllmain-x64.obj", Arm64XBinaryForm.X64Coff)]
        [TestCase("dllmain-x86.obj", Arm64XBinaryForm.X86Coff)]
        [TestCase("dllmain-arm64x.obj", Arm64XBinaryForm.Arm64XCoffUponX86Coff)]
        public void Guess(string dllName, Arm64XBinaryForm form)
        {
            var bytes = File.ReadAllBytes($@"Files\{dllName}");
            var guessr = new GuessArm64XBinaryHelper();
            Assert.That(guessr.Guess(bytes), Is.EqualTo(form));
        }

        [Test]
        [TestCase("arm32.dll", Arm64XBinaryForm.Arm32)]
        [TestCase("arm64.dll", Arm64XBinaryForm.Arm64)]
        [TestCase("arm64ec.dll", Arm64XBinaryForm.Arm64EC)]
        [TestCase("arm64x.dll", Arm64XBinaryForm.Arm64X)]
        [TestCase("foo.dll", Arm64XBinaryForm.Arm64X)] // cannot figure out Arm64XPureForwarder
        [TestCase("x64.dll", Arm64XBinaryForm.X64)]
        [TestCase("x86.dll", Arm64XBinaryForm.X86)]
        [TestCase("dllmain-arm32.obj", Arm64XBinaryForm.AnonymousCoff)]
        [TestCase("dllmain-arm64.obj", Arm64XBinaryForm.Arm64Coff)]
        [TestCase("dllmain-arm64ec.obj", Arm64XBinaryForm.Arm64ECCoff)]
        [TestCase("dllmain-x64.obj", Arm64XBinaryForm.X64Coff)]
        [TestCase("dllmain-x86.obj", Arm64XBinaryForm.X86Coff)]
        [TestCase("dllmain-arm64x.obj", Arm64XBinaryForm.Arm64XCoffUponX86Coff)]
        public void GuessByCHPE(string dllName, Arm64XBinaryForm form)
        {
            var bytes = File.ReadAllBytes($@"Files\{dllName}");
            var guessr = new GuessArm64XBinaryHelper();
            Assert.That(guessr.Guess(bytes, new Guess1Options { SeeCHPEMetadataPointerForArm64X = true, }), Is.EqualTo(form));
        }

        [Test]
        [TestCase("arm32.dll", Arm64XBinaryForm.Arm32)]
        [TestCase("arm64.dll", Arm64XBinaryForm.Arm64)]
        [TestCase("arm64ec.dll", Arm64XBinaryForm.X64)]
        [TestCase("arm64x.dll", Arm64XBinaryForm.Arm64)]
        [TestCase("foo.dll", Arm64XBinaryForm.Arm64)]
        [TestCase("x64.dll", Arm64XBinaryForm.X64)]
        [TestCase("x86.dll", Arm64XBinaryForm.X86)]
        [TestCase("dllmain-arm32.obj", Arm64XBinaryForm.AnonymousCoff)]
        [TestCase("dllmain-arm64.obj", Arm64XBinaryForm.Arm64Coff)]
        [TestCase("dllmain-arm64ec.obj", Arm64XBinaryForm.Arm64ECCoff)]
        [TestCase("dllmain-x64.obj", Arm64XBinaryForm.X64Coff)]
        [TestCase("dllmain-x86.obj", Arm64XBinaryForm.X86Coff)]
        [TestCase("dllmain-arm64x.obj", Arm64XBinaryForm.X86Coff)]
        public void GuessNative(string dllName, Arm64XBinaryForm nativeForm)
        {
            var bytes = File.ReadAllBytes($@"Files\{dllName}");
            var guessr = new GuessArm64XBinaryHelper();
            Assert.That(guessr.Guess(bytes, new Guess1Options { DisableArm64XDetection = true, }), Is.EqualTo(nativeForm));
        }

        [Test]
        [TestCase("dllmain-arm32.obj", 6, Arm64XBinaryForm.Arm32Coff)]
        public void Guess_2(string dllName, int skipLeadingBytes, Arm64XBinaryForm form)
        {
            var bytes = File.ReadAllBytes($@"Files\{dllName}");
            var guessr = new GuessArm64XBinaryHelper();
            Assert.That(guessr.Guess(bytes.AsMemory(skipLeadingBytes)), Is.EqualTo(form));
        }
    }
}
