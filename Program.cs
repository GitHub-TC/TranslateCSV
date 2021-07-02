using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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

            var translations = TranslationIO.ReadTranslationFromCsv(options.CsvFile);

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

            var translationsRef = string.IsNullOrEmpty(options.CsvRefFile) ? null : TranslationIO.ReadTranslationFromCsv(options.CsvRefFile).ToDictionary(t => t[0], t => t);

            int countTranslations = translations.Count(t => 
                options.NewTranslate || 
                string.IsNullOrWhiteSpace(t[targetTranslateIndex]) ||
                (translationsRef != null && translationsRef.TryGetValue(t[0], out var refData) && refData[sourceTranslateIndex] != t[sourceTranslateIndex])
            );
            Console.Write($"Start {(options.NewTranslate ? "new " : "")}translation for {Math.Min(options.LimitTranslations, countTranslations)} entries (y/n)? ");

            if (Console.ReadKey().KeyChar != 'y')
            {
                Console.WriteLine();
                Console.WriteLine("cancled.");
                return;
            }
            Console.WriteLine();

            var deepLTranslate = new DeepLTranslate(options.MaxParallelDeepLCalls)
            {
                ApiKey            = options.DeepLAuthKey,
                IsFreeApiKey      = options.DeepLFreeAuthKey,
                SourceLanguage    = "EN",
                TargetLanguage    = options.DeepLTargetLanguage,
                LimitTranslations = options.LimitTranslations,
            };

            if (!string.IsNullOrEmpty(options.KeepSpecialWordListFile))
            {
                deepLTranslate.ProtectWords = File.ReadAllLines(File.Exists(options.KeepSpecialWordListFile)
                                                ? options.KeepSpecialWordListFile
                                                : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), options.KeepSpecialWordListFile))
                                                .Where(t => !string.IsNullOrWhiteSpace(t))
                                                .Select(t => new Regex(t))
                                                .ToArray();
            }

            if (!string.IsNullOrEmpty(options.GlossarFile))
            {
                var glossar = TranslationIO.ReadTranslationFromCsv(options.GlossarFile);
                deepLTranslate.Glossar = glossar
                    .Where(g => !string.IsNullOrWhiteSpace(g[sourceTranslateIndex]))
                    .ToDictionary(g => new Regex($"(?'replace'{g[sourceTranslateIndex]?.Replace(" ","\\s")})[\\W]"), g => g[targetTranslateIndex]);
            }

            Counter = 0;

            Task.WaitAll(translations
                .Where(t => options.NewTranslate || string.IsNullOrWhiteSpace(t[targetTranslateIndex]))
                .Select(t => TranslateText(t, sourceTranslateIndex, targetTranslateIndex, deepLTranslate,
                        translationsRef != null && translationsRef.TryGetValue(t[0], out var refTranslation) ? refTranslation : null))
                .ToArray());

            Console.WriteLine($"{Counter}");

            Console.WriteLine($"write output to \"{options.CsvOutputFile ?? options.CsvFile}\"");
            TranslationIO.WriteTranslationToCsv(translations, options.CsvOutputFile ?? options.CsvFile);
        }

        private static async Task TranslateText(List<string> textEntries, int sourceTranslateIndex, int targetTranslateIndex, DeepLTranslate deepLTranslate, List<string> refTranslation)
        {
            string sourceText = textEntries[sourceTranslateIndex];

            if (refTranslation != null                              && 
                refTranslation[sourceTranslateIndex] == sourceText  && 
                !string.IsNullOrEmpty(refTranslation[targetTranslateIndex]))
            {
                textEntries[targetTranslateIndex] = refTranslation[targetTranslateIndex];
                return;
            }

            var result = await deepLTranslate.Translate(sourceText);
            if (result == null) return;

            textEntries[targetTranslateIndex] = result;
            Interlocked.Increment(ref Counter);
            Console.Write($"{Counter}\r");
        }
    }
}
