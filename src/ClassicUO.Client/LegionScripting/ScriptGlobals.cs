using System;
using System.Threading;

namespace ClassicUO.LegionScripting;

public static class CsLegionApiHost
{
    /// <summary>
    /// A thread/task local Legion API instance, accessible for scripts via the API field
    /// </summary>
    public static AsyncLocal<LegionAPI> Current { get; } = new();

    /// <summary>
    /// Provides access to the thread/task local Legion API instance
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static LegionAPI API
    {
        get
        {
            return Current.Value?? throw new InvalidOperationException($"{nameof(CsLegionApiHost)}.API accessed outside of script execution.");
        }
    }
}
