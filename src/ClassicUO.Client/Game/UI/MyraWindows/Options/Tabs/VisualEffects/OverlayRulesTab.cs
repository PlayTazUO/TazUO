#nullable enable

using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility.Collections;
using Myra.Graphics2D.UI;
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs.VisualEffects;

/// <summary>
/// The rulebase: which trigger raises which look. Rows are edited through the configurator rather
/// than inline, so a rename or a re-pointing is one saved decision instead of a save per keystroke;
/// only the enabled switch is a single click in the table.
/// </summary>
internal static class OverlayRulesTab
{
    #region Private members

    /// <summary>Heavy check mark, U+2714. Present in Noto Sans Symbols 2, absent from the body font.</summary>
    private const string BUILT_IN_GLYPH = "✔";

    private const int BUILT_IN_GLYPH_SIZE = 16;

    #endregion

    #region Internal methods

    /// <summary>Returns the rulebase editor as an option source.</summary>
    /// <returns>The option source.</returns>
    internal static IOptionSource GetContent()
    {
        OptionFragment panel = OptionsUi.Vertical(Option.Custom(BuildRulebase));

        // A table with its own toolbar does not render meaningfully in the search results page.
        panel.InheritsSearch = false;

        return panel;
    }

    #endregion

    #region Private methods

