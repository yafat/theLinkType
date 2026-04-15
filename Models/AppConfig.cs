namespace theLinkType.Models;

public sealed class AppConfig
{
    public OpenAiConfig OpenAI { get; set; } = new();
}

public sealed class OpenAiConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string TranscriptionLanguage { get; set; } = "zh";
    public string TranscriptionPrompt { get; set; } =
        "Please transcribe in Traditional Chinese used in Taiwan, with natural punctuation.";

    public string TranslationPromptTemplate { get; set; } =
        "Translate the following text into {targetLanguage}. Preserve the meaning accurately. Output only the translated result.";
}