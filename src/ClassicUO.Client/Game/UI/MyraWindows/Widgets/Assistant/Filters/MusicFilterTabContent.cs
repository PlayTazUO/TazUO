#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

public static class MusicFilterTabContent
{
    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel(
            "音乐过滤器允许您按ID静音特定的游戏音乐曲目。",
            MyraLabel.TextStyle.H3));

        var lastMusicPanel = new VerticalStackPanel { Spacing = 2 };
        var filtersPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildFilterList()
        {
            filtersPanel.Widgets.Clear();
            var filterList = SoundFilterManager.Instance.FilteredMusic.OrderBy(x => x).ToList();

            if (filterList.Count == 0)
            {
                filtersPanel.Widgets.Add(new MyraLabel("没有过滤的音乐。", MyraLabel.TextStyle.P));
                return;
            }

            filtersPanel.Widgets.Add(new MyraLabel($"共 {filterList.Count} 个曲目已过滤", MyraLabel.TextStyle.P));

            filtersPanel.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("清除所有过滤器", () =>
            {
                SoundFilterManager.Instance.Clear(isMusic: true);
                BuildFilterList();
            })));

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("音乐ID"),
                GridColumnInfo.Fill("操作")
            );

            int dataRow = 1;
            for (int i = filterList.Count - 1; i >= 0; i--)
            {
                int musicId = filterList[i];

                int[] current = { musicId };
                var musicBox = new MyraInputBox { Text = musicId.ToString() };
                musicBox.TextChangedByUser += (_, _) =>
                {
                    if (int.TryParse(musicBox.Text, out int newId))
                    {
                        newId = Math.Clamp(newId, 0, 149);
                        if (newId != current[0])
                        {
                            SoundFilterManager.Instance.RemoveFilter(current[0], isMusic: true);
                            SoundFilterManager.Instance.AddFilter(newId, isMusic: true);
                            current[0] = newId;
                        }
                    }
                };
                grid.AddWidget(musicBox, dataRow, 0);

                var actionsPanel = new HorizontalStackPanel { Spacing = 4 };
                actionsPanel.Widgets.Add(
                    new MyraButton("播放", () => Client.Game.Audio.PlayMusic(current[0], false, true))
                    {
                        Tooltip = "测试播放此曲目（绕过过滤器）",
                    }
                );
                actionsPanel.Widgets.Add(
                    MyraStyle.ApplyButtonDangerStyle(
                        new MyraButton(
                            "删除",
                            () =>
                            {
                                SoundFilterManager.Instance.RemoveFilter(current[0], isMusic: true);
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

        void BuildLastMusicSection()
        {
            lastMusicPanel.Widgets.Clear();
            lastMusicPanel.Widgets.Add(new MyraLabel("最近播放:", MyraLabel.TextStyle.H3));

            int c = 0;
            foreach ((int, string) track in Client.Game.Audio.LastPlayedMusic.GetItems())
            {
                c++;

                int id = track.Item1;

                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel($"音乐ID: {id} ({track.Item2})", MyraLabel.TextStyle.P));
                row.Widgets.Add(new MyraButton("添加过滤器", () =>
                {
                    SoundFilterManager.Instance.AddFilter(id, isMusic: true);
                    BuildFilterList();
                }) { Tooltip = "将此曲目添加到过滤列表" });
                row.Widgets.Add(new MyraButton("再次播放", () =>
                    Client.Game.Audio.PlayMusic(id, false, true)) { Tooltip = "再次播放此曲目" });
                lastMusicPanel.Widgets.Add(row);
            }

            lastMusicPanel.Widgets.Add(new MyraButton("刷新", () => BuildLastMusicSection())
            {
                Tooltip = "刷新最近播放的音乐显示"
            }.PlaceBefore(new MyraLabel(
                              "提示: 在游戏中让音乐播放以查看其ID，然后点击添加过滤器。",
                              MyraLabel.TextStyle.P)));

            if (c == 0)
            {
                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel("尚未播放音乐。", MyraLabel.TextStyle.P));
                row.Widgets.Add(new MyraButton("刷新", () => BuildLastMusicSection())
                    { Tooltip = "刷新最近播放的音乐显示" });
                lastMusicPanel.Widgets.Add(row);
            }
        }

        var addFilterPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newMusicBox = new MyraInputBox { HintText = "音乐ID (0-149)", Width = 120 };

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("添加", () =>
        {
            if (int.TryParse(newMusicBox.Text, out int musicId))
            {
                musicId = Math.Clamp(musicId, 0, 149);
                SoundFilterManager.Instance.AddFilter(musicId, isMusic: true);
                newMusicBox.Text = "";
                addFilterPanel.Visible = false;
                BuildFilterList();
            }
        }));
        addConfirmRow.Widgets.Add(new MyraButton("测试播放", () =>
        {
            if (int.TryParse(newMusicBox.Text, out int musicId))
                Client.Game.Audio.PlayMusic(Math.Clamp(musicId, 0, 149), false, true);
        }) { Tooltip = "测试播放此音乐ID" });
        addConfirmRow.Widgets.Add(new MyraButton("取消", () =>
        {
            addFilterPanel.Visible = false;
            newMusicBox.Text = "";
        }));

        var addFieldRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldRow.Widgets.Add(new MyraLabel("音乐ID:", MyraLabel.TextStyle.P)
            { Tooltip = "输入要过滤的音乐曲目的数字ID (0-149)" });
        addFieldRow.Widgets.Add(newMusicBox);

        addFilterPanel.Widgets.Add(new MyraLabel("添加音乐过滤器:", MyraLabel.TextStyle.H3));
        addFilterPanel.Widgets.Add(addFieldRow);
        addFilterPanel.Widgets.Add(addConfirmRow);

        var actionRow = new HorizontalStackPanel { Spacing = 4 };
        actionRow.Widgets.Add(new MyraButton("添加过滤条目", () => addFilterPanel.Visible = !addFilterPanel.Visible));
        actionRow.Widgets.Add(new MyraButton("Import", () =>
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
                    if (SoundFilterManager.Instance.FilteredMusic.Add(Math.Clamp(id, 0, 149)))
                        added++;
                }
                SoundFilterManager.Instance.Save(isMusic: true);
                BuildFilterList();
                GameActions.Print($"从剪贴板添加了 {added} 个音乐过滤器", Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print($"导入失败: {ex.Message}", Constants.HUE_ERROR);
            }
        }) { Tooltip = "从剪贴板JSON导入过滤的音乐曲目（追加到当前过滤器）" });
        actionRow.Widgets.Add(new MyraButton("导出", () =>
        {
            try
            {
                string json = JsonSerializer.Serialize(
                    SoundFilterManager.Instance.FilteredMusic,
                    HashSetIntContext.Default.HashSetInt32);
                json.CopyToClipboard();
                GameActions.Print(
                    $"已将 {SoundFilterManager.Instance.FilteredMusic.Count} 个音乐过滤器导出到剪贴板",
                    Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print($"导出失败: {ex.Message}", Constants.HUE_ERROR);
            }
        }) { Tooltip = "将所有过滤的音乐曲目导出为JSON到剪贴板" });

        BuildLastMusicSection();
        root.Widgets.Add(lastMusicPanel);
        root.Widgets.Add(actionRow);
        root.Widgets.Add(addFilterPanel);
        root.Widgets.Add(new MyraLabel("已过滤的音乐:", MyraLabel.TextStyle.H3));
        BuildFilterList();
        root.Widgets.Add(new ScrollViewer { Height = 250, Content = filtersPanel });

        return root;
    }
}
