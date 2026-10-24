# Solution Exercice (Lab04) — Solution Exercice 3 (structure)

Ajouter un tool :

```csharp
public static async Task<bool> SetStatusAsync(long id, string status)
{
    // 1) normalisation + whitelist status (New/Triaged/Planned)
    // 2) approval (o/n)
    // 3) UPDATE BacklogItems SET Status=$status WHERE Id=$id
    // 4) PrintLastItems()
    // return true/false
}
```

Puis l’enregistrer via `AIFunctionFactory.Create(...)`.