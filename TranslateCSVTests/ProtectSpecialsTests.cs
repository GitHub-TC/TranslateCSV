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

        [TestMethod]
        public void TestProtectWordsFile1()
        {
            var p = new ProtectSpecials()
            {
                ProtectWords = File.ReadAllLines(@"..\..\..\..\TranslateCSV\ProtectWords.txt").Select(W => new Regex(W)).ToArray(),
                Glossar = new Dictionary<Regex, string>()
            };

            var text = @"<b><color=#ffae00>[ Officer Paravel ]</color></b>\nHere's your payment, as promised.@w3 Most contract jobs will be paid directly to your account with Polaris, once you've been authenticated.@w4\nI recommend depositing your payment at the machine behind you when we're done here.\n<b><color=#a6ff00>< Payment Received: 15,000 Credits >@p9 </color></b>\n<b><color=#0088ff>[ {PlayerName} ]</color></b>\nWhat's with the obsession with money around here?@p8\n<b><color=#ffae00>[ Officer Paravel ]</color></b>\nTo Polaris and the Trade Guild, there's nothing more important, more sacred than money.@w4\nIt's a measure of one's value and one's achievements in life.@w4 A physical means of measuring the worth of a man can be seen in his wealth.@w6\nI suppose you could think of money to us as counters in the great game of life, and we're <i>all </i>players. Whether we like it or not.@p9\n<b><color=#ffae00>[ Officer Paravel ]</color></b>\nA man with no wealth is either a failure, or a threat to those that do.@w3 A man who does not seek to gain wealth is a danger to everyone that does.@w4\nSo keep in mind {PlayerName}, 8376455466.@w2 That if you do not seek compensation at Polaris, you will be considered untrustworthy.@w6 That shouldn't be anything new to <i>you</i>, though.";

            var protect = p.Protect(text);
            Assert.AreEqual(text, p.Restore(protect));

            Assert.AreEqual("<x i=\"7\"/>Emergency situation detected! <x i=\"6\"/> Protocol UCH001A has been initialized!", protect);
        }

        [TestMethod]
        public void TestProtectWordsFile2()
        {
            var p = new ProtectSpecials()
            {
                ProtectWords = File.ReadAllLines(@"..\..\..\..\TranslateCSV\ProtectWords.txt").Select(W => new Regex(W)).ToArray(),
                Glossar = new Dictionary<Regex, string>()
            };

            var text = @"<color=#019245><b>You won! </b></color>Here take your <color=#fddc1e>{2*GoldCoinsPlayerBet} Gold Coins</color>!";

            var protect = p.Protect(text);
            Assert.AreEqual(text, p.Restore(protect));

            Assert.AreEqual("<x i=\"9\"/>You won! <x i=\"8\"/>Here take your <x i=\"7\"/> Gold Coins<x i=\"0\"/>!", protect);
        }

    }
}