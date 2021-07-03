using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TranslateCSV
{
    public class ProtectSpecials
    {
        public Regex[] ProtectWords { get; set; } = new Regex[] { };
        public Dictionary<Regex, string> Glossar { get; set; } = new Dictionary<Regex, string>();
        List<KeyValuePair<int, string>> ProtecedData { get; set; } = new List<KeyValuePair<int, string>>();

        Regex MergeProteced = new Regex(@"(?'protect'(<x\si=""\d+""/>)+)");

        public string Protect(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return string.Empty;

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
                        ProtecedData.Add(new KeyValuePair<int, string>(ProtectedIndex++, protectGroup.Value));
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
                        ProtecedData.Add(new KeyValuePair<int, string>(ProtectedIndex++, item.Value));
                    }
                }
            }

            var matchesMerge = MergeProteced.Matches(protectedString);
            for (int mi = matchesMerge.Count - 1; mi >= 0; mi--)
            {
                var currentMatch = matchesMerge[mi];
                if (currentMatch.Success)
                {
                    var protectGroup = currentMatch.Groups["protect"];
                    if (protectGroup.Value.IndexOf("<x", 1) > 0) // mindestens zwei <x...
                    {
                        protectedString = protectedString.Substring(0, protectGroup.Index) + $"<x i=\"{ProtectedIndex}\"/>" + protectedString.Substring(protectGroup.Index + protectGroup.Length);
                        ProtecedData.Add(new KeyValuePair<int, string>(ProtectedIndex++, protectGroup.Value));
                    }
                }
            }


            return protectedString;
        }

        public string Restore(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return string.Empty;

            var restoredString = source;

            for (int i = ProtecedData.Count - 1; i >= 0; i--) restoredString = restoredString.Replace($"<x i=\"{ProtecedData[i].Key}\"/>", ProtecedData[i].Value);

            return restoredString;
        }
    }
}
