namespace theLinkType.Models;

public sealed class AppSettings
{
    public string? OpenAiApiKey { get; set; }

    public string? RefinePrompt { get; set; }

    public static string DefaultRefinePrompt =>
        "Rewrite the following speech transcript into clear, natural, well-punctuated sentences. " +
        "Preserve the original meaning, preserve the original language, do not add new facts, " +
        "and make it easy to read.";
}