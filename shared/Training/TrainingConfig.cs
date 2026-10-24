using Maf.DemoConnection;
using Microsoft.Extensions.AI;

namespace MafDay2.Common;

/// <summary>Routes the retained planning variants through the same central connection configuration as other demos.</summary>
internal static class TrainingConfig
{
    /// <summary>Creates an owned chat client using binary defaults, central settings, environment and CLI overrides.</summary>
    public static IChatClient CreateChatClient(string[] args)
    {
        using var configuration = DemoConfiguration.Load(args);
        return DemoConnection.CreateChatClient(ConnectionOptions.FromConfiguration(configuration));
    }
}

