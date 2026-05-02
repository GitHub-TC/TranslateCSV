using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TranslateCSV
{
    public class AppSettings
    {
        public string DeepLAuthKey { get; set; } = string.Empty;
        public bool DeepLFreeAuthKey { get; set; } = true;
        public string DeepLTargetLanguage { get; set; } = "DE";
        public string CsvTargetLanguage { get; set; } = "Deutsch";
        public string CsvSourceLanguage { get; set; } = "English";
        public string CsvFile { get; set; } = string.Empty;
        public string CsvRefFile { get; set; } = string.Empty;
        public bool NewTranslate { get; set; } = false;
        public string CsvOutputFile { get; set; } = string.Empty;
        public string KeepSpecialWordListFile { get; set; } = "ProtectWords.txt";
        public string GlossarFile { get; set; } = "GlossarWords.csv";
        public int LimitTranslations { get; set; } = 0;   // 0 = unbegrenzt
        public int MaxParallelDeepLCalls { get; set; } = 8;

        public double WindowWidth { get; set; } = 780;
        public double WindowHeight { get; set; } = 700;
        public double WindowLeft { get; set; } = double.NaN;
        public double WindowTop { get; set; } = double.NaN;

        public string UILanguage { get; set; } = "de";

        [JsonIgnore]
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TranslateCSV", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }
}

