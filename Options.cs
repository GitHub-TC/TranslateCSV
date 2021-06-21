using CommandLine;

namespace TranslateCSV
{
    partial class Program
    {
        public class Options
        {
            [Option("deepl-auth-key", Required = false, HelpText = "DeepL API auth key from https://www.deepl.com/pro#developer if it is not specified it will be requested by input")]
            public string DeepLAuthKey { get; set; }

            [Option("deepl-free", Required = false, Default = true, HelpText = "Use DeepL API with 'DeepL API Free' auth key")]
            public bool DeepLFreeAuthKey { get; set; }

            [Option("deepl-target-language", Required = true, HelpText = "Target language for DeepL API target_lang e.g. DE from https://www.deepl.com/docs-api/translating-text/request/")]
            public string DeepLTargetLanguage { get; set; }

            [Option("csv-target-language", Required = true, HelpText = "Target language for CSV file from head line e.g. Deutsch")]
            public string CsvTargetLanguage { get; set; }

            [Option("csv-source-language", Required = false, HelpText = "Source language for CSV file from head line e.g. English")]
            public string CsvSourceLanguage { get; set; }

            [Option("csv-input", Required = true, HelpText = "Input CSV file")]
            public string CsvFile { get; set; }

            [Option("new-translate", Required = false, HelpText = "Translate every entry and overwrite old translation")]
            public bool NewTranslate { get; set; }

            [Option("csv-output", Required = false, HelpText = "Output CSV file if the output written to another file")]
            public string CsvOutputFile { get; set; }

            [Option("limit-translations", Required = false, Default = int.MaxValue, HelpText = "Limit the translations to N entries")]
            public int LimitTranslations { get; set; }

        }
    }
}
