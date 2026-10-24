namespace AgentCiCd.Core;

/// <summary>Defines the classifier contract; its validity does not authorize an action.</summary>
public sealed record TicketDecision(string Action, string Category, int RiskScore, string Reason);

/// <summary>Records the host decision after validation and risk policy.</summary>
public sealed record AgentResult(string FinalAction, string Category, int RiskScore, string Reason);
