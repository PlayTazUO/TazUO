global using static ScriptContext;

using ClassicUO.LegionScripting;

/// <summary>
/// Provides the global API instance for script IntelliSense.
/// At runtime, the actual API is injected by TazUO's scripting engine.
/// </summary>
public static class ScriptContext
{
    public static LegionAPI API { get; } = null!;
}