using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TranslateCSV
{
    internal class DeepLTranslate : IDisposable
    {
        public SemaphoreSlim ParallelDeepLCallsSemaphore { get; set; }
        private Lazy<HttpClient> DeepLHttpClient { get; set; }

        public string ApiKey { get; internal set; }
        public bool IsFreeApiKey { get; internal set; }
        public string TargetLanguage { get; internal set; }
        public string SourceLanguage { get; internal set; }

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
            ParallelDeepLCallsSemaphore = null;

            if (DeepLHttpClient?.IsValueCreated == true)
            {
                DeepLHttpClient.Value.Dispose();
                DeepLHttpClient = null;
            }
        }

        public async Task<string> Translate(string text)
        {
            await ParallelDeepLCallsSemaphore.WaitAsync();

            try
            {
                return await TranslateCall(text);
            }
            catch (Exception error)
            {
                Console.WriteLine($"Exception: {text} error: {error}");
            }
            finally
            {
                ParallelDeepLCallsSemaphore.Release();
            }

            return null;
        }

        private async Task<string> TranslateCall(string text)
        {
            var @params = new Dictionary<string, string>() { 
                { "auth_key",    ApiKey },
                { "source_lang", SourceLanguage },
                { "target_lang", TargetLanguage },
                { "text", text }
            };

            var response        = await DeepLHttpClient.Value.GetAsync(new Uri(DeepLHttpClient.Value.BaseAddress, QueryHelpers.AddQueryString("v2/translate", @params)));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode) {
                var jsonResonse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);
                return jsonResonse["text"];
            }
            else Console.WriteLine($"Translate '{text}' error {response.StatusCode}: {responseContent}");

            return null;
        }
    }
}