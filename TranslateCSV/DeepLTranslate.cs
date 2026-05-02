using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TranslateCSV
{
    public class DeepLTranslate : IDisposable
    {
        public SemaphoreSlim ParallelDeepLCallsSemaphore { get; set; }
        private Lazy<HttpClient>? DeepLHttpClient { get; set; }

        public string ApiKey { get; set; } = string.Empty;
        public bool IsFreeApiKey { get; set; }
        public string TargetLanguage { get; set; } = string.Empty;
        public string SourceLanguage { get; set; } = string.Empty;
        public int LimitTranslations { get; set; }

        public Regex[] ProtectWords { get; set; } = Array.Empty<Regex>();
        public Dictionary<Regex, string> Glossar { get; set; } = new();
        public ConcurrentDictionary<string, string> AlreadyTranslated { get; set; } = new();

        public event Action<string>? OnLog;

        int translationsCounter;

        public DeepLTranslate(int maxParallelDeepLCalls)
        {
            ParallelDeepLCallsSemaphore = new SemaphoreSlim(maxParallelDeepLCalls);
            DeepLHttpClient = new Lazy<HttpClient>(() => new HttpClient
            {
                BaseAddress = new Uri(IsFreeApiKey ? "https://api-free.deepl.com" : "https://api.deepl.com")
            });
        }

        public void Dispose()
        {
            ParallelDeepLCallsSemaphore?.Dispose();
            ParallelDeepLCallsSemaphore = null!;

            if (DeepLHttpClient?.IsValueCreated == true)
            {
                DeepLHttpClient.Value.Dispose();
                DeepLHttpClient = null;
            }
        }

        public async Task<string?> Translate(string text, CancellationToken cancellationToken = default)
        {
            if (translationsCounter > LimitTranslations) return null;

            await ParallelDeepLCallsSemaphore.WaitAsync(cancellationToken);
            try
            {
                return await TranslateCall(text, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error)
            {
                OnLog?.Invoke($"Ausnahme bei \"{text}\": {error.Message}");
            }
            finally
            {
                ParallelDeepLCallsSemaphore.Release();
            }

            return null;
        }

        private async Task<string?> TranslateCall(string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text) || Interlocked.Increment(ref translationsCounter) > LimitTranslations) return null;
            if (AlreadyTranslated.TryGetValue(text, out var alreadyTranslated)) return alreadyTranslated;

            var protect = new ProtectSpecials { ProtectWords = ProtectWords, Glossar = Glossar };
            var protectedText = protect.Protect(text);
            var startText = 0;
            string? completeTranslatedText = null;

            while (true)
            {
                var endText = protectedText.Length > (startText + 1000)
                    ? Math.Max(startText + 1000, protectedText.IndexOf('.', startText + 1000) + 1)
                    : protectedText.Length;

                var queryString = string.Join("&", new Dictionary<string, string>
                {
                    ["auth_key"]     = ApiKey,
                    ["source_lang"]  = SourceLanguage,
                    ["target_lang"]  = TargetLanguage,
                    ["tag_handling"] = "xml",
                    ["ignore_tags"]  = "x",
                    ["text"]         = protectedText.Substring(startText, endText - startText)
                }.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

                startText = endText;

                var response = await DeepLHttpClient!.Value.GetAsync($"v2/translate?{queryString}", cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                        var translatedText = protect.Restore(
                            ((JsonElement)jsonResponse!["translations"])[0].GetProperty("text").GetString()!);

                        completeTranslatedText = completeTranslatedText == null
                            ? translatedText
                            : completeTranslatedText + " " + translatedText;

                        if (endText == protectedText.Length)
                        {
                            AlreadyTranslated.TryAdd(text, completeTranslatedText);
                            return completeTranslatedText;
                        }
                    }
                    catch (Exception error)
                    {
                        OnLog?.Invoke($"Fehler beim Übersetzen von \"{text}\": {response.StatusCode}: {responseContent} → {error.Message}");
                        return null;
                    }
                }
                else
                {
                    OnLog?.Invoke($"API-Fehler für \"{text}\": {response.StatusCode}: {responseContent}");
                    return null;
                }
            }
        }
    }
}
