#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.Logic;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.Logic;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Properties;
using Myra.Graphics2D.UI.WrapPanel;
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs.VisualEffects;

/// <summary>
/// Edits one rule: its name, what raises it, that trigger's own parameters, and which look it
/// raises.
/// <para>
/// Works on a draft rather than on the rule itself, which is what lets cancelling leave no trace.
/// The parameters are shown through a <see cref="Myra.Graphics2D.UI.Properties.PropertyGrid"/> over whatever concrete type the
/// chosen trigger declares, so a definition's knobs need no UI of their own.
/// </para>
/// </summary>
internal sealed class OverlayRuleConfigurator : IRuleConfigurator<OverlayRule>
{
    #region Public events

    /// <inheritdoc />
    public event EventHandler<RuleCrudEventArgs<OverlayRule>>? Crud;

    /// <inheritdoc />
    public event EventHandler? EditorClosed;

    #endregion

    #region Private members

    private const int INPUT_WIDTH = 220;

    /// <summary>Width for a numeric field standing beside a wider input, rather than filling a row
    /// on its own. A sound index is four digits and a curve power is one; sized for either.</summary>
    private const int NUMBER_INPUT_WIDTH = 64;

    /// <summary>Fallback for a curve power that cannot be read off its parameters - the same value
    /// <see cref="FalloffCurve.Quadratic" /> is, so an unreadable one behaves like the default curve
    /// rather than like something arbitrary.</summary>
    private const float DEFAULT_FALLOFF_POWER = 2f;

    /// <summary>Reset targets, one per definition. See <see cref="DefaultParametersFor" />.</summary>
    private static readonly Dictionary<string, TriggerParameters> _defaultParameters = [];

