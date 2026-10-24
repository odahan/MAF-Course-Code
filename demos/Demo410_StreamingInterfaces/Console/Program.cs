using Streaming.Client;

using var http = new HttpClient
{
    BaseAddress = new Uri(global::Maf.DemoConnection.DemoConfiguration.GetSetting("MAF_BACKEND_URL") ?? "http://localhost:5088"),
    Timeout = Timeout.InfiniteTimeSpan
};
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
var session = args.Length > 0 ? args[0] : Guid.NewGuid().ToString("N");
Console.WriteLine($"Session: {session}. Reuse this ID to resume. Ctrl+C cancels.");
try
{
    var prompt = args.Length > 1 ? args[1] : "/incident INC-318";
    await new DiscussionClient(http).StreamAsync(new(session, prompt), Console.Write, stop.Token);
    Console.WriteLine("\nCompleted and saved.");
    return 0;
}
catch (OperationCanceledException) when (stop.IsCancellationRequested) { Console.Error.WriteLine("Canceled."); return 130; }
catch (Exception error) { Console.Error.WriteLine($"Failed: {error.Message}"); return 2; }
