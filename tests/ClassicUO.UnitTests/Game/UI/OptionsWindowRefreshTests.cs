using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Myra.Graphics2D.UI.WrapPanel;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

[CollectionDefinition("Options widgets", DisableParallelization = true)]
public class OptionsWidgetsCollection;

[Collection("Options widgets")]
public class OptionsWindowRefreshTests : IDisposable
{
    private static readonly FieldInfo CurrentStylesheet = typeof(Stylesheet)
        .GetField("_current", BindingFlags.Static | BindingFlags.NonPublic);
    private readonly object _previousStylesheet = CurrentStylesheet.GetValue(null);

    public OptionsWindowRefreshTests()
    {
        // Exercise real tab widgets without loading a graphics device or game fonts.
        Stylesheet.Current = new Stylesheet
        {
            LabelStyle = new LabelStyle(),
            ButtonStyle = new ButtonStyle { LabelStyle = new LabelStyle() },
            TextBoxStyle = new TextBoxStyle(),
            TabControlStyle = new TabControlStyle
            {
                TabItemStyle = new ImageTextButtonStyle { LabelStyle = new LabelStyle() },
                ContentStyle = new WidgetStyle()
            }
        };
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void PresetRefreshKeepsNestedTabsSelectedAndUpdatesTheirValues(int selectedSubTab)
    {
        var profile = new Profile { NamePlateOpacity = 23 };
        var source = new OptionTabGroup()
            .AddTab("Containers", () => new OptionEntry(() => new Widget()))
            .AddTab("Nameplates", () => new OptionTabGroup()
                .AddTab("General", () => new OptionEntry(() => new Widget { Tag = profile.NamePlateOpacity }))
                .AddTab("Profiles", () => new OptionEntry(() => new Widget { Tag = profile.NamePlateOpacity })));
        var tabs = (MyraTabControl)source.Render();
        tabs.SelectedIndex = 1;
        var nameplates = (MyraTabControl)tabs.SelectedItem.Content;
        nameplates.SelectedIndex = selectedSubTab;
        Widget previousContent = nameplates.SelectedItem.Content;
        var panel = new WrapPanel();
        panel.Widgets.Add(tabs);
        OptionsWindow window = CreateWindow(source, panel);

        NamePlatePresets.Apply(profile, NamePlatePreset.Legacy);
        window.RefreshCurrentContent();

        Assert.Same(tabs, Assert.Single(panel.Widgets));
        Assert.Equal(1, tabs.SelectedIndex);
        Assert.Same(nameplates, tabs.SelectedItem.Content);
        Assert.Equal(selectedSubTab, nameplates.SelectedIndex);
        Assert.NotSame(previousContent, nameplates.SelectedItem.Content);
        Assert.Equal((byte)75, nameplates.SelectedItem.Content.Tag);

        // Saving or selecting another preset uses the same refresh path.
        NamePlatePresets.Apply(profile, NamePlatePreset.Orion);
        window.RefreshCurrentContent();
        Assert.Equal(1, tabs.SelectedIndex);
        Assert.Equal(selectedSubTab, nameplates.SelectedIndex);
        Assert.Equal((byte)70, nameplates.SelectedItem.Content.Tag);
    }

    [Fact]
    public void RefreshAlsoUpdatesCategoriesWithoutTabs()
    {
        int value = 1;
        var source = new OptionEntry(() => new Widget { Tag = value });
        var panel = new WrapPanel();
        panel.Widgets.Add(source.Render());
        OptionsWindow window = CreateWindow(source, panel);

        value = 2;
        window.RefreshCurrentContent();

        Assert.Equal(2, Assert.Single(panel.Widgets).Tag);
    }

    private static OptionsWindow CreateWindow(IOptionSource source, WrapPanel panel)
    {
        var window = (OptionsWindow)RuntimeHelpers.GetUninitializedObject(typeof(OptionsWindow));
        SetField(window, "_optionsPanel", panel);
        SetField(window, "_optionSources", new Dictionary<string, List<IOptionSource>> { ["Interface"] = [source] });
        SetField(window, "_lastCategory", "Interface");
        SetField(window, "_searchField", new MyraInputBox());
        return window;
    }

    private static void SetField(OptionsWindow window, string name, object value) =>
        typeof(OptionsWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(window, value);

    public void Dispose() => CurrentStylesheet.SetValue(null, _previousStylesheet);
}
