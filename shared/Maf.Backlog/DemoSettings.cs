using Maf.DemoConnection;
using Microsoft.Extensions.Configuration;

namespace Maf.Backlog;

/// <summary>Loads the common connection configuration for the fundamental console examples.</summary>
public static class DemoSettings
{
    /// <summary>Loads the distributed profile followed by environment overrides and explicit CLI options.</summary>
    public static ConnectionOptions Load(string[] args) => ConnectionOptions.FromConfiguration(
        new ConfigurationBuilder().AddDemoConfiguration(args).Build());
}
