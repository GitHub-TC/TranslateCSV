using Microsoft.Win32;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace TranslateCSV
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        private static readonly string[] DeepLLanguages =
        [
            "BG", "CS", "DA", "DE", "EL", "EN-GB", "EN-US",
            "ES", "ET", "FI", "FR", "HU", "ID", "IT", "JA",
            "KO", "LT", "LV", "NL", "PL", "PT-BR", "PT-PT",
            "RO", "RU", "SK", "SL", "SV", "TR", "UK", "ZH"
        ];

        public MainWindow()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            DeepLTargetLanguageBox.ItemsSource = DeepLLanguages;
            LoadSettingsToUI();
        }

        // ── Einstellungen laden / speichern ──────────────────────────────

        private void LoadSettingsToUI()
        {
            ApiKeyBox.Password        = _settings.DeepLAuthKey;
            ApiKeyTextBox.Text        = _settings.DeepLAuthKey;
            FreeKeyRadio.IsChecked    = _settings.DeepLFreeAuthKey;
            ProKeyRadio.IsChecked     = !_settings.DeepLFreeAuthKey;

            CsvSourceLanguageBox.Text = _settings.CsvSourceLanguage;
            CsvTargetLanguageBox.Text = _settings.CsvTargetLanguage;
            DeepLTargetLanguageBox.Text = _settings.DeepLTargetLanguage;

            CsvInputBox.Text          = _settings.CsvFile;
            CsvOutputBox.Text         = _settings.CsvOutputFile;
            CsvRefBox.Text            = _settings.CsvRefFile;
            ProtectWordsBox.Text      = _settings.KeepSpecialWordListFile;
            GlossarBox.Text           = _settings.GlossarFile;

            NewTranslateCheck.IsChecked     = _settings.NewTranslate;
            LimitTranslationsBox.Text       = _settings.LimitTranslations.ToString();
            MaxParallelCallsBox.Text        = _settings.MaxParallelDeepLCalls.ToString();

            Width  = _settings.WindowWidth;
            Height = _settings.WindowHeight;
            if (!double.IsNaN(_settings.WindowLeft) && !double.IsNaN(_settings.WindowTop))
            {
                Left = _settings.WindowLeft;
                Top  = _settings.WindowTop;
            }
        }

        private void SaveSettingsFromUI()
        {
            _settings.DeepLAuthKey        = ApiKeyBox.Password;
            _settings.DeepLFreeAuthKey    = FreeKeyRadio.IsChecked == true;
            _settings.CsvSourceLanguage   = CsvSourceLanguageBox.Text;
            _settings.CsvTargetLanguage   = CsvTargetLanguageBox.Text;
            _settings.DeepLTargetLanguage = DeepLTargetLanguageBox.Text;
            _settings.CsvFile             = CsvInputBox.Text;
            _settings.CsvOutputFile       = CsvOutputBox.Text;
            _settings.CsvRefFile          = CsvRefBox.Text;
            _settings.KeepSpecialWordListFile = ProtectWordsBox.Text;
            _settings.GlossarFile         = GlossarBox.Text;
            _settings.NewTranslate        = NewTranslateCheck.IsChecked == true;

            if (int.TryParse(LimitTranslationsBox.Text, out var limit))
                _settings.LimitTranslations = limit;
            if (int.TryParse(MaxParallelCallsBox.Text, out var parallel))
                _settings.MaxParallelDeepLCalls = parallel;

            _settings.WindowWidth  = Width;
            _settings.WindowHeight = Height;
            _settings.WindowLeft   = Left;
            _settings.WindowTop    = Top;

            _settings.Save();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
            => SaveSettingsFromUI();

        // ── API-Key anzeigen / verbergen ─────────────────────────────────

        private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
            => ApiKeyTextBox.Text = ApiKeyBox.Password;

        private void ApiKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => ApiKeyBox.Password = ApiKeyTextBox.Text;

        private void ShowKeyToggle_Checked(object sender, RoutedEventArgs e)
        {
            ApiKeyTextBox.Text        = ApiKeyBox.Password;
            ApiKeyBox.Visibility      = Visibility.Collapsed;
            ApiKeyTextBox.Visibility  = Visibility.Visible;
            ApiKeyTextBox.CaretIndex  = ApiKeyTextBox.Text.Length;
        }

        private void ShowKeyToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ApiKeyBox.Password        = ApiKeyTextBox.Text;
            ApiKeyTextBox.Visibility  = Visibility.Collapsed;
            ApiKeyBox.Visibility      = Visibility.Visible;
        }

        // ── Datei-Browser ────────────────────────────────────────────────

        private void BrowseCsvInput_Click(object sender, RoutedEventArgs e)
            => BrowseOpen(CsvInputBox, "CSV-Dateien|*.csv|Alle Dateien|*.*");

        private void BrowseCsvOutput_Click(object sender, RoutedEventArgs e)
            => BrowseSave(CsvOutputBox, "CSV-Dateien|*.csv|Alle Dateien|*.*");

        private void BrowseCsvRef_Click(object sender, RoutedEventArgs e)
            => BrowseOpen(CsvRefBox, "CSV-Dateien|*.csv|Alle Dateien|*.*");

        private void BrowseProtectWords_Click(object sender, RoutedEventArgs e)
            => BrowseOpen(ProtectWordsBox, "Textdateien|*.txt|Alle Dateien|*.*");

        private void BrowseGlossar_Click(object sender, RoutedEventArgs e)
            => BrowseOpen(GlossarBox, "CSV-Dateien|*.csv|Alle Dateien|*.*");

        private static void BrowseOpen(TextBox target, string filter)
        {
            var dlg = new OpenFileDialog { Filter = filter, Title = "Datei öffnen" };
            if (!string.IsNullOrWhiteSpace(target.Text)) dlg.FileName = target.Text;
            if (dlg.ShowDialog() == true) target.Text = dlg.FileName;
        }

        private static void BrowseSave(TextBox target, string filter)
        {
            var dlg = new SaveFileDialog { Filter = filter, Title = "Ausgabedatei festlegen" };
            if (!string.IsNullOrWhiteSpace(target.Text)) dlg.FileName = target.Text;
            if (dlg.ShowDialog() == true) target.Text = dlg.FileName;
        }

        // ── Übersetzung starten / abbrechen ──────────────────────────────

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;

            if (string.IsNullOrWhiteSpace(CsvInputBox.Text))
            {
                MessageBox.Show("Bitte eine Eingabedatei auswählen (Tab 'Dateien').",
                    "Fehlende Eingabe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(CsvTargetLanguageBox.Text))
            {
                MessageBox.Show("Bitte die Zielsprache der CSV-Datei angeben, z.B. 'Deutsch' (Tab 'API & Sprachen').",
                    "Fehlende Eingabe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(DeepLTargetLanguageBox.Text))
            {
                MessageBox.Show("Bitte den DeepL-Sprachcode angeben, z.B. 'DE' (Tab 'API & Sprachen').",
                    "Fehlende Eingabe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveSettingsFromUI();
            LogBox.Clear();
            SetRunningState(true);
            AppendLog("═══════════════════════════════════════");
            AppendLog("   TranslateCSV 2.0 – Übersetzung läuft");
            AppendLog("═══════════════════════════════════════");

            _cts = new CancellationTokenSource();
            var service = new TranslationService();
            service.Log      += msg   => Dispatcher.Invoke(() => AppendLog(msg));
            service.Progress += count => Dispatcher.Invoke(() => UpdateProgress(count));

            try
            {
                await service.TranslateAsync(_settings, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                AppendLog("⚠  Übersetzung wurde abgebrochen.");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Unerwarteter Fehler: {ex.Message}");
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetRunningState(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            AppendLog("⚠  Abbruch angefordert – bitte warten...");
        }

        // ── Hilfsmethoden ────────────────────────────────────────────────

        private void SetRunningState(bool running)
        {
            _isRunning             = running;
            StartButton.IsEnabled  = !running;
            CancelButton.IsEnabled = running;
            ProgressBar.Visibility = running ? Visibility.Visible : Visibility.Collapsed;

            if (running)
            {
                ProgressBar.IsIndeterminate = true;
                StatusText.Text = "Übersetzung läuft...";
                StatusText.Foreground = System.Windows.Media.Brushes.DarkOrange;
            }
            else
            {
                StatusText.Text = "Bereit";
                StatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void UpdateProgress(int count)
        {
            ProgressBar.IsIndeterminate = false;
            StatusText.Text = $"{count} übersetzt";
        }

        private void AppendLog(string message)
        {
            LogBox.AppendText(message + "\n");
            LogBox.ScrollToEnd();
        }
    }
}
