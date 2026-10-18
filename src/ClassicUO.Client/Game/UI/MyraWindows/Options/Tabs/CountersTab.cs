using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for the counter-bar feature settings</summary>
public static class CountersTab
{
    /// <summary>Returns the option fragment for counter-bar enable/disable and display configuration</summary>
    internal static IOptionSource GetContent()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(
                new Accessor<bool>(
                    () => profile.CounterBarEnabled,
                    b =>
                    {
                        profile.CounterBarEnabled = b;
                        CounterBarGump counterGump = UIManager.GetGump<CounterBarGump>();

                        if (b)
                        {
                            if (counterGump != null)
                                counterGump.IsEnabled = counterGump.IsVisible = true;
                            else
                                UIManager.Add(counterGump = new CounterBarGump(World.Instance, 200, 200));
                        }
                        else if (counterGump != null)
                        {
                            counterGump.IsEnabled = false;
                            counterGump.IsVisible = false;
                        }

                        counterGump?.SetLayout(
                            profile.CounterBarCellSize,
                            profile.CounterBarRows,
                            profile.CounterBarColumns
                        );
                    }
                ),
                TazLang.Get("mog_counters_enablecounters")
            ),
            Option.Checkbox(
                TazLang.Get("mog_counters_showhotkeys"),
                new Accessor<bool>(() => profile.CounterBarShowHotkeys, b =>
                {
                    profile.CounterBarShowHotkeys = b;
                    UIManager.GetGump<CounterBarGump>()?.RefreshHotkeyLabels();
                }),
                search: new SearchMetadata(TazLang.Get("mog_counters_showhotkeys"), Keywords: [TazLang.Get("mog_kw_counter"), TazLang.Get("mog_kw_hotkey")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_counters_disableitemscaling"),
                new Accessor<bool>(() => profile.CounterBarDisableItemScaling, b => profile.CounterBarDisableItemScaling = b),
                search: new SearchMetadata(TazLang.Get("mog_counters_disableitemscaling"), Keywords: [TazLang.Get("mog_kw_counter"), TazLang.Get("mog_kw_item"), TazLang.Get("mog_kw_scaling")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_counters_disableiconscaling"),
                new Accessor<bool>(() => profile.CounterBarDisableIconScaling, b => profile.CounterBarDisableIconScaling = b),
                search: new SearchMetadata(TazLang.Get("mog_counters_disableiconscaling"), Keywords: [TazLang.Get("mog_kw_counter"), TazLang.Get("mog_kw_icon"), TazLang.Get("mog_kw_spell"), TazLang.Get("mog_kw_scaling")])
            ),
            GetAbbreviationGroup(),
            GetHighlightGroup(),
            GetLayoutGroup(),
            Option.Button(
                TazLang.Get("mog_counters_converttoactionbar", "Convert to Action Bar"),
                ConvertToActionBar,
                search: new SearchMetadata(
                    TazLang.Get("mog_counters_converttoactionbar", "Convert to Action Bar"),
                    Keywords: [TazLang.Get("mog_kw_counter"), TazLang.Get("mog_kw_actionbar")]
                )
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_counters_enablecounters"), Tags: [TazLang.Get("mog_kw_counter"), TazLang.Get("mog_kw_reagent")], Keywords: [TazLang.Get("mog_kw_counter")]));
    }

    /// <summary>
    /// Replaces the single counter bar with a named action bar, carrying over its layout, every cell
    /// (action or item counter) and its hotkeys, then removes the counter bar so only one remains.
    /// </summary>
    private static void ConvertToActionBar()
    {
        CounterBarGump counter = UIManager.GetGump<CounterBarGump>();

        if (counter == null)
            return;

        Profile profile = ProfileManager.CurrentProfile;

        int rows = counter.Rows;
        int columns = counter.Columns;

        var bar = new ActionBarGump(
            World.Instance,
            counter.X,
            counter.Y,
            counter.RectSize,
            rows,
            columns,
            TazLang.Get("actionbar_defaultname", "Action Bar")
        );

        int cells = rows * columns;

        for (int i = 0; i < cells; i++)
        {
            CounterBarGump.CounterItem source = counter.GetCounterItem(i);
            ActionBarGump.ActionItem target = bar.GetActionItem(i);

            if (source == null || target == null)
                continue;

            if (source.HasAction)
                target.SetSlot(source.Slot);
            else
                target.SetGraphic(source.Graphic, source.Hue);

            var binding = counter.GetCellHotkey(i);
            if (binding is { IsEmpty: false })
                bar.SetCellHotkey(i, binding);
        }

        bar.RefreshHotkeyLabels();
        UIManager.Add(bar);

        profile.CounterBarEnabled = false;
        counter.Dispose();
    }

    private static OptionFragment GetAbbreviationGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.CounterBarDisplayAbbreviatedAmount), TazLang.Get("mog_counters_abbreviatedvalues")),
            Option.IntegerInput(
                TazLang.Get("mog_counters_abbreviateifamountexceeds"),
                new Accessor<int>(() => profile.CounterBarAbbreviatedAmount),
                min: 999,
                max: 999999999,
                search: new SearchMetadata(TazLang.Get("mog_counters_abbreviateifamountexceeds"), Keywords: [TazLang.Get("mog_kw_abbreviate"), TazLang.Get("mog_kw_amount"), TazLang.Get("mog_kw_exceed")])
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_counters_enablecounters"), Tags: [TazLang.Get("mog_kw_counter")], Keywords: [TazLang.Get("mog_kw_abbreviate")]));
    }

    private static OptionFragment GetHighlightGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_counters_sectionhighlightinglabel") },
            Option.Checkbox(
                TazLang.Get("mog_counters_highlightitemsonuse"),
                new Accessor<bool>(() => profile.CounterBarHighlightOnUse),
                search: new SearchMetadata(TazLang.Get("mog_counters_highlightitemsonuse"), Keywords: [TazLang.Get("mog_kw_highlight"), TazLang.Get("mog_kw_item"), TazLang.Get("mog_kw_use")])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.CounterBarHighlightOnAmount), TazLang.Get("mog_counters_highlightredwhenamountislow")),
                Option.IntegerInput(
                    TazLang.Get("mog_counters_highlightredifamountisbelow"),
                    new Accessor<int>(() => profile.CounterBarHighlightAmount),
                    min: 1,
                    max: 60000,
                    search: new SearchMetadata(TazLang.Get("mog_counters_highlightredifamountisbelow"), Keywords: [TazLang.Get("mog_kw_highlight"), TazLang.Get("mog_kw_amount"), TazLang.Get("mog_kw_below")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_counters_sectionhighlightinglabel"), Tags: [TazLang.Get("mog_kw_counter")], Keywords: [TazLang.Get("mog_kw_highlight")]))
        );
    }

    private static OptionFragment GetLayoutGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_counters_counterlayout") },
            Option.Slider(
                TazLang.Get("mog_counters_gridsize"),
                30,
                80,
                new Accessor<float>(() => profile.CounterBarCellSize, v =>
                {
                    profile.CounterBarCellSize = (int)v;
                    UIManager.GetGump<CounterBarGump>()
                        ?.SetLayout(
                            profile.CounterBarCellSize,
                            profile.CounterBarRows,
                            profile.CounterBarColumns
                        );
                }),
                search: new SearchMetadata(TazLang.Get("mog_counters_gridsize"), Keywords: [TazLang.Get("mog_kw_grid"), TazLang.Get("mog_kw_size")])
            ),
            Option.IntegerInput(
                TazLang.Get("mog_counters_rows"),
                new Accessor<int>(() => profile.CounterBarRows, v =>
                {
                    profile.CounterBarRows = v;
                    UIManager.GetGump<CounterBarGump>()
                        ?.SetLayout(
                            profile.CounterBarCellSize,
                            profile.CounterBarRows,
                            profile.CounterBarColumns
                        );
                }),
                min: 1,
                max: 30,
                search: new SearchMetadata(TazLang.Get("mog_counters_rows"), Keywords: [TazLang.Get("mog_kw_row")])
            ),
            Option.IntegerInput(
                TazLang.Get("mog_counters_columns"),
                new Accessor<int>(() => profile.CounterBarColumns, v =>
                {
                    profile.CounterBarColumns = v;
                    UIManager.GetGump<CounterBarGump>()
                        ?.SetLayout(
                            profile.CounterBarCellSize,
                            profile.CounterBarRows,
                            profile.CounterBarColumns
                        );
                }),
                min: 1,
                max: 30,
                search: new SearchMetadata(TazLang.Get("mog_counters_columns"), Keywords: [TazLang.Get("mog_kw_column")])
            )
        );
    }
}
