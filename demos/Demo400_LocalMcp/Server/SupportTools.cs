using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol;
using ModelContextProtocol.Server;

/// <summary>Exposes read-only support fixtures through MCP independently of any model.</summary>
[McpServerToolType]
public static partial class SupportTools
{
    private static readonly Incident[] Incidents =
    [
        new("INC-204", "Atlas Retail", "Open", "High", "PDF export interrupted"),
        new("INC-318", "Cobalt Health", "InProgress", "Critical", "Intermittent connection errors"),
        new("INC-411", "Boreal Industry", "Resolved", "Medium", "Reporting temporarily blocked")
    ];

    /// <summary>Returns the explicitly public priority customer catalogue.</summary>
    [McpServerTool(Name = "list_priority_customers", ReadOnly = true)]
    [Description("Lists priority customers and their service levels. Read-only fixture data.")]
    public static Customer[] ListPriorityCustomers() =>
    [
        new("Atlas Retail", "Platinum", "Sales"),
        new("Boreal Industry", "Gold", "Production"),
        new("Cobalt Health", "Platinum", "IT")
    ];

    /// <summary>Distinguishes a valid missing incident from invalid tool input.</summary>
    [McpServerTool(Name = "get_incident_status", ReadOnly = true)]
    [Description("Gets an incident such as INC-318. Returns Found or NotFound; never invents incidents.")]
    public static IncidentLookup GetIncidentStatus(
        [Description("Incident ID matching INC- followed by three digits.")] string incidentId)
    {
        if (incidentId is null || !IncidentIdPattern().IsMatch(incidentId))
            throw new McpException("Invalid incident ID. Expected INC- followed by three digits.");
        var incident = Incidents.FirstOrDefault(i => i.Id.Equals(incidentId, StringComparison.OrdinalIgnoreCase));
        return new(incident is null ? "NotFound" : "Found", incident);
    }

    [GeneratedRegex("\\AINC-[0-9]{3}\\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IncidentIdPattern();
}

/// <summary>Describes a public customer fixture.</summary>
public sealed record Customer(string Name, string ServiceLevel, string Contact);
/// <summary>Describes an incident fixture.</summary>
public sealed record Incident(string Id, string Customer, string Status, string Impact, string Summary);
/// <summary>Separates missing data from protocol errors.</summary>
public sealed record IncidentLookup(string Status, Incident? Incident);
