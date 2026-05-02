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
        private bool _suppressLangChange;

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
            InitLanguageBox();
            LoadSettingsToUI();
            ApplyLanguage();
        }

        // ── Lokalisierung ─────────────────────────────────────────────────

        private static readonly string AppVersion =
            System.Reflection.Assembly.GetExecutingAssembly()
                  .GetName().Version?.ToString(3) ?? "2.0.0";

        private string L(string key) => Localization.Get(key);

        private void InitLanguageBox()
        {
            _suppressLangChange = true;
            var lang = _settings.UILanguage;
            foreach (ComboBoxItem item in UILanguageBox.Items)
            {
                if (item.Tag?.ToString() == lang)
                {
                    UILanguageBox.SelectedItem = item;
                    break;
                }
            }
            if (UILanguageBox.SelectedItem == null)
                UILanguageBox.SelectedIndex = 0;
            Localization.SetLanguage(lang);
            _suppressLangChange = false;
        }

        private void UILanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressLangChange) return;
            if (UILanguageBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                Localization.SetLanguage(tag);
                _settings.UILanguage = tag;
                ApplyLanguage();
            }
        }

        private void ApplyLanguage()
        {
            Title = $"{L("WindowTitle")} v{AppVersion}";

            LangLabelText.Text = L("LangLabel");

            TabItem_Api.Header      = L("Tab_Api");
            TabItem_Files.Header    = L("Tab_Files");
            TabItem_Advanced.Header = L("Tab_Advanced");

            Group_ApiKey.Header      = L("Group_ApiKey");
            Group_Languages.Header   = L("Group_Languages");
            Group_CsvFiles.Header    = L("Group_CsvFiles");
            Group_HelperFiles.Header = L("Group_HelperFiles");
            Group_Advanced.Header    = L("Group_Advanced");
            Group_Log.Header         = L("Group_Log");

            Lbl_ApiKey.Content        = L("Lbl_ApiKey");
            Lbl_SourceLang.Content    = L("Lbl_SourceLang");
            Lbl_TargetLang.Content    = L("Lbl_TargetLang");
            Lbl_DeepLCode.Content     = L("Lbl_DeepLCode");
            Lbl_InputFile.Content     = L("Lbl_InputFile");
            Lbl_OutputFile.Content    = L("Lbl_OutputFile");
            Lbl_RefFile.Content       = L("Lbl_RefFile");
            Lbl_ProtectWords.Content  = L("Lbl_ProtectWords");
            Lbl_Glossar.Content       = L("Lbl_Glossar");
            Lbl_NewTranslate.Content  = L("Lbl_NewTranslate");
            Lbl_MaxTranslations.Content = L("Lbl_MaxTranslations");
            Lbl_ParallelCalls.Content = L("Lbl_ParallelCalls");

            FreeKeyRadio.Content = L("Radio_Free");
            ProKeyRadio.Content  = L("Radio_Pro");

            Hint_ApiUrl.Text      = L("Hint_ApiUrl");
            Hint_LangCodes.Text   = L("Hint_LangCodes");
            Hint_Files.Text       = L("Hint_Files");
            Hint_HelperFiles.Text = L("Hint_HelperFiles");
            Hint_Unlimited.Text   = L("Hint_Unlimited");
            Hint_Advanced.Text    = L("Hint_Advanced");

            StartButton.Content  = L("Btn_Start");
            CancelButton.Content = L("Btn_Cancel");

            ShowKeyToggle.ToolTip         = L("Tooltip_ShowKey");
            ApiKeyBox.ToolTip             = L("Tooltip_ApiKey");
            ApiKeyTextBox.ToolTip         = L("Tooltip_ApiKey");
            Lbl_ApiKey.ToolTip            = L("Tooltip_ApiKey");
            FreeKeyRadio.ToolTip          = L("Tooltip_RadioFree");
            ProKeyRadio.ToolTip           = L("Tooltip_RadioPro");
            CsvSourceLanguageBox.ToolTip  = L("Tooltip_SourceLang");
            CsvTargetLanguageBox.ToolTip  = L("Tooltip_TargetLang");
            DeepLTargetLanguageBox.ToolTip = L("Tooltip_DeepLCode");
            CsvInputBox.ToolTip           = L("Tooltip_InputFile");
            CsvOutputBox.ToolTip          = L("Tooltip_OutputFile");
            CsvRefBox.ToolTip             = L("Tooltip_RefFile");
            ProtectWordsBox.ToolTip       = L("Tooltip_ProtectWords");
            GlossarBox.ToolTip            = L("Tooltip_Glossar");
            BrowseCsvInputBtn.ToolTip     = L("Tooltip_Browse");
            BrowseCsvOutputBtn.ToolTip    = L("Tooltip_Browse");
            BrowseCsvRefBtn.ToolTip       = L("Tooltip_Browse");
            BrowseProtectWordsBtn.ToolTip = L("Tooltip_Browse");
            BrowseGlossarBtn.ToolTip      = L("Tooltip_Browse");
            NewTranslateCheck.ToolTip     = L("Tooltip_NewTranslate");
            LimitTranslationsBox.ToolTip  = L("Tooltip_MaxTranslations");
            MaxParallelCallsBox.ToolTip   = L("Tooltip_ParallelCalls");

            if (!_isRunning)
                StatusText.Text = L("Status_Ready");
        }

        // ── Einstellungen laden / speichern ──────────────────────────────

        private void LoadSettingsToUI()
        {
            ApiKeyBox.Password        = _settings.DeepLAuthKey;
            ApiKeyTextBox.Text        = _settings.DeepLAuthKey;
            FreeKeyRadio.IsChecked    = _settings.DeepLFreeAuthKey;
            ProKeyRadio.IsChecked     = !_settings.DeepLFreeAuthKey;

            CsvSourceLanguageBox.Text   = _settings.CsvSourceLanguage;
            CsvTargetLanguageBox.Text   = _settings.CsvTargetLanguage;
            DeepLTargetLanguageBox.Text = _settings.DeepLTargetLanguage;

            CsvInputBox.Text     = _settings.CsvFile;
            CsvOutputBox.Text    = _settings.CsvOutputFile;
            CsvRefBox.Text       = _settings.CsvRefFile;
            ProtectWordsBox.Text = _settings.KeepSpecialWordListFile;
            GlossarBox.Text      = _settings.GlossarFile;

            NewTranslateCheck.IsChecked = _settings.NewTranslate;
            LimitTranslationsBox.Text   = "5";   // immer 5 beim Start
            MaxParallelCallsBox.Text    = _settings.MaxParallelDeepLCalls.ToString();

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
            ApiKeyTextBox.Text       = ApiKeyBox.Password;
            ApiKeyBox.Visibility     = Visibility.Collapsed;
            ApiKeyTextBox.Visibility = Visibility.Visible;
            ApiKeyTextBox.CaretIndex = ApiKeyTextBox.Text.Length;
        }

        private void ShowKeyToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ApiKeyBox.Password       = ApiKeyTextBox.Text;
            ApiKeyTextBox.Visibility = Visibility.Collapsed;
            ApiKeyBox.Visibility     = Visibility.Visible;
        }

        // ── Datei-Browser ────────────────────────────────────────────────

        private void BrowseCsvInput_Click(object sender, RoutedEventArgs e)
            => BrowseOpen(CsvInputBox, "CSV|*.csv|*.*|*.*");

        private void BrowseCsvOutput_Click(object sender, RoutedEventArgs e)
            => BrowseSave(CsvOutputBox, "CSV|*.csv|*.*|*.*");

        private void BrowseCsvRef_Click(object sender, RoutedEventArgs e)
            => BrowseOpen(CsvRefBox, "CSV|*.csv|*.*|*.*");

        private void BrowseProtectWords_Click(object sender, RoutedEventArgs e)
            => BrowseOpen(ProtectWordsBox, "TXT|*.txt|*.*|*.*");

        private void BrowseGlossar_Click(object sender, RoutedEventArgs e)
            => BrowseOpen(GlossarBox, "CSV|*.csv|*.*|*.*");

        private static void BrowseOpen(TextBox target, string filter)
        {
            var dlg = new OpenFileDialog { Filter = filter };
            if (!string.IsNullOrWhiteSpace(target.Text)) dlg.FileName = target.Text;
            if (dlg.ShowDialog() == true) target.Text = dlg.FileName;
        }

        private static void BrowseSave(TextBox target, string filter)
        {
            var dlg = new SaveFileDialog { Filter = filter };
            if (!string.IsNullOrWhiteSpace(target.Text)) dlg.FileName = target.Text;
            if (dlg.ShowDialog() == true) target.Text = dlg.FileName;
        }

        // ── Übersetzung starten / abbrechen ──────────────────────────────

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;

            if (string.IsNullOrWhiteSpace(CsvInputBox.Text))
            {
                MessageBox.Show(L("Msg_MissingInput"), L("Msg_MissingTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(CsvTargetLanguageBox.Text))
            {
                MessageBox.Show(L("Msg_MissingTargetLang"), L("Msg_MissingTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(DeepLTargetLanguageBox.Text))
            {
                MessageBox.Show(L("Msg_MissingDeepLCode"), L("Msg_MissingTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveSettingsFromUI();
            LogBox.Clear();
            SetRunningState(true);
            AppendLog("═══════════════════════════════════════");
            AppendLog($"   {L("LogHeader")}");
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
                AppendLog(L("Msg_Cancelled"));
            }
            catch (Exception ex)
            {
                AppendLog($"❌ {ex.Message}");
                MessageBox.Show($"{L("Msg_Error")}:\n{ex.Message}", L("Msg_Error"),
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
            AppendLog(L("Msg_CancelRequest"));
            StatusText.Text = L("Status_Cancelling");
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
                StatusText.Text = L("Status_Running");
                StatusText.Foreground = System.Windows.Media.Brushes.DarkOrange;
            }
            else
            {
                StatusText.Text = L("Status_Ready");
                StatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void UpdateProgress(int count)
        {
            ProgressBar.IsIndeterminate = false;
            StatusText.Text = $"{count} ✓";
        }

        private void AppendLog(string message)
        {
            LogBox.AppendText(message + "\n");
            LogBox.ScrollToEnd();
        }
    }
}
