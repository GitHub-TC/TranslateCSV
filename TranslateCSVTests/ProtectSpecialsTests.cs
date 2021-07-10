using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TranslateCSV.Tests
{
    [TestClass]
    public class ProtectSpecialsTests
    {
        [TestMethod]
        public void ProtectTestNone()
        {
            var p = new ProtectSpecials()
            {
                ProtectWords = new[] { new Regex(@"(?'protect'\[(\S+?)\])") },
                Glossar      = new Dictionary<Regex, string> { { new Regex(@"(?'replace'report)[^\S]"), "report"} }
            };

            var text = "[c][FFFF00]Want to report a game bug?[-][/c]\\nPlease reproduce it in a unmodded, default game before reporting it to the Empyrion developers.";

            Assert.AreEqual(text, p.Restore(p.Protect(text)));
        }

        [TestMethod]
        public void TestProtectWordsFile()
        {
            var p = new ProtectSpecials()
            {
                ProtectWords = File.ReadAllLines(@"..\..\..\..\TranslateCSV\ProtectWords.txt").Select(W => new Regex(W)).ToArray(),
                Glossar = new Dictionary<Regex, string>()
            };

            var text = @"\n<color=#00ffff>Emergency situation detected! </color>\n\n@w4 Protocol UCH001A has been initialized!";

            var protect = p.Protect(text);
            Assert.AreEqual(text, p.Restore(protect));

            Assert.AreEqual("<x i=\"7\"/>Emergency situation detected! <x i=\"6\"/> Protocol UCH001A has been initialized!", protect);
        }
    }
}