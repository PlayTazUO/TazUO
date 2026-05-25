#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

public static class SoundFilterTabContent
{
    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel(
            "声音过滤器允许您按ID静音特定的游戏声音。",
            MyraLabel.TextStyle.H3));

        var lastSoundPanel = new VerticalStackPanel { Spacing = 2 };
        var filtersPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildFilterList()
        {
            filtersPanel.Widgets.Clear();
            var filterList = SoundFilterManager.Instance.FilteredSounds.OrderBy(x => x).ToList();

            if (filterList.Count == 0)
            {
                filtersPanel.Widgets.Add(new MyraLabel("没有过滤的声音。", MyraLabel.TextStyle.P));
                return;
            }

            filtersPanel.Widgets.Add(new MyraLabel($"共 {filterList.Count} 个声音已过滤", MyraLabel.TextStyle.P));

            filtersPanel.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("清除所有过滤器", () =>
            {
                SoundFilterManager.Instance.Clear();
                BuildFilterList();
            })));

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("声音ID"),
                GridColumnInfo.Fill("操作")
            );

            int dataRow = 1;
            for (int i = filterList.Count - 1; i >= 0; i--)
            {
                int soundId = filterList[i];

                // Track current ID so we can remove-old/add-new on edit without rebuilding
                int[] current = { soundId };
                var soundBox = new MyraInputBox { Text = soundId.ToString() };
                soundBox.TextChangedByUser += (_, _) =>
                {
                    if (int.TryParse(soundBox.Text, out int newId))
                    {
                        newId = Math.Clamp(newId, 0, 65535);
                        if (newId != current[0])
                        {
                            SoundFilterManager.Instance.RemoveFilter(current[0]);
                            SoundFilterManager.Instance.AddFilter(newId);
                            current[0] = newId;
                        }
                    }
                };
                grid.AddWidget(soundBox, dataRow, 0);

                int capturedId = soundId;
                var actionsPanel = new HorizontalStackPanel { Spacing = 4 };
                actionsPanel.Widgets.Add(
                    new MyraButton("播放", () => Client.Game.Audio.PlaySound(current[0], true))
                    {
                        Tooltip = "测试播放此声音（绕过过滤器）",
                    }
                );
                actionsPanel.Widgets.Add(
                    MyraStyle.ApplyButtonDangerStyle(
                        new MyraButton(
                            "删除",
                            () =>
                            {
                                SoundFilterManager.Instance.RemoveFilter(current[0]);
                                BuildFilterList();
                            }
                        )
                        {
                            Tooltip = "删除此过滤器",
                        }
                    )
                );

                grid.AddWidget(actionsPanel, dataRow, 1);


                dataRow++;
            }

            filtersPanel.Widgets.Add(grid);
        }

        void BuildLastSoundSection()
        {
            lastSoundPanel.Widgets.Clear();
            lastSoundPanel.Widgets.Add(new MyraLabel("最近播放:", MyraLabel.TextStyle.H3));

            int c = 0;
            foreach ((int, string) sound in Client.Game.Audio.LastPlayedSounds.GetItems())
            {
                c++;

                int id = sound.Item1;

                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel($"声音ID: {id} ({sound.Item2})", MyraLabel.TextStyle.P));
                row.Widgets.Add(new MyraButton("添加过滤器", () =>
                {
                    SoundFilterManager.Instance.AddFilter(id);
                    BuildFilterList();
                }) { Tooltip = "将此声音添加到过滤列表" });
                row.Widgets.Add(new MyraButton("再次播放", () =>
                    Client.Game.Audio.PlaySound(id, true)) { Tooltip = "再次播放此声音" });
                lastSoundPanel.Widgets.Add(row);
            }

            lastSoundPanel.Widgets.Add(new MyraButton("刷新", () => BuildLastSoundSection())
            {
                Tooltip = "刷新最近播放的声音显示"
            }.PlaceBefore(new MyraLabel(
                                          "提示: 在游戏中播放声音以查看其ID，然后点击添加过滤器。",
                                          MyraLabel.TextStyle.P)));

            if(c == 0)
            {
                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel("尚未播放声音。", MyraLabel.TextStyle.P));
                row.Widgets.Add(new MyraButton("刷新", () => BuildLastSoundSection())
                    { Tooltip = "刷新最近播放的声音显示" });
                lastSoundPanel.Widgets.Add(row);
            }
        }

        var addFilterPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newSoundBox = new MyraInputBox { HintText = "声音ID (0-65535)", Width = 120 };

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("添加", () =>
        {
            if (int.TryParse(newSoundBox.Text, out int soundId))
            {
                soundId = Math.Clamp(soundId, 0, 65535);
                SoundFilterManager.Instance.AddFilter(soundId);
                newSoundBox.Text = "";
                addFilterPanel.Visible = false;
                BuildFilterList();
            }
        }));
        addConfirmRow.Widgets.Add(new MyraButton("测试播放", () =>
        {
            if (int.TryParse(newSoundBox.Text, out int soundId))
                Client.Game.Audio.PlaySound(Math.Clamp(soundId, 0, 65535), true);
        }) { Tooltip = "测试播放此声音ID" });
        addConfirmRow.Widgets.Add(new MyraButton("取消", () =>
        {
            addFilterPanel.Visible = false;
            newSoundBox.Text = "";
        }));

        var addFieldRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldRow.Widgets.Add(new MyraLabel("声音ID:", MyraLabel.TextStyle.P)
            { Tooltip = "输入要过滤的声音的数字ID (0-65535)" });
        addFieldRow.Widgets.Add(newSoundBox);

        addFilterPanel.Widgets.Add(new MyraLabel("添加声音过滤器:", MyraLabel.TextStyle.H3));
        addFilterPanel.Widgets.Add(addFieldRow);
        addFilterPanel.Widgets.Add(addConfirmRow);

        var actionRow = new HorizontalStackPanel { Spacing = 4 };
        actionRow.Widgets.Add(new MyraButton("添加过滤条目", () => addFilterPanel.Visible = !addFilterPanel.Visible));
        actionRow.Widgets.Add(new MyraButton("导入", () =>
        {
            try
            {
                string? json = Clipboard.GetClipboardText();
                if (string.IsNullOrWhiteSpace(json))
                {
                    GameActions.Print("剪贴板为空", Constants.HUE_ERROR);
                    return;
                }

                HashSet<int>? importedFilters = JsonSerializer.Deserialize(json, HashSetIntContext.Default.HashSetInt32);
                if (importedFilters == null)
                {
                    GameActions.Print("解析剪贴板数据失败", Constants.HUE_ERROR);
                    return;
                }

                int added = 0;
                foreach (int id in importedFilters)
                {
                    if (SoundFilterManager.Instance.FilteredSounds.Add(Math.Clamp(id, 0, 65535)))
                        added++;
                }
                SoundFilterManager.Instance.Save();
                BuildFilterList();
                GameActions.Print($"从剪贴板添加了 {added} 个声音过滤器", Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print($"导入失败: {ex.Message}", Constants.HUE_ERROR);
            }
        }) { Tooltip = "从剪贴板JSON导入过滤的声音（追加到当前过滤器）" });
        actionRow.Widgets.Add(new MyraButton("导出", () =>
        {
            try
            {
                string json = JsonSerializer.Serialize(
                    SoundFilterManager.Instance.FilteredSounds,
                    HashSetIntContext.Default.HashSetInt32);
                json.CopyToClipboard();
                GameActions.Print(
                    $"已将 {SoundFilterManager.Instance.FilteredSounds.Count} 个声音过滤器导出到剪贴板",
                    Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print($"导出失败: {ex.Message}", Constants.HUE_ERROR);
            }
        }) { Tooltip = "将所有过滤的声音导出为JSON到剪贴板" });

        BuildLastSoundSection();
        root.Widgets.Add(lastSoundPanel);
        root.Widgets.Add(actionRow);
        root.Widgets.Add(addFilterPanel);
        root.Widgets.Add(new MyraLabel("已过滤的声音:", MyraLabel.TextStyle.H3));
        BuildFilterList();
        root.Widgets.Add(new ScrollViewer { Height = 250, Content = filtersPanel });

        return root;
    }
}
