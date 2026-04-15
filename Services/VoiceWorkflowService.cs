using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using theLinkType.Services;

namespace theLinkType.Services;

public sealed class VoiceWorkflowService
{
    private readonly AudioRecorderService _audioRecorder;
    private readonly OpenAiService _openAiService;
    private readonly TranscriptDisplayService _displayService;
    private readonly Func<string, Task> _log;

    private readonly SemaphoreSlim _mutex = new(1, 1);

    public VoiceWorkflowService(
        AudioRecorderService audioRecorder,
        OpenAiService openAiService,
        TranscriptDisplayService displayService,
        Func<string, Task> log)
    {
        _audioRecorder = audioRecorder;
        _openAiService = openAiService;
        _displayService = displayService;
        _log = log;
    }

    public async Task BeginRecordingAsync()
    {
        if (_audioRecorder.IsRecording)
            return;

        await _mutex.WaitAsync();
        try
        {
            if (_audioRecorder.IsRecording)
                return;

            await _audioRecorder.StartAsync();
            await _log("Recording started.");
        }
        catch (Exception ex)
        {
            await _log($"Recording start failed: {ex.Message}");
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task StopRecordTranscribeTranslateAndDisplayAsync(
        bool translateEnabled,
        string? targetLanguage,
        string translationPromptTemplate,
        string transcriptionLanguage,
        string transcriptionPrompt)
    {
        await _mutex.WaitAsync();

        try
        {
            if (!_audioRecorder.IsRecording)
                return;

            byte[] wavBytes = await _audioRecorder.StopAndGetWavBytesAsync();

            if (wavBytes.Length == 0)
            {
                await _log("No audio captured.");
                return;
            }

            string transcript = await _openAiService.TranscribeAsync(
                wavBytes,
                transcriptionLanguage,
                transcriptionPrompt);

            if (string.IsNullOrWhiteSpace(transcript))
            {
                await _log("Transcript was empty.");
                return;
            }

            await _displayService.AppendTranscriptAsync(transcript);

            if (!translateEnabled)
            {
                await _displayService.AppendBlankLineAsync();
                return;
            }

            Paragraph translatingParagraph = await _displayService.AppendBlueStatusAsync("translating...");

            string translated = await _openAiService.TranslateAsync(
                transcript,
                targetLanguage ?? "English",
                translationPromptTemplate);

            if (string.IsNullOrWhiteSpace(translated))
            {
                translated = "[翻譯失敗: API回傳空白的結果]";
            }

            await _displayService.ReplaceParagraphAsync(translatingParagraph, translated, Brushes.Blue);
            await _displayService.AppendBlankLineAsync();
        }
        catch (Exception ex)
        {
            await _log($"Workflow failed: {ex.Message}");
        }
        finally
        {
            _mutex.Release();
        }
    }
}