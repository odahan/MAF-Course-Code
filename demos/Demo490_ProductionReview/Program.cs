Console.WriteLine("Production review checklist - educational scope");
Console.WriteLine("A checklist does not implement or certify these controls.");
foreach (string question in new[]
{
    "Is the business contract validated by host code?",
    "Does the host bind approval to the exact proposal?",
    "Are evidence eligibility and citations checked before action?",
    "Does a restart preserve the pending decision and prevent duplicate effects?",
    "Are permissions, budgets, cancellation and failure paths exercised?",
    "Are safe traces distinct from committed business records?",
    "Is the exact model/provider profile qualified on the intended workload?",
    "Have authentication, deployment, backups and operational recovery been designed?"
})
    Console.WriteLine("Review: " + question);
Console.WriteLine("Start with docs/lot-015/Guide.md and AuditClosure.md in the canonical distribution.");
