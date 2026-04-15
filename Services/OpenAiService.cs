using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace theLinkType.Services;

public sealed class OpenAiService
{
    private readonly Func<string> _getApiKey;
    private readonly HttpClient _httpClient;

    public OpenAiService(Func<string> getApiKey)
    {
        _getApiKey = getApiKey;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
    }

    public async Task<string> TranscribeAsync(byte[] wavBytes, string language, string prompt)
    {
        string apiKey = _getApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is missing.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/audio/transcriptions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var form = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(wavBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(fileContent, "file", "speech.wav");

        form.Add(new StringContent("gpt-4o-transcribe"), "model");
        form.Add(new StringContent(language), "language");
        form.Add(new StringContent("json"), "response_format");
        form.Add(new StringContent(prompt), "prompt");

        request.Content = form;

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Transcription failed: {response.StatusCode} {responseText}");
        }

        using JsonDocument doc = JsonDocument.Parse(responseText);
        return doc.RootElement.GetProperty("text").GetString() ?? string.Empty;
    }

    public async Task<string> TranslateAsync(string transcript, string targetLanguage, string translationPromptTemplate)
    {
        string apiKey = _getApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is missing.");

        string instructions = translationPromptTemplate.Replace("{targetLanguage}", targetLanguage);

        var body = new
        {
            model = "gpt-5-mini",
            instructions,
            input = transcript
        };

        string json = JsonSerializer.Serialize(body);

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        string responseText = await response.Content.ReadAsStringAsync();

        // Optional debug file
        File.WriteAllText("last_translate_response.json", responseText);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Translation failed: {response.StatusCode} {responseText}");
        }

        using JsonDocument doc = JsonDocument.Parse(responseText);

        // 1) Try convenience field first if present
        if (doc.RootElement.TryGetProperty("output_text", out JsonElement outputTextElement) &&
            outputTextElement.ValueKind == JsonValueKind.String)
        {
            string? directText = outputTextElement.GetString();
            if (!string.IsNullOrWhiteSpace(directText))
                return directText;
        }

        // 2) Parse output[] -> message -> content[] -> output_text -> text
        if (doc.RootElement.TryGetProperty("output", out JsonElement outputArray) &&
            outputArray.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement outputItem in outputArray.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("type", out JsonElement itemType) ||
                    itemType.GetString() != "message")
                {
                    continue;
                }

                if (!outputItem.TryGetProperty("content", out JsonElement contentArray) ||
                    contentArray.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement contentItem in contentArray.EnumerateArray())
                {
                    if (!contentItem.TryGetProperty("type", out JsonElement contentType) ||
                        contentType.GetString() != "output_text")
                    {
                        continue;
                    }

                    if (contentItem.TryGetProperty("text", out JsonElement textElement))
                    {
                        string? text = textElement.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                            return text;
                    }
                }
            }
        }

        return string.Empty;
    }
}