#nullable enable
using ClassicUO.Configuration;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

public static class SearchableComboBoxLocalization
{
    public static void Install()
    {
        SearchableComboBoxStrings.HintText = () => TazLang.Get("mog_searchellipses", "Search...");
        SearchableComboBoxStrings.CaseSensitive = () => TazLang.Get("scb_casesensitive", "Aa");
        SearchableComboBoxStrings.WholeWord = () => TazLang.Get("scb_wholeword", "ab|");
        SearchableComboBoxStrings.Regex = () => TazLang.Get("scb_regex", ".*");
        SearchableComboBoxStrings.NoResults = () => TazLang.Get("scb_noresults", "No results");
        SearchableComboBoxStrings.InvalidRegex = () => TazLang.Get("scb_invalidregex", "Invalid regex");
    }
}
