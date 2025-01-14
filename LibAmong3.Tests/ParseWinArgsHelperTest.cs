using LibAmong3.Helpers;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Tests
{
    public class ParseWinArgsHelperTest
    {
        [Test]
        [TestCase("123", new string[] { "123", })]
        [TestCase(" 123", new string[] { "123", })]
        [TestCase(" 123 ", new string[] { "123", })]
        [TestCase("123 ", new string[] { "123", })]
        [TestCase("1 2 3", new string[] { "1", "2", "3", })]
        [TestCase("/I\"A B C\"", new string[] { "/IA B C", })]
        [TestCase("/C\"echo hello user\"", new string[] { "/Cecho hello user", })]
        [TestCase("/C\"echo \"\"hello user\"\"\"", new string[] { "/Cecho \"hello user\"", })]
        [TestCase("/C\"echo \"\"hello\"\" user\"", new string[] { "/Cecho \"hello\" user", })]
        [TestCase("/C\"echo \"\"hello\"\"\" user", new string[] { "/Cecho \"hello\"", "user", })]
        [TestCase("\"\"", new string[] { "", })]
        [TestCase("\"\"\"\"", new string[] { "\"", })]
        [TestCase("\"\"\"\"\"\"", new string[] { "\"\"", })]
        public void TestArgs(string args, string[] argv)
        {
            var helper = new ParseWinArgsHelper();
            var actual = helper.ParseArgs(args);
            CollectionAssert.AreEqual(argv, actual);
        }
    }
}