    private static Widget BuildRulebase()
    {
        OverlaySystemSettings overlays = DecorationSettings.Current.Overlays;

        var rulebase = new Rulebase<OverlayRule>(new OverlayRuleConfigurator())
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,

            // Position is precedence here - first match claims the effect - so a rule the user has
            // just authored belongs where it will actually be reached.
            AddNewRulesFirst = true,
            TitleLabel =
            {
                Text = TazLang.Get("visualeffects_rules", "Rules"),
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        rulebase.Columns.AddRange(BuildColumns(overlays, rulebase));

        foreach (OverlayRule rule in overlays.ResolveRules())
            rulebase.Rules.Add(rule);

        rulebase.RuleCrud += (_, args) => OnRuleCrud(overlays, rulebase, args);
        rulebase.Reordered += (_, _) => PersistOrder(overlays, rulebase.Rules);

        return rulebase;
    }

    private static RulebaseColumn<OverlayRule>[] BuildColumns(OverlaySystemSettings overlays, Rulebase<OverlayRule> rulebase) =>
    [
        new RulebaseColumn<OverlayRule>
        {
            Header = TazLang.Get("visualeffects_rulebuiltin", "Built-in"),
            HeaderTooltip = TazLang.Get(
                "visualeffects_rulebuiltintooltip",
                "Built in to the client. Can be switched off and reordered,\n"
                + "but not edited or deleted - copy one to customise it."
            ),
            CellContentAlignment = HorizontalAlignment.Center,
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = rule => BuiltInMark(rule.IsBuiltIn)
        },
        new RulebaseColumn<OverlayRule>
        {
            Header = TazLang.Get("visualeffects_rulename", "Rule"),
            HeaderTooltip = TazLang.Get(
                "visualeffects_rulenametooltip",
                "What you called this rule. Names are yours alone -\n"
                + "nothing refers to a rule by one."
            ),
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = rule => Text(rule.Name)
        },
        new RulebaseColumn<OverlayRule>
        {
            Header = TazLang.Get("visualeffects_ruletrigger", "Trigger"),
            HeaderTooltip = TazLang.Get("visualeffects_ruletriggertooltip", "What raises this rule."),
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = rule => Text(TriggerName(rule))
        },
        new RulebaseColumn<OverlayRule>
        {
            Header = TazLang.Get("visualeffects_ruletype", "Type"),
            HeaderTooltip = TazLang.Get(
                "visualeffects_ruletypetooltip",
                "Poll: sampled a few times a second.\n"
                + "Event: costs nothing until it fires."
            ),
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = rule => Text(TriggerKindName(rule))
        },
        new RulebaseColumn<OverlayRule>
        {
            Header = TazLang.Get("visualeffects_ruleeffect", "Effect"),
            HeaderTooltip = TazLang.Get("visualeffects_ruleeffecttooltip", "The look this rule raises."),
            Proportion = new Proportion(ProportionType.Fill),
            CellFactory = rule => Text(overlays.FindProfile(rule.ProfileId)?.Name ?? TazLang.Get("visualeffects_rulemissingprofile", "(missing)"))
        },
        new RulebaseColumn<OverlayRule>
        {
            Header = TazLang.Get("visualeffects_ruleenabled", "Enabled"),
            HeaderTooltip = TazLang.Get(
                "visualeffects_ruleenabledtooltip",
                "Whether this rule is watched at all. A switched-off rule costs\n"
                + "nothing and keeps everything it was configured with."
            ),
            CellContentAlignment = HorizontalAlignment.Center,
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = rule => MyraCheckButton.CreateWithCallback(
                rule.Enabled,
                enabled =>
                {
                    rule.Enabled = enabled;
                    PersistOrder(overlays, rulebase.Rules);
                }
            )
        }
    ];

    private static MyraLabel Text(string value) =>
        new(value, MyraLabel.TextStyle.P) { VerticalAlignment = VerticalAlignment.Center };

    /// <summary>
    /// Tick or nothing. Drawn from the symbol font rather than the body one, which has no glyph for
    /// it and would render the cell blank either way.
    /// </summary>
    /// <param name="isBuiltIn">Whether the rule is shipped.</param>
    /// <returns>The cell content.</returns>
    private static MyraLabel BuiltInMark(bool isBuiltIn) =>
        new(isBuiltIn ? BUILT_IN_GLYPH : string.Empty, BUILT_IN_GLYPH_SIZE)
        {
            Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, BUILT_IN_GLYPH_SIZE),
            Wrap = false,
            SingleLine = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

    private static string TriggerName(OverlayRule rule) =>
        TriggerCatalog.Instance.Find(rule.Trigger.DefinitionId)?.DisplayName
        ?? TazLang.Get("visualeffects_ruleunknowntrigger", "(unknown)");

    private static string TriggerKindName(OverlayRule rule)
    {
        ITriggerDefinition? definition = TriggerCatalog.Instance.Find(rule.Trigger.DefinitionId);

        if (definition == null)
            return string.Empty;

        return definition.Kind == TriggerKind.Poll
            ? TazLang.Get("visualeffects_triggerkindpoll", "Poll")
            : TazLang.Get("visualeffects_triggerkindevent", "Event");
    }

    private static void OnRuleCrud(
        OverlaySystemSettings overlays,
        Rulebase<OverlayRule> rulebase,
        RuleCrudEventArgs<OverlayRule> args
    )
    {
        switch (args.Event)
        {
            case RuleCrudEventType.Create:
                overlays.Rules.Add(args.Rule);
                break;

            case RuleCrudEventType.Delete:
                overlays.Rules.Remove(args.Rule);
                break;
        }

        // Cells are built once and cached, and the rulebase only re-renders unforced after an edit -
        // so a renamed or re-pointed rule would keep showing what it was called when the row was
        // built. Forced, every column re-reads the rule it belongs to.
        rulebase.RefreshTable(true);

        // Adding and deleting both re-stamp every row's position, built-ins included, so the table's
        // order has to be written back here as well as after an explicit move.
        PersistOrder(overlays, rulebase.Rules);
    }

    /// <summary>
    /// Writes the table's positions back. Built-in rules keep only their position and enabled state;
    /// the user's own hold theirs directly, so the list order is already stored by the time this
    /// runs.
    /// </summary>
    /// <param name="overlays">The settings to record into.</param>
    /// <param name="rules">The rules as the table now shows them.</param>
    private static void PersistOrder(OverlaySystemSettings overlays, IEnumerable<OverlayRule> rules)
    {
        foreach (OverlayRule rule in rules.Where(rule => rule.IsBuiltIn))
            overlays.TrackRuleState(rule);

        Commit();
    }

    /// <summary>Persists the rulebase and rewires the manager against it.</summary>
    private static void Commit()
    {
        DecorationSettings.Current.Save();
        ScreenOverlayManager.Instance.RulesChanged();
    }

    #endregion
}
