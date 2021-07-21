using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace TranslateCSV
{
    public class TranslationIO
    {
        public static List<List<string>> ReadTranslationFromCsv(string csvFile)
        {
            if (!File.Exists(csvFile)) throw new FileNotFoundException("File not found", csvFile);

            var translations = new List<List<string>>();
            using var reader = new StreamReader(csvFile);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();
            var languages = csv.HeaderRecord.Length;

            do
            {
                var newLine = new List<string>();
                for (int i = 0; i < languages; i++) newLine.Add(csv.TryGetField(typeof(string), i, out var field) ? field?.ToString() : string.Empty);
                translations.Add(newLine);
            }
            while (csv.Read());

            return translations;
        }

        public static void WriteTranslationToCsv(List<List<string>> translations, string csvFile)
        {
            using var writer = new StreamWriter(csvFile);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            translations.ForEach(t =>
            {
                t.ForEach(csv.WriteField);
                csv.NextRecord();
            });
        }
    }
}
