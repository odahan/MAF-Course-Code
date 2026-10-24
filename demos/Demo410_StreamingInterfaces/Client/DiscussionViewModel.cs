using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Streaming.Client;

/// <summary>Updates an existing displayed message without replacing the message collection.</summary>
public partial class DisplayMessage(string role) : ObservableObject
{
    /// <summary>Gets the display role.</summary>
    public string Role { get; } = role;
    [ObservableProperty] private string text = "";
}

/// <summary>Owns UI commands and cancellation; the backend owns durable conversation state.</summary>
public partial class DiscussionViewModel(DiscussionClient client) : ObservableObject
{
    [ObservableProperty] private string sessionId = Guid.NewGuid().ToString("N");
    /// <summary>Gets the presentation-only history.</summary>
    public ObservableCollection<DisplayMessage> Messages { get; } = [];
    [ObservableProperty] private string input = "/incident INC-318";
    [ObservableProperty] private string status = "Ready. Successful turns persist on the server.";

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SendAsync(CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(Input)) return;
        var prompt = Input;
        Input = "";
        Messages.Add(new("User") { Text = prompt });
        var answer = new DisplayMessage("Assistant");
        Messages.Add(answer);
        Status = "Generating";
        try
        {
            await client.StreamAsync(new(SessionId, prompt), text => answer.Text += text, token);
            Status = "Completed and saved";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { Status = "Canceled; completion unconfirmed. Partial display only."; }
        catch (Exception error) { Status = $"Failed; completion unconfirmed. {error.Message}"; }
    }

    [RelayCommand]
    private void NewConversation()
    {
        if (SendCommand.IsRunning) return;
        SessionId = Guid.NewGuid().ToString("N");
        Messages.Clear();
        Status = "New conversation. Previous server snapshots remain on disk.";
    }
}