    private readonly Panel _root = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Top
    };

    /// <summary>The rule the table holds, or null while creating a new one.</summary>
    private OverlayRule? _target;

    /// <summary>What the editor writes to. The same object as <see cref="_target"/> only when
    /// creating, since a new rule has nothing to roll back to.</summary>
    private OverlayRule _draft = new();

    #endregion

    #region Public methods

    /// <inheritdoc />
    public Widget GetConfiguratorWidget(OverlayRule rule, bool isEdit)
    {
        _target = isEdit ? rule : null;
        _draft = isEdit ? rule.Clone() : Seed(rule);

        Rebuild();

        return _root;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Fills a newly created rule in with something that runs. An empty rule would name no trigger
    /// and no look, which the manager can only skip - and a row that silently does nothing is worse
    /// than one the user has to re-point.
    /// </summary>
    /// <param name="rule">The fresh rule from the rulebase.</param>
    /// <returns>The same rule, seeded.</returns>
    private static OverlayRule Seed(OverlayRule rule)
    {
        ITriggerDefinition? definition = TriggerCatalog.Instance.All.FirstOrDefault();
        EffectProfile? profile = DecorationSettings.Current.Overlays.AllProfiles().FirstOrDefault();

        rule.Name = TazLang.Get("visualeffects_newrule", "New rule");

        if (definition != null)
            rule.Trigger = Bind(definition);

        if (profile != null)
            rule.ProfileId = profile.Id;

        return rule;
    }

    private static TriggerBinding Bind(ITriggerDefinition definition) =>
        new() { DefinitionId = definition.Id, Parameters = definition.CreateDefaultParameters() };

    private void Rebuild()
    {
        List<ITriggerDefinition> definitions = [.. TriggerCatalog.Instance.All];
        List<EffectProfile> profiles = [.. DecorationSettings.Current.Overlays.AllProfiles()];
        ITriggerDefinition? definition = TriggerCatalog.Instance.Find(_draft.Trigger.DefinitionId);

        // Save and cancel sit under the rule's own fields but above the parameter grid. A trigger's
        // parameter table is as long as that trigger needs, and burying the only way out underneath
        // it would mean scrolling to leave.
        StackPanel panel = OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            NameInput(),
            TriggerCombo(definitions, definition),
            ProfileCombo(profiles),
            Buttons()
        );

        MyraGrid? rich = RichParameterRows();

        if (rich != null)
        {
            panel.Widgets.Add(OptionTabCommons.StyledHorizontalSeparator());
            panel.Widgets.Add(rich);
        }

        Widget? parameters = ParameterGrid(definition);

        if (parameters != null)
        {
            panel.Widgets.Add(OptionTabCommons.StyledHorizontalSeparator());
            panel.Widgets.Add(parameters);
        }

        Widget? filter = FilterBuilder();

        if (filter != null)
        {
            panel.Widgets.Add(OptionTabCommons.StyledHorizontalSeparator());
            panel.Widgets.Add(filter);
        }

        _root.Widgets.Clear();
        _root.Widgets.Add(panel);
    }

    /// <summary>
    /// The expression editor, for a trigger whose matching is a tree rather than a fixed set of
    /// fields. Below the grid rather than in it: a bracket nests, grows and shrinks, where a grid row
    /// is one editor beside one label.
    /// </summary>
    /// <returns>The titled builder, or null where the chosen trigger has no expression.</returns>
    private Widget? FilterBuilder()
    {
        if (_draft.Trigger.Parameters is not ILogicFilterParameters filtered)
            return null;

        var builder = new LogicBuilder(filtered.Filter, filtered.FilterSchema);

        // Nothing is persisted here - the whole editor works on a draft, and Save is what commits it.

        return OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            new MyraLabel(TazLang.Get("visualeffects_rulefilter", "Match when"), MyraLabel.TextStyle.H5),
            builder
        );
    }

    private StackPanel NameInput()
    {
        var input = new MyraInputBox { Text = _draft.Name, Width = INPUT_WIDTH };

        input.TextChanged += (_, _) => _draft.Name = input.Text ?? string.Empty;

        return Labelled(TazLang.Get("visualeffects_rulename", "Rule"), input);
    }

    private StackPanel TriggerCombo(List<ITriggerDefinition> definitions, ITriggerDefinition? selected)
    {
        ContainsLevenshteinComboBox combo = SearchableCombo(
            selected?.DisplayName,
            definitions.Select(entry => entry.DisplayName),
            chosen =>
            {
                ITriggerDefinition? definition = definitions.FirstOrDefault(entry => entry.DisplayName == chosen);

                if (definition == null || definition.Id == _draft.Trigger.DefinitionId)
                    return;

                // Parameters belong to the definition that reads them, so switching trigger cannot
                // carry the old ones across - they are a different type entirely.
                _draft.Trigger = Bind(definition);
                Rebuild();
            }
        );

        return Labelled(TazLang.Get("visualeffects_ruletrigger", "Trigger"), combo);
    }

    private StackPanel ProfileCombo(List<EffectProfile> profiles)
    {
        EffectProfile? selected = profiles.FirstOrDefault(profile => profile.Id == _draft.ProfileId);

        ContainsLevenshteinComboBox combo = SearchableCombo(
            selected?.Name,
            profiles.Select(profile => profile.Name),
            chosen =>
            {
                EffectProfile? profile = profiles.FirstOrDefault(entry => entry.Name == chosen);

                if (profile != null)
                    _draft.ProfileId = profile.Id;
            }
        );

        return Labelled(TazLang.Get("visualeffects_ruleeffect", "Effect"), combo);
    }

    /// <summary>
    /// A type-to-filter combo, matching the profile library's. The effect list grows with every look
    /// the user authors and the trigger list with every definition shipped, so both are the kind of
    /// list that stops being scrollable long before it stops being useful.
    /// </summary>
    /// <param name="selected">The name to show, or null where nothing resolves.</param>
    /// <param name="items">The names to offer.</param>
    /// <param name="onChosen">Called with the chosen name.</param>
    /// <returns>The combo.</returns>
    private static ContainsLevenshteinComboBox SearchableCombo(
        string? selected,
        IEnumerable<string> items,
        Action<string> onChosen
    )
    {
        // addSelectedItemIfMissing is off: everything offered comes from the live catalogue or
        // library, so a name that is not in it points at something deleted and must not be
        // re-presented as a valid choice.
        var combo = new ContainsLevenshteinComboBox(
            selected ?? string.Empty,
            items,
            chosen =>
            {
                if (chosen != null)
                    onChosen(chosen);
            },
            addSelectedItemIfMissing: false
        )
        {
            VerticalAlignment = VerticalAlignment.Center,
            TooltipSelector = name => name,
            Width = INPUT_WIDTH
        };

        MyraStyle.ApplySearchComboBoxPopupBorder(combo);

        return combo;
    }

    /// <summary>
    /// The chosen trigger's own knobs, or null where it takes none. Reflected rather than
    /// hand-listed: the parameter types are narrow, so the grid shows exactly the fields that
    /// definition reads and nothing else.
    /// <para>
    /// Styled and given a pristine instance to reset against, so it reads and behaves as the profile
    /// composer's grid does - tooltips from each field's description, reset buttons in the symbol
    /// font, the same spacing.
    /// </para>
    /// </summary>
    /// <param name="definition">The chosen definition.</param>
    /// <returns>The grid, or null.</returns>
    private StyledPropertyGrid? ParameterGrid(ITriggerDefinition? definition)
    {
        if (definition?.ParameterType == null || _draft.Trigger.Parameters == null)
            return null;

        var grid = new StyledPropertyGrid(() => DefaultParametersFor(definition))
        {
            Object = _draft.Trigger.Parameters
        };

        return grid;
    }

    /// <summary>
    /// The parameters that need more than a grid row: a sound to browse and hear, and a curve that
    /// has to explain itself and reveal a second field for one of its choices.
    /// <para>
    /// Built above the property grid rather than as editors inside it. A grid cell is one widget
    /// beside one label in a column sized for a number, so a composite editor put in one is clipped;
    /// and the grid discards and rebuilds every cell whenever a reset is pressed, taking whatever
    /// state such an editor held with it.
    /// </para>
    /// <para>
    /// One two-column grid rather than a row each, so every editor starts at the same x however wide
    /// its label is - a label column that shifts per row reads as a misalignment, not a layout.
    /// </para>
    /// </summary>
    /// <returns>The block, or null where the chosen trigger has no such parameter.</returns>
    private MyraGrid? RichParameterRows()
    {
        TriggerParameters? parameters = _draft.Trigger.Parameters;

        if (parameters == null)
            return null;

        var grid = new MyraGrid();

        grid.AddColumn();
        grid.AddColumn(new Proportion(ProportionType.Fill));

        foreach (PropertyInfo property in parameters.GetType().GetProperties())
        {
            if (property.PropertyType == typeof(int)
                && property.GetCustomAttribute<SoundIndexEditorAttribute>() != null)
            {
                // Centred: the editor is a single row of fields, so its label belongs beside them.
                AddRichRow(grid, property, SoundEditor(parameters, property), VerticalAlignment.Center);
                continue;
            }

            if (property.PropertyType == typeof(FalloffCurve)
                && property.GetCustomAttribute<FalloffEditorAttribute>() is { } falloff)
            {
                // Top: the editor is several stacked rows, and a label centred against all of them
                // would sit beside the explanation rather than beside the list it names.
                AddRichRow(grid, property, FalloffEditor(parameters, property, falloff), VerticalAlignment.Top);
            }
        }

        return grid.Widgets.Count == 0 ? null : grid;
    }

    /// <summary>
    /// Adds one label-and-editor row, the label carrying the parameter's own tooltip so it explains
    /// itself as the grid rows below it do.
    /// </summary>
    /// <param name="grid">The block being filled.</param>
    /// <param name="property">The parameter being edited.</param>
    /// <param name="editor">Its editor.</param>
    /// <param name="labelAlignment">Where the label sits against a multi-row editor.</param>
    private static void AddRichRow(
        MyraGrid grid,
        PropertyInfo property,
        Widget editor,
        VerticalAlignment labelAlignment
    )
    {
        int row = grid.RowsProportions.Count;

        grid.AddRow();

        var label = new MyraLabel(ParameterMetadata.LabelFor(property), MyraLabel.TextStyle.P)
        {
            VerticalAlignment = labelAlignment,
            Tooltip = ParameterMetadata.TooltipFor(property)
        };

        grid.AddWidget(label, row, 0);
        grid.AddWidget(editor, row, 1);
    }

    private static SoundIndexPicker SoundEditor(TriggerParameters parameters, PropertyInfo property)
    {
        var picker = new SoundIndexPicker(
            property.GetValue(parameters) is int stored ? stored : 0,
            NUMBER_INPUT_WIDTH,
            INPUT_WIDTH
        );

        picker.IndexChanged += (_, index) => property.SetValue(parameters, index);

        return picker;
    }

    /// <summary>
    /// The distance-response editor, which owns four properties rather than one: the curve, the
    /// power its custom form uses, and the strengths at either end of the range. The last three are
    /// kept out of the property grid precisely so this can drop the ones the chosen curve gives no
    /// meaning to.
    /// </summary>
    /// <param name="parameters">The parameters being edited.</param>
    /// <param name="property">The curve property.</param>
    /// <param name="falloff">Names the siblings that go with it.</param>
    /// <returns>The editor.</returns>
    private static FalloffPicker FalloffEditor(
        TriggerParameters parameters,
        PropertyInfo property,
        FalloffEditorAttribute falloff
    )
    {
        Type owner = parameters.GetType();

        var properties = new FalloffProperties(
            property,
            owner.GetProperty(falloff.PowerProperty),
            owner.GetProperty(falloff.NearStrengthProperty),
            owner.GetProperty(falloff.FarStrengthProperty)
        );

        return new FalloffPicker(parameters, properties, INPUT_WIDTH, NUMBER_INPUT_WIDTH);
    }

    /// <summary>
    /// An untouched parameter object for the chosen definition, cached per definition because every
    /// reset button reads one through reflection and a definition's defaults never change within a
    /// session.
    /// </summary>
    /// <param name="definition">The definition to ask.</param>
    /// <returns>The pristine parameters, or null if it takes none.</returns>
    private static TriggerParameters? DefaultParametersFor(ITriggerDefinition definition)
    {
        if (_defaultParameters.TryGetValue(definition.Id, out TriggerParameters? pristine))
            return pristine;

        pristine = definition.CreateDefaultParameters();

        if (pristine != null)
            _defaultParameters[definition.Id] = pristine;

        return pristine;
    }

    private WrapPanel Buttons() =>
        OptionTabCommons.StyledHorizontalWrapPanel(
            new MyraButton(TazLang.Get("profileeditor_save", "Save"), Save),
            new MyraButton(TazLang.Get("profileeditor_cancel", "Cancel"), Cancel)
        );

    private void Save()
    {
        if (_target == null)
        {
            Crud?.Invoke(this, new RuleCrudEventArgs<OverlayRule>(_draft, RuleCrudEventType.Create));
            return;
        }

        _target.ApplyFrom(_draft);
        Crud?.Invoke(this, new RuleCrudEventArgs<OverlayRule>(_target, RuleCrudEventType.Update));
    }

    private void Cancel() => EditorClosed?.Invoke(this, EventArgs.Empty);

    private static StackPanel Labelled(string label, Widget content) =>
        OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            new MyraLabel(label, MyraLabel.TextStyle.P) { VerticalAlignment = VerticalAlignment.Center },
            content
        );

    #endregion
}
