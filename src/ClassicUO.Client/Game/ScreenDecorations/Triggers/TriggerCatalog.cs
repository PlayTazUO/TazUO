#nullable enable

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.ScreenDecorations.Triggers.Definitions;

namespace ClassicUO.Game.ScreenDecorations.Triggers;

/// <summary>
/// Every trigger definition the client ships. Code-defined and fixed at startup; this is what the
/// rule editor offers in its trigger dropdown, and the only way a persisted binding reaches one.
/// </summary>
public sealed class TriggerCatalog
{
    #region Public accessors

    public static TriggerCatalog Instance { get; } = new();

    /// <summary>The definitions, in the order the rule editor should offer them.</summary>
    public IReadOnlyList<ITriggerDefinition> All { get; }

    #endregion

    #region Private members

    private readonly FrozenDictionary<string, ITriggerDefinition> _byId;

    #endregion

    #region Ctor

    private TriggerCatalog()
    {
        // Add shipped definitions here. Nothing else knows the set: a rule reaches one only through
        // Find, by the id it persisted.
        All =
        [
            new SoundPlayedTriggerDefinition(),
            new ChatMessageTriggerDefinition(),
            new ObjectPropertiesTriggerDefinition(),
            new PlayerAttributeTriggerDefinition()
        ];

        _byId = All.ToFrozenDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// The definition a binding names.
    /// </summary>
    /// <param name="definitionId">The persisted <see cref="ITriggerDefinition.Id" />.</param>
    /// <returns>The definition, or null for an id this build does not know - a rule written by a
    /// newer client, or one whose trigger has since been withdrawn.</returns>
    public ITriggerDefinition? Find(string? definitionId) =>
        definitionId != null && _byId.TryGetValue(definitionId, out ITriggerDefinition? definition) ? definition : null;

    #endregion
}
