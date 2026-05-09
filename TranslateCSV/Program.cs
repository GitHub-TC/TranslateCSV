using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TranslateCSV
{
    public class TranslationService
    {
        private int _counter;

        public event Action<string>? Log;
        public event Action<int>? Progress;

        private void OnLog(string msg) => Log?.Invoke(msg);

        public async Task<bool> TranslateAsync(AppSettings options, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(options.DeepLAuthKey))
            {
                OnLog("❌ Kein API-Schlüssel angegeben.");
                return false;
            }

            List<List<string>> translations;
            try
            {
                translations = TranslationIO.ReadTranslationFromCsv(options.CsvFile);
            }
            catch (Exception ex)
            {
                OnLog($"❌ Fehler beim Lesen der CSV-Datei: {ex.Message}");
                return false;
            }

            OnLog($"📄 {translations.Count} Einträge geladen (inkl. Kopfzeile)");

            var targetIndex = translations[0].FindIndex(f => f.Equals(options.CsvTargetLanguage, StringComparison.InvariantCultureIgnoreCase));
            if (targetIndex == -1) { OnLog($"❌ Zielspalte \"{options.CsvTargetLanguage}\" nicht gefunden."); return false; }
            OnLog($"✔ Zielspalte \"{options.CsvTargetLanguage}\" → Spalte {targetIndex}");

            var sourceIndex = translations[0].FindIndex(f => f.Equals(options.CsvSourceLanguage, StringComparison.InvariantCultureIgnoreCase));
            if (sourceIndex == -1) { OnLog($"❌ Quellspalte \"{options.CsvSourceLanguage}\" nicht gefunden."); return false; }
            OnLog($"✔ Quellspalte \"{options.CsvSourceLanguage}\" → Spalte {sourceIndex}");

            var refData = string.IsNullOrEmpty(options.CsvRefFile) || !File.Exists(options.CsvRefFile)
                ? null
                : TranslationIO.ReadTranslationFromCsv(options.CsvRefFile).ToDictionaryUnique(t => t[0], t => t);

            int limit = options.LimitTranslations <= 0 ? int.MaxValue : options.LimitTranslations;

            int toTranslate = translations.Count(t =>
                options.NewTranslate ||
                string.IsNullOrWhiteSpace(t[targetIndex]) ||
                (refData != null && refData.TryGetValue(t[0], out var r) && r[sourceIndex] != t[sourceIndex]));

            OnLog($"🔄 Starte Übersetzung von {Math.Min(limit, toTranslate)} Einträgen...");

            using var deepL = new DeepLTranslate(options.MaxParallelDeepLCalls)
            {
                ApiKey           = options.DeepLAuthKey,
                IsFreeApiKey     = options.DeepLFreeAuthKey,
                SourceLanguage   = "EN",
                TargetLanguage   = options.DeepLTargetLanguage,
                LimitTranslations = limit,
            };
            deepL.OnLog += OnLog;

            ReadProtectWords(options, deepL);
            ReadGlossar(options, targetIndex, sourceIndex, deepL);

            _counter = 0;
            CopyDuplicates(translations, sourceIndex, targetIndex);

            var tasks = translations
                .Where(t => options.NewTranslate || string.IsNullOrWhiteSpace(t[targetIndex]))
                .Select(t => TranslateEntry(t, sourceIndex, targetIndex, deepL,
                    refData != null && refData.TryGetValue(t[0], out var r) ? r : null,
                    cancellationToken))
                .ToArray();

            bool cancelled = false;
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            OnLog($"✅ {_counter} Einträge übersetzt.");

            var outputFile = string.IsNullOrEmpty(options.CsvOutputFile) ? options.CsvFile : options.CsvOutputFile;
            OnLog($"💾 Schreibe Ergebnis nach \"{outputFile}\" ...");
            TranslationIO.WriteTranslationToCsv(translations, outputFile);

            if (cancelled)
            {
                OnLog("⚠️ Abgebrochen – bisherige Ergebnisse wurden gespeichert.");
                throw new OperationCanceledException(cancellationToken);
            }

            OnLog("🎉 Fertig!");
            return true;
        }

        private void ReadGlossar(AppSettings options, int targetIndex, int sourceIndex, DeepLTranslate deepL)
        {
            if (string.IsNullOrEmpty(options.GlossarFile)) return;
            var path = ResolveFile(options.GlossarFile);
            if (path == null) { OnLog($"ℹ Glossar-Datei nicht gefunden: {options.GlossarFile}"); return; }

            var glossar = TranslationIO.ReadTranslationFromCsv(path);
            deepL.Glossar = glossar
                .Where(g => !string.IsNullOrWhiteSpace(g[sourceIndex]))
                .ToDictionary(
                    g => new Regex($"(?'replace'{Regex.Escape(g[sourceIndex]!).Replace("\\ ", "\\s")})[\\W]"),
                    g => g[targetIndex]);
            OnLog($"📖 Glossar geladen: {deepL.Glossar.Count} Einträge");
        }

        private void ReadProtectWords(AppSettings options, DeepLTranslate deepL)
        {
            if (string.IsNullOrEmpty(options.KeepSpecialWordListFile)) return;
            var path = ResolveFile(options.KeepSpecialWordListFile);
            if (path == null) { OnLog($"ℹ Schutzwort-Datei nicht gefunden: {options.KeepSpecialWordListFile}"); return; }

            deepL.ProtectWords = File.ReadAllLines(path)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => new Regex(t))
                .ToArray();
            OnLog($"🛡 Schutzwörter geladen: {deepL.ProtectWords.Length} Muster");
        }

        private static string? ResolveFile(string path)
        {
            if (File.Exists(path)) return path;
            var next = Path.Combine(AppContext.BaseDirectory, path);
            return File.Exists(next) ? next : null;
        }

        private static void CopyDuplicates(List<List<string>> translations, int sourceIndex, int targetIndex)
        {
            var seen = new ConcurrentDictionary<string, string>();
            foreach (var t in translations)
            {
                if (seen.TryGetValue(t[sourceIndex], out var existing)) t[targetIndex] = existing;
                else seen.TryAdd(t[sourceIndex], t[targetIndex]);
            }
        }

        private async Task TranslateEntry(List<string> entry, int sourceIndex, int targetIndex,
            DeepLTranslate deepL, List<string>? refEntry, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            if (refEntry != null &&
                refEntry[sourceIndex] == entry[sourceIndex] &&
                !string.IsNullOrEmpty(refEntry[targetIndex]))
            {
                entry[targetIndex] = refEntry[targetIndex];
                return;
            }

            var result = await deepL.Translate(entry[sourceIndex], ct);
            if (result == null) return;

            entry[targetIndex] = result.Replace('"', '\'');
            Progress?.Invoke(Interlocked.Increment(ref _counter));
        }
    }
}

