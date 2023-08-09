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


# Files for translation

Copy the TranslateCSV.exe in the directories and make batch files with the commands below and execute them from a command line window

### ...\Extras\Localization.csv:

```
TranslateCSV.exe --csv-input Localization.csv --csv-output Localization.csv --deepl-target-language DE --csv-target-language Deutsch --csv-ref-input Localization-old.csv --deepl-free
IF ERRORLEVEL 0 COPY /Y Localization.csv Localization-old.csv
```

### ...\Extras\PDA.csv
```
TranslateCSV.exe --csv-input PDA.csv --csv-output PDA.csv --deepl-target-language DE --csv-target-language Deutsch --csv-ref-input PDA-old.csv --deepl-free
IF ERRORLEVEL 0 COPY /Y PDA.csv PDA-old.csv
```

### ...\Content\Configuration\Dialogues.csv
```
TranslateCSV.exe --csv-input Dialogues.csv --csv-output Dialogues.csv --deepl-target-language DE --csv-target-language Deutsch --csv-ref-input Dialogues-old.csv --deepl-free
IF ERRORLEVEL 0 COPY /Y Dialogues.csv Dialogues-old.csv
```