namespace Episode011.ModeDegrade;

/// <summary>Defines reproducible failure scenarios independently of provider health.</summary>
public enum ScenarioDemo
{
    Nominal = 1,
    TimeoutSimule = 2,
    OllamaIndisponibleSimule = 3,
    SortieInvalideSimulee = 4,
    FallbackDirect = 5
}
