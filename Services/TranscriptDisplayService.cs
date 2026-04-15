using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace theLinkType.Services;

public sealed class TranscriptDisplayService
{
    private readonly RichTextBox _richTextBox;
    private readonly Dispatcher _dispatcher;

    public TranscriptDisplayService(RichTextBox richTextBox, Dispatcher dispatcher)
    {
        _richTextBox = richTextBox;
        _dispatcher = dispatcher;

        if (_richTextBox.Document == null)
        {
            _richTextBox.Document = new FlowDocument();
        }
    }

    public async Task<Paragraph> AppendTranscriptAsync(string transcript)
    {
        return await _dispatcher.InvokeAsync(() =>
        {
            var p = new Paragraph { Margin = new System.Windows.Thickness(0) };
            p.Inlines.Add(new Run(transcript) { Foreground = Brushes.Black });

            _richTextBox.Document.Blocks.Add(p);
            _richTextBox.ScrollToEnd();
            return p;
        });
    }

    public async Task<Paragraph> AppendBlueStatusAsync(string text)
    {
        return await _dispatcher.InvokeAsync(() =>
        {
            var p = new Paragraph { Margin = new System.Windows.Thickness(0) };
            p.Inlines.Add(new Run(text) { Foreground = Brushes.Blue });

            _richTextBox.Document.Blocks.Add(p);
            _richTextBox.ScrollToEnd();
            return p;
        });
    }

    public async Task ReplaceParagraphAsync(Paragraph paragraph, string newText, Brush color)
    {
        await _dispatcher.InvokeAsync(() =>
        {
            paragraph.Inlines.Clear();
            paragraph.Inlines.Add(new Run(newText) { Foreground = color });
            _richTextBox.ScrollToEnd();
        });
    }

    public async Task AppendBlankLineAsync()
    {
        await _dispatcher.InvokeAsync(() =>
        {
            var p = new Paragraph { Margin = new System.Windows.Thickness(0) };
            p.Inlines.Add(new Run(" "));
            _richTextBox.Document.Blocks.Add(p);
            _richTextBox.ScrollToEnd();
        });
    }
}