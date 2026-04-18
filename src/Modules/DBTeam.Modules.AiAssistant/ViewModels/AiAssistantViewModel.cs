using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Modules.AiAssistant.Engine;

namespace DBTeam.Modules.AiAssistant.ViewModels;

public partial class ChatTurn : ObservableObject
{
    [ObservableProperty] private string role = "user";
    [ObservableProperty] private string content = "";
    public string Avatar => Role == "user" ? "AccountCircle" : "Robot";
    public string HeaderColor => Role == "user" ? "#1E88E5" : "#2E7D32";
}

public partial class AiAssistantViewModel : ObservableObject
{
    private readonly AiSettingsStore _store;
    private readonly AiClient _client = new();

    public AiAssistantViewModel(AiSettingsStore store)
    {
        _store = store;
        Settings = _store.Load();
        ProviderChoices = new ObservableCollection<AiProvider>((AiProvider[])Enum.GetValues(typeof(AiProvider)));
        QuickPrompts = new ObservableCollection<string>
        {
            "Explain this SQL query step by step:",
            "Suggest an index that would speed up this query:",
            "Optimize this JOIN:",
            "Convert this query to use a CTE:",
            "Write a T-SQL stored procedure that…",
            "Generate sample INSERT statements for a table with these columns:",
            "Find potential SQL injection risks in this query:",
            "Explain what this execution plan XML tells us:"
        };
    }

    public ObservableCollection<ChatTurn> Turns { get; } = new();
    public ObservableCollection<AiProvider> ProviderChoices { get; }
    public ObservableCollection<string> QuickPrompts { get; }

    [ObservableProperty] private AiSettings settings = new();
    [ObservableProperty] private string currentInput = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string status = "Ready";

    [RelayCommand]
    public void SaveSettings()
    {
        _store.Save(Settings);
        Status = "Settings saved";
    }

    [RelayCommand]
    public void UseProviderPreset(string? preset)
    {
        Settings = preset switch
        {
            "Anthropic" => new AiSettings { Provider = AiProvider.Anthropic, Endpoint = "https://api.anthropic.com/v1/messages", Model = "claude-sonnet-4-6", ApiKey = Settings.ApiKey, SystemPrompt = Settings.SystemPrompt },
            "OpenAI" => new AiSettings { Provider = AiProvider.OpenAI, Endpoint = "https://api.openai.com/v1/chat/completions", Model = "gpt-4o-mini", ApiKey = Settings.ApiKey, SystemPrompt = Settings.SystemPrompt },
            "Ollama" => new AiSettings { Provider = AiProvider.Ollama, Endpoint = "http://localhost:11434/api/chat", Model = "llama3.2", ApiKey = "", SystemPrompt = Settings.SystemPrompt },
            _ => Settings
        };
    }

    [RelayCommand]
    public async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentInput)) return;
        IsBusy = true; Status = "Thinking...";
        Turns.Add(new ChatTurn { Role = "user", Content = CurrentInput });
        var userMsg = CurrentInput; CurrentInput = "";

        try
        {
            var msgs = new System.Collections.Generic.List<AiMessage>();
            foreach (var t in Turns) msgs.Add(new AiMessage { Role = t.Role, Content = t.Content });
            var reply = await _client.ChatAsync(Settings, msgs);
            Turns.Add(new ChatTurn { Role = "assistant", Content = reply });
            Status = $"Turn {Turns.Count / 2}";
        }
        catch (Exception ex)
        {
            Turns.Add(new ChatTurn { Role = "assistant", Content = $"[error] {ex.Message}" });
            Status = "Error";
        }
        IsBusy = false;
    }

    [RelayCommand]
    public void Clear() { Turns.Clear(); Status = "Cleared"; }

    [RelayCommand]
    public void UseQuickPrompt(string? prompt)
    {
        if (!string.IsNullOrEmpty(prompt)) CurrentInput = prompt + "\n\n";
    }
}
