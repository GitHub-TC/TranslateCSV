using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TranslateCSV
{
    partial class Program
    {
        private static int Counter;

        static void Main(string[] args) => Parser.Default.ParseArguments<Options>(args).WithParsed(Translate);

        private static void Translate(Options options)
        {
            if (string.IsNullOrEmpty(options.DeepLAuthKey))
            {
                Console.Write("DeepL REST API auth key:");
                options.DeepLAuthKey = Console.ReadLine();

                if (string.IsNullOrEmpty(options.DeepLAuthKey)) return;
            }

            List<List<string>> translations = TranslationIO.ReadTranslationFromCsv(options.CsvFile);

            Console.WriteLine($"Found {translations.Count} entries total (with headline)");
            var targetTranslateIndex = translations[0].FindIndex(f => f.Equals(options.CsvTargetLanguage, StringComparison.InvariantCultureIgnoreCase));
            if (targetTranslateIndex == -1)
            {
                Console.WriteLine($"Not found translation col for \"{options.CsvTargetLanguage}\"");
                return;
            }
            Console.WriteLine($"Found translation col for \"{options.CsvTargetLanguage}\" at {targetTranslateIndex}");

            var sourceTranslateIndex = translations[0].FindIndex(f => f.Equals(options.CsvSourceLanguage, StringComparison.InvariantCultureIgnoreCase));
            if (sourceTranslateIndex == -1)
            {
                Console.WriteLine($"Not found source translation col for \"{options.CsvSourceLanguage}\"");
                return;
            }
            Console.WriteLine($"Found source translation col for \"{options.CsvSourceLanguage}\" at {sourceTranslateIndex}");

            Console.Write($"Start {(options.NewTranslate ? "new " : "")}translation for {Math.Min(options.LimitTranslations, translations.Count(t => options.NewTranslate || string.IsNullOrWhiteSpace(t[targetTranslateIndex])))} entries (y/n)? ");
            if (Console.ReadKey().KeyChar != 'y')
            {
                Console.WriteLine();
                Console.WriteLine("cancled.");
                return;
            }
            Console.WriteLine();

            var deepLTranslate = new DeepLTranslate()
            {
                ApiKey          = options.DeepLAuthKey,
                IsFreeApiKey    = options.DeepLFreeAuthKey,
                SourceLanguage  = "EN",
                TargetLanguage  = options.DeepLTargetLanguage,
            };

            Counter = 0;

            Task.WaitAll(translations
                .Where(t => options.NewTranslate || string.IsNullOrWhiteSpace(t[targetTranslateIndex]))
                .Select(t => TranslateText(t, sourceTranslateIndex, targetTranslateIndex, deepLTranslate))
                .Take(options.LimitTranslations)
                .ToArray());

            Console.WriteLine($"{Counter}");

            Console.WriteLine($"write output to \"{options.CsvOutputFile ?? options.CsvFile}\"");
            TranslationIO.WriteTranslationToCsv(translations, options.CsvOutputFile ?? options.CsvFile);
        }

        private static async Task TranslateText(List<string> textEntries, int sourceTranslateIndex, int targetTranslateIndex, DeepLTranslate deepLTranslate)
        {
            textEntries[targetTranslateIndex] = await deepLTranslate.Translate(textEntries[sourceTranslateIndex]);
            Interlocked.Increment(ref Counter);
            Console.Write($"{Counter}\r");
        }
    }
}
