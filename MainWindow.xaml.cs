using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using theLinkType.Models;
using theLinkType.Services;

namespace theLinkType;

public partial class MainWindow : Window
{
    private readonly AppConfigService _configService;
    private readonly AudioRecorderService _audioRecorderService;
    private readonly OpenAiService _openAiService;
    private readonly TranscriptDisplayService _displayService;
    private readonly VoiceWorkflowService _workflowService;

    private AppConfig _config = new();
    private bool _started;

    public MainWindow()
    {
        InitializeComponent();

        _configService = new AppConfigService();
        _config = _configService.Load();

        _audioRecorderService = new AudioRecorderService();
        _openAiService = new OpenAiService(() => _config.OpenAI.ApiKey);
        _displayService = new TranscriptDisplayService(OutputBox, Dispatcher);

        _workflowService = new VoiceWorkflowService(
            _audioRecorderService,
            _openAiService,
            _displayService,
            LogAsync);
    }

    private void TranslationToggle_Checked(object sender, RoutedEventArgs e)
    {
        LanguageComboBox.Visibility = Visibility.Visible;
    }

    private void TranslationToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        LanguageComboBox.Visibility = Visibility.Collapsed;
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_started) return;

        if (string.IsNullOrWhiteSpace(_config.OpenAI.ApiKey))
        {
            MessageBox.Show("OpenAI API key is missing in appsettings.json.", "Missing API Key",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await _workflowService.BeginRecordingAsync();
        _started = true;
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_started) return;

        bool translateEnabled = TranslationToggle.IsChecked == true;
        string? targetLanguage = translateEnabled ? GetSelectedLanguage() : null;

        await _workflowService.StopRecordTranscribeTranslateAndDisplayAsync(
            translateEnabled,
            targetLanguage,
            _config.OpenAI.TranslationPromptTemplate,
            _config.OpenAI.TranscriptionLanguage,
            _config.OpenAI.TranscriptionPrompt);

        _started = false;
    }

    private string GetSelectedLanguage()
    {
        if (LanguageComboBox.SelectedItem is ComboBoxItem item &&
            item.Content is string value)
        {
            return value;
        }

        return "English";
    }

    private Task LogAsync(string message)
    {
        return Task.CompletedTask;
    }
}