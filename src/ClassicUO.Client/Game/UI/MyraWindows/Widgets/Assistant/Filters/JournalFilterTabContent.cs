#nullable enable
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

public static class JournalFilterTabContent
{
    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel(
            "Journal Filter hides specific messages from the journal. Messages that match exactly will be filtered out.",
            MyraLabel.TextStyle.H3));

        var addFilterPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newFilterBox = new MyraInputBox { HintText = "Filter text (exact match)", Width = 300 };

        var filtersPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildFilterList()
        {
            filtersPanel.Widgets.Clear();
            List<string> filters = JournalFilterManager.Instance.Filters.ToList();

            if (filters.Count == 0)
            {
                filtersPanel.Widgets.Add(new MyraLabel("No filters configured.", MyraLabel.TextStyle.H3));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(GridColumnInfo.Fill("Filter Text"), GridColumnInfo.Auto("Actions"));

            int dataRow = 1;
            for (int i = filters.Count - 1; i >= 0; i--)
            {
                string filter = filters[i];

                // Track current value so we can remove-old/add-new on every edit
                string[] current = { filter };
                var filterBox = new MyraInputBox { Text = filter };
                filterBox.TextChangedByUser += (_, _) =>
                {
                    string newVal = filterBox.Text ?? "";
                    if (!string.IsNullOrWhiteSpace(newVal) && newVal != current[0])
                    {
                        JournalFilterManager.Instance.RemoveFilter(current[0]);
                        JournalFilterManager.Instance.AddFilter(newVal);
                        JournalFilterManager.Instance.Save(false);
                        current[0] = newVal;
                    }
                };
                grid.AddWidget(filterBox, dataRow, 0);

                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Delete", () =>
                {
                    JournalFilterManager.Instance.RemoveFilter(current[0]);
                    JournalFilterManager.Instance.Save(false);
                    BuildFilterList();
                }) { Tooltip = "Delete this filter" }), dataRow, 1);

                dataRow++;
            }

            filtersPanel.Widgets.Add(grid);
        }

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("Add", () =>
        {
            string text = newFilterBox.Text ?? "";
            if (!string.IsNullOrWhiteSpace(text))
            {
                JournalFilterManager.Instance.AddFilter(text);
                JournalFilterManager.Instance.Save(false);
                newFilterBox.Text = "";
                addFilterPanel.Visible = false;
                BuildFilterList();
            }
        }));
        addConfirmRow.Widgets.Add(new MyraButton("Cancel", () =>
        {
            addFilterPanel.Visible = false;
            newFilterBox.Text = "";
        }));

        var addFieldRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldRow.Widgets.Add(new MyraLabel("Filter Text:", MyraLabel.TextStyle.P)
            { Tooltip = "Must match the journal entry exactly. Partial matches not supported." });
        addFieldRow.Widgets.Add(newFilterBox);

        addFilterPanel.Widgets.Add(new MyraLabel("Add New Filter:", MyraLabel.TextStyle.H3));
        addFilterPanel.Widgets.Add(addFieldRow);
        addFilterPanel.Widgets.Add(addConfirmRow);

        var actionRow = new HorizontalStackPanel { Spacing = 4 };
        actionRow.Widgets.Add(new MyraButton("Add Filter Entry", () => addFilterPanel.Visible = !addFilterPanel.Visible));
        actionRow.Widgets.Add(new MyraButton("Import", () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && JournalFilterManager.Instance.ImportFromJson(json))
            {
                BuildFilterList();
                return;
            }
            GameActions.Print("Your clipboard does not have a valid export copied.", Constants.HUE_ERROR);
        }) { Tooltip = "Import from your clipboard, must have a valid export copied." });
        actionRow.Widgets.Add(new MyraButton("Export", () =>
        {
            JournalFilterManager.Instance.GetJsonExport()?.CopyToClipboard();
            GameActions.Print("Exported journal filters to your clipboard!", Constants.HUE_SUCCESS);
        }) { Tooltip = "Export your filters to your clipboard." });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addFilterPanel);
        root.Widgets.Add(new MyraLabel("Current Journal Filters:", MyraLabel.TextStyle.H3));
        BuildFilterList();
        root.Widgets.Add(new ScrollViewer { Height = 250, Content = filtersPanel });

        return root;
    }
}
