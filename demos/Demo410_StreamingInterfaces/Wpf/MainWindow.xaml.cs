using System.Net.Http;
using System.Windows;
using Streaming.Client;

namespace ClientWpf;

/// <summary>Composes the desktop view and releases network resources on close.</summary>
public partial class MainWindow : Window
{
    private readonly HttpClient http = new()
    {
        BaseAddress = new Uri(global::Maf.DemoConnection.DemoConfiguration.GetSetting("MAF_BACKEND_URL") ?? "http://localhost:5088"),
        Timeout = Timeout.InfiniteTimeSpan
    };

    /// <summary>Connects bindings to the Toolkit view model.</summary>
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new DiscussionViewModel(new DiscussionClient(http));
        DataContext = viewModel;
        Closed += (_, _) => { viewModel.SendCommand.Cancel(); http.Dispose(); };
    }
}
