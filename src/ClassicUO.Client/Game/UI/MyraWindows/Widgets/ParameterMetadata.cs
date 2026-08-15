#nullable enable

using System.Reflection;
using ClassicUO.Configuration;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// Reads the name and explanation a property declares for a property grid, so a hand-built editor
/// can label itself exactly as a generated row would.
/// <para>
/// Resolved through <see cref="TazLang" /> rather than off the framework attributes: those report
/// their English fallback as the framework value, so a hand-built row taking it would sit
/// untranslated beside generated rows that were not.
/// </para>
/// </summary>
public static class ParameterMetadata
{
    /// <summary>The property's display name.</summary>
    /// <param name="property">The property to name.</param>
    /// <returns>Its display name, or the property's own name where it declares none.</returns>
    public static string LabelFor(PropertyInfo property)
    {
        LocalizedDisplayNameAttribute? localized = property.GetCustomAttribute<LocalizedDisplayNameAttribute>();

        return localized == null
            ? property.Name
            : TazLang.Get(localized.Key, localized.DisplayName);
    }

    /// <summary>The property's explanation.</summary>
    /// <param name="property">The property to explain.</param>
    /// <returns>Its description, or null where it declares none.</returns>
    public static string? TooltipFor(PropertyInfo property)
    {
        LocalizedDescriptionAttribute? described = property.GetCustomAttribute<LocalizedDescriptionAttribute>();

        return described == null ? null : TazLang.Get(described.Key, described.Description);
    }
}
