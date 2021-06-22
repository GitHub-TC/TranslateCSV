# !!!! This is "under construction" and not finished yet !!!!

# TranslateCSV

A command line program to translate the CSV files from Empyrion or a scenario.

## DeepL Translation engine
For translation the translation API of DeepL https://www.deepl.com/translator is used. 
For this an access is needed which can be requested at https://www.deepl.com/pro#developer.

## Call

| Parameters | Description  |
| ---        | ---          |
| --deepl-auth-key          | DeepL API auth key from https://www.deepl.com/pro#developer if it is not specified it will be requested by input |
| --deepl-free              | (Default: true) Use DeepL API with 'DeepL API Free' auth key
| --deepl-target-language   | **Required** Target language for DeepL API target_lang e.g. DE from https://www.deepl.com/docs-api/translating-text/request/
| --csv-target-language     | **Required** Target language for CSV file from head line e.g. Deutsch
| --csv-input               | **Required** Input CSV file
| --new-translate           | Translate every entry and overwrite old translation
| --csv-output              | Output CSV file if the output written to another file
| --csv-source-language     | (Default: English) Source language for CSV file from head line e.g. English
| --csv-ref-input           | Old reference CSV file to compare and copy existing translations
| --limit-translations      | (Default: 2147483647) Limit the translations to N entries
| --max-parallel-deepl-calls| (Default: 8) Limit the translations to N calls parallel
| --help                    | Display this help screen.
| --version                 | Display version information.

```
TranslateCSV.exe --csv-input "C:\steamcmd\empyrion.server\Content\Scenarios\Reforged Eden\Extras\PDA\PDA.csv" --deepl-target-language DE --csv-target-language Deutsch
```