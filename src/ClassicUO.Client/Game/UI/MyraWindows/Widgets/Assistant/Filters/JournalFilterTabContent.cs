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
            "日志过滤器可隐藏日志中的特定消息。完全匹配的消息将被过滤掉。",
            MyraLabel.TextStyle.H3));

        var addFilterPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newFilterBox = new MyraInputBox { HintText = "过滤文本（精确匹配）", Width = 300 };

        var filtersPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildFilterList()
        {
            filtersPanel.Widgets.Clear();
            List<string> filters = JournalFilterManager.Instance.Filters.ToList();

            if (filters.Count == 0)
            {
                filtersPanel.Widgets.Add(new MyraLabel("没有配置过滤器。", MyraLabel.TextStyle.H3));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(GridColumnInfo.Fill("过滤文本"), GridColumnInfo.Auto("操作"));

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

                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("删除", () =>
                {
                    JournalFilterManager.Instance.RemoveFilter(current[0]);
                    JournalFilterManager.Instance.Save(false);
                    BuildFilterList();
                }) { Tooltip = "删除此过滤器" }), dataRow, 1);

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
        addFieldRow.Widgets.Add(new MyraLabel("过滤文本:", MyraLabel.TextStyle.P)
            { Tooltip = "必须与日志条目完全匹配。不支持部分匹配。" });
        addFieldRow.Widgets.Add(newFilterBox);

        addFilterPanel.Widgets.Add(new MyraLabel("添加新过滤器:", MyraLabel.TextStyle.H3));
        addFilterPanel.Widgets.Add(addFieldRow);
        addFilterPanel.Widgets.Add(addConfirmRow);

        var actionRow = new HorizontalStackPanel { Spacing = 4 };
        actionRow.Widgets.Add(new MyraButton("添加过滤条目", () => addFilterPanel.Visible = !addFilterPanel.Visible));
        actionRow.Widgets.Add(new MyraButton("导入", () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && JournalFilterManager.Instance.ImportFromJson(json))
            {
                BuildFilterList();
                return;
            }
            GameActions.Print("您的剪贴板中没有有效的导出数据。", Constants.HUE_ERROR);
        }) { Tooltip = "从剪贴板导入，必须有有效的导出数据。" });
        actionRow.Widgets.Add(new MyraButton("导出", () =>
        {
            JournalFilterManager.Instance.GetJsonExport()?.CopyToClipboard();
            GameActions.Print("已将日志过滤器导出到剪贴板!", Constants.HUE_SUCCESS);
        }) { Tooltip = "将过滤器导出到剪贴板。" });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addFilterPanel);
        root.Widgets.Add(new MyraLabel("当前日志过滤器:", MyraLabel.TextStyle.H3));
        BuildFilterList();
        root.Widgets.Add(new ScrollViewer { Height = 250, Content = filtersPanel });

        return root;
    }
}
