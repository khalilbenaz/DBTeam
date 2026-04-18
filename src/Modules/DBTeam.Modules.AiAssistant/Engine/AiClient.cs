using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DBTeam.Modules.AiAssistant.Engine;

public sealed class AiMessage { public string Role { get; set; } = "user"; public string Content { get; set; } = ""; }

public enum AiProvider { OpenAI, Anthropic, Ollama, AzureOpenAI, Custom }

public sealed class AiSettings
{
    public AiProvider Provider { get; set; } = AiProvider.Anthropic;
    public string Endpoint { get; set; } = "https://api.anthropic.com/v1/messages";
    public string Model { get; set; } = "claude-sonnet-4-6";
    public string ApiKey { get; set; } = "";
    public int MaxTokens { get; set; } = 2048;
    public string SystemPrompt { get; set; } = "You are an expert T-SQL / SQL Server assistant. Answer concisely with code blocks when showing SQL. Never include column values from the user data, only schemas.";
}

public sealed class AiClient
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(2) };

    public async Task<string> ChatAsync(AiSettings s, IReadOnlyList<AiMessage> messages, CancellationToken ct = default)
    {
        return s.Provider switch
        {
            AiProvider.Anthropic => await AnthropicAsync(s, messages, ct),
            AiProvider.OpenAI or AiProvider.AzureOpenAI or AiProvider.Custom => await OpenAiCompatibleAsync(s, messages, ct),
            AiProvider.Ollama => await OllamaAsync(s, messages, ct),
            _ => "(unsupported provider)"
        };
    }

    private static async Task<string> AnthropicAsync(AiSettings s, IReadOnlyList<AiMessage> messages, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, s.Endpoint);
        req.Headers.Add("x-api-key", s.ApiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        var body = new
        {
            model = s.Model,
            max_tokens = s.MaxTokens,
            system = s.SystemPrompt,
            messages = messages
        };
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        var txt = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) return $"[{(int)res.StatusCode}] {txt}";
        using var doc = JsonDocument.Parse(txt);
        if (doc.RootElement.TryGetProperty("content", out var arr) && arr.GetArrayLength() > 0)
            return arr[0].GetProperty("text").GetString() ?? "";
        return txt;
    }

    private static async Task<string> OpenAiCompatibleAsync(AiSettings s, IReadOnlyList<AiMessage> messages, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, s.Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", s.ApiKey);
        var list = new List<AiMessage> { new() { Role = "system", Content = s.SystemPrompt } };
        list.AddRange(messages);
        var body = new { model = s.Model, messages = list, max_tokens = s.MaxTokens };
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        var txt = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) return $"[{(int)res.StatusCode}] {txt}";
        using var doc = JsonDocument.Parse(txt);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private static async Task<string> OllamaAsync(AiSettings s, IReadOnlyList<AiMessage> messages, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, s.Endpoint);
        var list = new List<AiMessage> { new() { Role = "system", Content = s.SystemPrompt } };
        list.AddRange(messages);
        var body = new { model = s.Model, messages = list, stream = false };
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        var txt = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) return $"[{(int)res.StatusCode}] {txt}";
        using var doc = JsonDocument.Parse(txt);
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}
