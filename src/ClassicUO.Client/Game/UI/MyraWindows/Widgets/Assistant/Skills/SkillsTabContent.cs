#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Skills;

public static class SkillsTabContent
{
    public static Widget Build()
    {
        Skill[]? skills = World.Instance?.Player?.Skills;
        if (skills == null)
            return new MyraLabel("Not connected", MyraLabel.TextStyle.P);

        PlayerMobile player = World.Instance!.Player!;
        int count = skills.Length;
        int[] sortedIndices = new int[count];
        for (int i = 0; i < count; i++) sortedIndices[i] = i;

        int sortColIndex = 1;
        bool sortAscending = true;
        bool showGroups = false;

        var gridPanel = new VerticalStackPanel();

        void SortSkills() =>
            Array.Sort(sortedIndices, (a, b) =>
            {
                if (a >= skills.Length || b >= skills.Length) return 0;
                Skill? sa = skills[a];
                Skill? sb = skills[b];
                if (sa == null || sb == null) return 0;
                int cmp = sortColIndex switch
                {
                    1 => string.Compare(sa.Name, sb.Name, StringComparison.OrdinalIgnoreCase),
                    2 => sa.Value.CompareTo(sb.Value),
                    3 => sa.Base.CompareTo(sb.Base),
                    4 => sa.Cap.CompareTo(sb.Cap),
                    5 => (sa.Base - sa.BaseAtLogin).CompareTo(sb.Base - sb.BaseAtLogin),
                    6 => ((byte)sa.Lock).CompareTo((byte)sb.Lock),
                    _ => 0
                };
                return sortAscending ? cmp : -cmp;
            });

        void AddSkillRow(Skill? skill, int row, MyraGrid grid)
        {
            if (skill == null) return;

            if (skill.IsClickable)
            {
                int capturedIdx = skill.Index;
                grid.AddWidget(
                    new MyraButton("Use", () => GameActions.UseSkill(capturedIdx))
                        { Tooltip = $"Use {skill.Name}" },
                    row, 0);
            }

            var name = new MyraLabel(skill.Name, MyraLabel.TextStyle.P);
            if (skill.IsClickable)
            {
                name.TouchDoubleClick += (_, _) => UIManager.Add(new SkillButtonGump(World.Instance, skill,
                    Input.Mouse.Position.X, Input.Mouse.Position.Y));
                name.Tooltip = $"Double click to create a skill button for {skill.Name}";
            }
            grid.AddWidget(name, row, 1);
            grid.AddWidget(new MyraLabel(skill.Value.ToString("F1"), MyraLabel.TextStyle.P), row, 2);
            grid.AddWidget(new MyraLabel(skill.Base.ToString("F1"), MyraLabel.TextStyle.P), row, 3);
            grid.AddWidget(new MyraLabel(skill.Cap.ToString("F1"), MyraLabel.TextStyle.P), row, 4);

            float delta = skill.Base - skill.BaseAtLogin;
            string deltaStr;
            if (delta > 0f)       deltaStr = $"+{delta:F1}";
            else if (delta < 0f)  deltaStr = $"{delta:F1}";
            else                  deltaStr = "0.0";
            grid.AddWidget(new MyraLabel(deltaStr, MyraLabel.TextStyle.P), row, 5);

            var lockWrapper = new HorizontalStackPanel();
            void BuildLockBtn()
            {
                lockWrapper.Widgets.Clear();
                int capturedSkillIdx = skill.Index;

                var btn = new MyraButton("", () =>
                {
                    byte nextLock = (byte)(((byte)skill.Lock + 1) % 3);
                    GameActions.ChangeSkillLockStatus((ushort)capturedSkillIdx, nextLock);
                    AsyncNetClient.Socket.Send_SkillsRequest(player.Serial);
                    BuildLockBtn();
                });
                btn.Tooltip = $"Lock: {skill.Lock}. Click to cycle.";
                lockWrapper.Widgets.Add(MyraStyle.ApplySkillButtonStyle(btn, skill.Lock));
            }
            BuildLockBtn();
            grid.AddWidget(lockWrapper, row, 6);
        }

        void BuildGroupedRows(MyraGrid mainGrid)
        {
            List<SkillsGroup>? groups = World.Instance?.SkillsGroupManager?.Groups;
            if (groups == null) return;

            int skillCount = skills.Length;
            int dataRow = 1;

            foreach (SkillsGroup group in groups)
            {
                var groupIndices = new List<int>(group.Count);
                float groupTotal = 0f;

                for (int gi = 0; gi < group.Count; gi++)
                {
                    byte skillIdx = group.GetSkill(gi);
                    if (skillIdx == 0xFF || skillIdx >= skillCount) continue;
                    Skill? skill = skills[skillIdx];
                    if (skill == null) continue;
                    groupIndices.Add(skillIdx);
                    groupTotal += skill.Base;
                }

                groupIndices.Sort((a, b) =>
                {
                    Skill? sa = skills[a];
                    Skill? sb = skills[b];
                    if (sa == null || sb == null) return 0;
                    int cmp = sortColIndex switch
                    {
                        1 => string.Compare(sa.Name, sb.Name, StringComparison.OrdinalIgnoreCase),
                        2 => sa.Value.CompareTo(sb.Value),
                        3 => sa.Base.CompareTo(sb.Base),
                        4 => sa.Cap.CompareTo(sb.Cap),
                        5 => (sa.Base - sa.BaseAtLogin).CompareTo(sb.Base - sb.BaseAtLogin),
                        6 => ((byte)sa.Lock).CompareTo((byte)sb.Lock),
                        _ => 0
                    };
                    return sortAscending ? cmp : -cmp;
                });

                var groupHeader = new MyraLabel($"── {group.Name} ({groupTotal:F1}) ──", MyraLabel.TextStyle.H3);
                mainGrid.AddWidget(groupHeader, dataRow, 0);
                Grid.SetColumnSpan(groupHeader, 7);
                dataRow++;

                foreach (int idx in groupIndices)
                {
                    AddSkillRow(skills[idx], dataRow, mainGrid);
                    dataRow++;
                }
            }
        }

        void BuildGrid()
        {
            gridPanel.Widgets.Clear();

            var grid = new MyraGrid();
            grid.AddColumn();
            grid.AddColumn(new Proportion(ProportionType.Fill));
            grid.AddColumn(null, 5);
            MyraStyle.ApplyStandardGridStyling(grid);

            grid.AddWidget(new MyraLabel("Use", MyraLabel.TextStyle.TableHeader), 0, 0);

            void AddSortHeader(string name, int col, int gridCol)
            {
                string indicator = sortColIndex == col ? (sortAscending ? " ↑" : " ↓") : "";
                grid.AddWidget(new MyraButton(name + indicator, () =>
                {
                    if (sortColIndex == col) sortAscending = !sortAscending;
                    else { sortColIndex = col; sortAscending = true; }
                    SortSkills();
                    BuildGrid();
                }), 0, gridCol);
            }

            AddSortHeader("Name",  1, 1);
            AddSortHeader("Value", 2, 2);
            AddSortHeader("Base",  3, 3);
            AddSortHeader("Cap",   4, 4);
            AddSortHeader("+/-",   5, 5);
            AddSortHeader("Lock",  6, 6);

            if (showGroups)
                BuildGroupedRows(grid);
            else
                for (int i = 0; i < sortedIndices.Length; i++)
                    AddSkillRow(skills[sortedIndices[i]], i + 1, grid);

            gridPanel.Widgets.Add(grid);
        }

        // ── Toolbar ─────────────────────────────────────────────────────────
        var toolbar = new HorizontalStackPanel { Spacing = 4 };

        toolbar.Widgets.Add(new MyraButton("All Up", () =>
        {
            for (int i = 0; i < skills.Length; i++)
                if(skills[i].Lock != Lock.Up)
                    GameActions.ChangeSkillLockStatus((ushort)i, (byte)Lock.Up);
            AsyncNetClient.Socket.Send_SkillsRequest(player.Serial);
        }));

        toolbar.Widgets.Add(new MyraButton("All Down", () =>
        {
            for (int i = 0; i < skills.Length; i++)
                if(skills[i].Lock != Lock.Down)
                    GameActions.ChangeSkillLockStatus((ushort)i, (byte)Lock.Down);
            AsyncNetClient.Socket.Send_SkillsRequest(player.Serial);
        }));

        toolbar.Widgets.Add(new MyraButton("All Lock", () =>
        {
            for (int i = 0; i < skills.Length; i++)
                if(skills[i].Lock != Lock.Locked)
                    GameActions.ChangeSkillLockStatus((ushort)i, (byte)Lock.Locked);
            AsyncNetClient.Socket.Send_SkillsRequest(player.Serial);
        }));

        toolbar.Widgets.Add(new MyraLabel("|", MyraLabel.TextStyle.P));

        toolbar.Widgets.Add(new MyraButton("Reset +/-", () =>
        {
            for (int i = 0; i < skills.Length; i++)
                if (skills[i] != null) skills[i].BaseAtLogin = skills[i].Base;
            BuildGrid();
        }) { Tooltip = "Reset the +/- column baseline to current values" });

        toolbar.Widgets.Add(new MyraButton("Copy All", () =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name\tValue\tBase\tCap\t+/-\tLock");
            for (int i = 0; i < sortedIndices.Length; i++)
            {
                int idx = sortedIndices[i];
                if (idx >= skills.Length) continue;
                Skill? skill = skills[idx];
                if (skill == null) continue;
                float d = skill.Base - skill.BaseAtLogin;
                string lockStr = skill.Lock switch
                {
                    Lock.Up     => "Up",
                    Lock.Down   => "Down",
                    Lock.Locked => "Locked",
                    _           => "?"
                };
                sb.AppendLine($"{skill.Name}\t{skill.Value:F1}\t{skill.Base:F1}\t{skill.Cap:F1}\t{d:F1}\t{lockStr}");
            }
            sb.ToString().CopyToClipboard();
            GameActions.Print("Skills copied to clipboard.", Constants.HUE_SUCCESS);
        }) { Tooltip = "Copy all skills to clipboard as tab-separated text" });

        toolbar.Widgets.Add(new MyraLabel("|", MyraLabel.TextStyle.P));

        toolbar.Widgets.Add(MyraCheckButton.CreateWithCallback(false, b =>
        {
            showGroups = b;
            BuildGrid();
        }, "Show Groups"));

        toolbar.Widgets.Add(new MyraLabel("|", MyraLabel.TextStyle.P));

        float baseSum = 0f, capSum = 0f;
        for (int i = 0; i < skills.Length; i++)
            if (skills[i] != null) { baseSum += skills[i].Base; capSum += skills[i].Cap; }
        toolbar.Widgets.Add(new MyraLabel($"Total: {baseSum:F1} / {capSum:F1}", MyraLabel.TextStyle.P));

        SortSkills();
        BuildGrid();

        var root = new VerticalStackPanel { Spacing = 4 };
        root.Widgets.Add(toolbar);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 500, Content = gridPanel });
        return root;
    }
}
