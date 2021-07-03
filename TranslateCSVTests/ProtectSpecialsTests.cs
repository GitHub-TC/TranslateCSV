using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
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
    }
}