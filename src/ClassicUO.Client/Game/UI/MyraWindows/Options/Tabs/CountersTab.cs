using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class CountersTab
{
    internal static IOptionSource GetContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Counters counterLang = lang.GetCounters;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

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
                        else
                            counterGump?.IsEnabled = counterGump.IsVisible = false;

                        counterGump?.SetLayout(
                            profile.CounterBarCellSize,
                            profile.CounterBarRows,
                            profile.CounterBarColumns
                        );
                    }
                ),
                counterLang.EnableCounters
            ),
            GetAbbreviationGroup(),
            GetHighlightGroup(),
            GetLayoutGroup()
        ).WithSearch(new SearchMetadata(counterLang.EnableCounters, Tags: [kw.Counter, kw.Reagent], Keywords: [kw.Counter]));
    }

    private static OptionFragment GetAbbreviationGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Counters counterLang = lang.GetCounters;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.CounterBarDisplayAbbreviatedAmount), counterLang.AbbreviatedValues),
            Option.NumericInput(
                counterLang.AbbreviateIfAmountExceeds,
                new Accessor<int>(() => profile.CounterBarAbbreviatedAmount),
                min: 999,
                max: 999999999,
                search: new SearchMetadata(counterLang.AbbreviateIfAmountExceeds, Keywords: [kw.Abbreviate, kw.Amount, kw.Exceed])
            )
        ).WithSearch(new SearchMetadata(counterLang.EnableCounters, Tags: [kw.Counter], Keywords: [kw.Abbreviate]));
    }

    private static OptionFragment GetHighlightGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Counters counterLang = lang.GetCounters;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = counterLang.SectionHighlightingLabel },
            Option.Checkbox(
                counterLang.HighlightItemsOnUse,
                new Accessor<bool>(() => profile.CounterBarHighlightOnUse),
                search: new SearchMetadata(counterLang.HighlightItemsOnUse, Keywords: [kw.Highlight, kw.Item, kw.Use])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.CounterBarHighlightOnAmount), counterLang.HighlightRedWhenAmountIsLow),
                Option.NumericInput(
                    counterLang.HighlightRedIfAmountIsBelow,
                    new Accessor<int>(() => profile.CounterBarHighlightAmount),
                    min: 1,
                    max: 60000,
                    search: new SearchMetadata(counterLang.HighlightRedIfAmountIsBelow, Keywords: [kw.Highlight, kw.Amount, kw.Below])
                )
            ).WithSearch(new SearchMetadata(counterLang.SectionHighlightingLabel, Tags: [kw.Counter], Keywords: [kw.Highlight]))
        );
    }

    private static OptionFragment GetLayoutGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Counters counterLang = lang.GetCounters;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = counterLang.CounterLayout },
            Option.Slider(
                counterLang.GridSize,
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
                search: new SearchMetadata(counterLang.GridSize, Keywords: [kw.Grid, kw.Size])
            ),
            Option.NumericInput(
                counterLang.Rows,
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
                search: new SearchMetadata(counterLang.Rows, Keywords: [kw.Row])
            ),
            Option.NumericInput(
                counterLang.Columns,
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
                search: new SearchMetadata(counterLang.Columns, Keywords: [kw.Column])
            )
        );
    }
}
