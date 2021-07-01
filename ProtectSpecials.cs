using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TranslateCSV
{
    public class ProtectSpecials
    {
        public Regex[] ProtectWords { get; set; } = new Regex[] { };
        public Dictionary<Regex, string> Glossar { get; set; } = new Dictionary<Regex, string>();
        Dictionary<int, string> ProtecedData { get; set; } = new Dictionary<int, string>();

        public string Protect(string source)
        {
            var protectedString = source;

            ProtecedData.Clear();
            int ProtectedIndex = 0;

            for (int i = 0; i < ProtectWords.Length; i++)
            {
                var matches = ProtectWords[i].Matches(protectedString);
                for (int mi = matches.Count - 1; mi >= 0; mi--)
                {
                    var currentMatch = matches[mi];
                    if (currentMatch.Success)
                    {
                        var protectGroup = currentMatch.Groups["protect"];
                        protectedString = protectedString.Substring(0, protectGroup.Index) + $"<x i=\"{ProtectedIndex}\"/>" + protectedString.Substring(protectGroup.Index + protectGroup.Length);
                        ProtecedData.Add(ProtectedIndex++, protectGroup.Value);
                    }
                }
            }

            foreach (var item in Glossar)
            {
                var matches = item.Key.Matches(protectedString);
                for (int mi = matches.Count - 1; mi >= 0; mi--)
                {
                    var currentMatch = matches[mi];
                    if (currentMatch.Success)
                    {
                        var replaceGroup = currentMatch.Groups["replace"];
                        protectedString = protectedString.Substring(0, replaceGroup.Index) + $"<x i=\"{ProtectedIndex}\"/>" + protectedString.Substring(replaceGroup.Index + replaceGroup.Length);
                        ProtecedData.Add(ProtectedIndex++, item.Value);
                    }
                }
            }

            return protectedString;
        }

        public string Restore(string source)
        {
            var restoredString = source;

            foreach (var item in ProtecedData)
            {
                restoredString = restoredString.Replace($"<x i=\"{item.Key}\"/>", item.Value);
            }

            return restoredString;
        }
    }
}
