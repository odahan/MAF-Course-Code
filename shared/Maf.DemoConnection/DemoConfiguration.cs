using Microsoft.Extensions.Configuration;

namespace Maf.DemoConnection;

/// <summary>Loads central settings independently of the executable's working directory.</summary>
public static class DemoConfiguration
{
    /// <summary>Finds the explicit configuration or the shared file above the executable.</summary>
    public static string? FindCentralFile(string? startDirectory = null)
    {
        var explicitPath = Environment.GetEnvironmentVariable("MAF_DEMO_CONFIG");
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            if (!Path.IsPathFullyQualified(explicitPath) || !File.Exists(explicitPath))
                throw new ArgumentException("MAF_DEMO_CONFIG must identify an existing absolute configuration file.");
            return Path.GetFullPath(explicitPath);
        }
        for (var directory = new DirectoryInfo(startDirectory ?? AppContext.BaseDirectory);
             directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "shared", "Configuration", "ai-settings.json");
            if (File.Exists(Path.Combine(directory.FullName, "BaseTechnique.slnx")) && File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    /// <summary>Adds binary defaults, central settings, environment and command-line overrides, in that order.</summary>
    public static IConfigurationBuilder AddDemoConfiguration(this IConfigurationBuilder builder, string[] args,
        string? baseDirectory = null, string? centralFile = null)
    {
        builder.AddJsonFile(Path.Combine(baseDirectory ?? AppContext.BaseDirectory, "ai-settings.json"), optional: true);
        var shared = centralFile ?? FindCentralFile(baseDirectory);
        if (shared is not null) builder.AddJsonFile(Path.GetFullPath(shared), optional: false);
        return builder.AddEnvironmentVariables().AddCommandLine(args);
    }

    /// <summary>Builds the effective configuration, including non-AI settings.</summary>
    public static ConfigurationRoot Load(string[]? args = null) =>
        (ConfigurationRoot)new ConfigurationBuilder().AddDemoConfiguration(args ?? []).Build();

    /// <summary>Reads a setting previously exposed only as an environment variable.</summary>
    public static string? GetSetting(string key)
    {
        using var configuration = Load();
        return configuration[key];
    }

    /// <summary>Reads a process secret, falling back to the Windows user environment for saved configurator keys.</summary>
    public static string? ReadSecret(string variable) => Environment.GetEnvironmentVariable(variable)
        ?? (OperatingSystem.IsWindows() ? Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User) : null);
}
