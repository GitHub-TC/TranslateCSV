# TranslateCSV

A command line program to translate the CSV files from Empyrion or a scenario.

## DeepL Translation engine
For translation the translation API of DeepL https://www.deepl.com/translator is used.\
For this an access is needed which can be requested at https://www.deepl.com/pro#developer.

For the firsts Tests you can start with the "Deep API Free" with 500k characters per month.

![](Screenshots/DeepL.png)

## Call

For the current commandline parameter please call:
```
TranslateCSV.exe --help
```

for example:

```
TranslateCSV.exe --csv-input "C:\steamcmd\empyrion.server\Content\Scenarios\Reforged Eden\Extras\PDA\PDA.csv" --deepl-target-language DE --csv-target-language Deutsch --deepl-free
```