using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    internal class ScriptManagerGump : ResizableGump
    {
        private AlphaBlendControl background;
        private ScrollArea scrollArea;
        private NiceButton refresh;
        private TextBox title;
        internal const int GROUPINDENT = 10;
        internal const int V_SPACING = 2;
        private const int MIN_WIDTH = 200;
        private const int REFRESH_BUTTON_WIDTH = 75;
        private HashSet<string> groups = new HashSet<string>();
        private static int lastX = -1, lastY = -1;
        private static int lastWidth = 300, lastHeight = 400;
        public override GumpType GumpType => GumpType.ScriptManager;
        public static bool RefreshContent = false;
        public const string NOGROUPTEXT = "No group";
        public ScriptManagerGump() : base(lastWidth, lastHeight, MIN_WIDTH, 200, 0, 0)
        {
            X = lastX;
            Y = lastY;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            CanMove = true;
            AnchorType = ANCHOR_TYPE.DISABLED;
            LegionScripting.LoadScriptsFromFile();

            Add(background = new AlphaBlendControl(0.77f) { X = BorderControl.BorderSize, Y = BorderControl.BorderSize });

            title = TextBox.GetOne("Script Manager", TrueTypeLoader.EMBEDDED_FONT, 18, Color.DarkOrange, TextBox.RTLOptions.Default(Width - 2 * BorderControl.BorderSize));
            title.X = BorderControl.BorderSize;
            title.Y = BorderControl.BorderSize;
            title.AcceptMouseInput = false;
            Add(title);

            Add(refresh = new NiceButton(Width - REFRESH_BUTTON_WIDTH - BorderControl.BorderSize, BorderControl.BorderSize, REFRESH_BUTTON_WIDTH, 25, ButtonAction.Default, "Menu")
            {
                IsSelectable = false
            });

            refresh.ContextMenu = new ContextMenuControl();
            refresh.ContextMenu.Add(new ContextMenuItemEntry("Refresh", () =>
            {
                Dispose();
                ScriptManagerGump g = new ScriptManagerGump() { X = X, Y = Y };
                g.ResizeWindow(new Point(Width, Height));
                UIManager.Add(g);
            }));
            refresh.ContextMenu.Add(new ContextMenuItemEntry("Public Script Browser", () =>
            {
                UIManager.Add(new ScriptBrowser());
            }));

            refresh.MouseDown += (s, e) =>
            {
                refresh.ContextMenu?.Show();
            };

            Add(scrollArea = new ScrollArea(BorderControl.BorderSize, refresh.Height + refresh.Y, Width - (BorderControl.BorderSize * 2), Height - (BorderControl.BorderSize * 2) - 25, true));
            scrollArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            BuildGump();

            if (lastX == -1 && lastY == -1)
            {
                CenterXInViewPort();
                CenterYInViewPort();
            }

            ResizeWindow(new Point(lastWidth, lastHeight));
            OnResize();
        }

        private void BuildGump()
        {
            Dictionary<string, Dictionary<string, List<ScriptFile>>> groupsMap = new Dictionary<string, Dictionary<string, List<ScriptFile>>>
            {
                { "", new Dictionary<string, List<ScriptFile>>(){ { "", new List<ScriptFile>() } } }
            };

            foreach (ScriptFile sf in LegionScripting.LoadedScripts)
            {
                if (!groupsMap.ContainsKey(sf.Group))
                    groupsMap[sf.Group] = new Dictionary<string, List<ScriptFile>>();

                if (!groupsMap[sf.Group].ContainsKey(sf.SubGroup))
                    groupsMap[sf.Group][sf.SubGroup] = new List<ScriptFile>();

                var grouppath = Path.Combine(sf.Group, sf.SubGroup);
                if (!groups.Contains(grouppath))
                    groups.Add(grouppath);

                groupsMap[sf.Group][sf.SubGroup].Add(sf);
            }

            int y = 0;

            foreach (var group in groupsMap)
            {
                var g = new GroupControl(group.Key == "" ? NOGROUPTEXT : group.Key, Width - scrollArea.ScrollBarWidth() - 2 - GROUPINDENT) { Y = y };
                g.GroupExpandedShrunk += GroupExpandedShrunk;
                g.AddGroups(group.Value);

                y += g.Height + V_SPACING;
                scrollArea.Add(g);
            }
        }

        private void GroupExpandedShrunk(object sender, EventArgs e)
        {
            RepositionChildren();
        }
        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("rw", Width.ToString());
            writer.WriteAttributeString("rh", Height.ToString());
        }
        public override void SlowUpdate()
        {
            base.SlowUpdate();
            if (RefreshContent)
            {
                RefreshContent = false;
                Dispose();
                ScriptManagerGump g = new ScriptManagerGump() { X = X, Y = Y };
                g.ResizeWindow(new Point(Width, Height));
                UIManager.Add(g);
            }
        }
        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            Point savedSize = new Point(Width, Height);

            if (int.TryParse(xml.GetAttribute("rw"), out int width) && width > 0)
                savedSize.X = width;

            if (int.TryParse(xml.GetAttribute("rh"), out int height) && height > 0)
                savedSize.Y = height;

            ResizeWindow(savedSize);

            int.TryParse(xml.GetAttribute("x"), out X);
            int.TryParse(xml.GetAttribute("y"), out Y);
        }
        public override void OnResize()
        {
            base.OnResize();

            if (background != null) //Quick check to see if the gump has been built yet
            {
                background.Width = Width - (BorderControl.BorderSize * 2);
                background.Height = Height - (BorderControl.BorderSize * 2);

                title.Width = Width - REFRESH_BUTTON_WIDTH - (BorderControl.BorderSize * 2);

                refresh.X = Width - BorderControl.BorderSize - refresh.Width;

                scrollArea.Width = Width - (BorderControl.BorderSize * 2);
                scrollArea.Height = Height - BorderControl.BorderSize - (refresh.Y + refresh.Height);
                scrollArea.UpdateScrollbarPosition();

                RepositionChildren();
            }

            lastWidth = Width;
            lastHeight = Height;
        }
        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);
            lastX = X;
            lastY = Y;
        }
        private void RepositionChildren()
        {
            int y = 0;
            foreach (Control c in scrollArea.Children)
            {
                if (c is ScrollBarBase) continue;

                c.Y = y;
                y += c.Height + V_SPACING;

                if (c is GroupControl gc)
                {
                    gc.UpdateSize(scrollArea.Width - scrollArea.ScrollBarWidth() - 2);
                }
            }
        }

        internal class GroupControl : Control
        {
            public event EventHandler<EventArgs> GroupExpandedShrunk;
            private readonly NiceButton expand, options;
            private readonly TextBox label;
            private readonly DataBox dataBox;
            private readonly string group;
            private readonly string parentGroup;
            private const int HEIGHT = 25;

            private const string SCRIPT_HEADER =
            "# See examples at" +
            "\n#   https://github.com/PlayTazUO/PublicLegionScripts/" +
            "\n# Or documentation at" +
            "\n#   https://github.com/PlayTazUO/TazUO/wiki/TazUO.Legion-Scripting";
            private const string EXAMPLE_LSCRIPT =
            SCRIPT_HEADER +
            @"
player = API.Player
delay = 8
diffhits = 10

while True:
    if player.HitsMax - player.Hits > diffhits or player.IsPoisoned:
        if API.BandageSelf():
            API.CreateCooldownBar(delay, 'Bandaging...', 21)
            API.Pause(delay)
        else:
            API.SysMsg('WARNING: No bandages!', 32)
            break
    API.Pause(0.5)";
            private string expandShrink
            {
                get
                {
                    if (dataBox == null) return "-";
                    return dataBox.IsVisible ? "-" : "+";
                }
            }
            public GroupControl(string group, int width, string parentGroup = "")
            {
                CanMove = true;
                Width = width;
                Height = HEIGHT;
                this.group = group;
                this.parentGroup = parentGroup;
                dataBox = new DataBox(0, HEIGHT, width, 0);
                if (parentGroup == "")
                    dataBox.IsVisible = !LegionScripting.IsGroupCollapsed(group);
                else
                    dataBox.IsVisible = !LegionScripting.IsGroupCollapsed(parentGroup, group);

                expand = new NiceButton(0, 0, 25, HEIGHT, ButtonAction.Default, expandShrink) { IsSelectable = false };
                expand.MouseDown += Expand_MouseDown;

                label = TextBox.GetOne(group + "  ", TrueTypeLoader.EMBEDDED_FONT, 16, Color.White, TextBox.RTLOptions.Default());
                label.AcceptMouseInput = false;
                Add(label);
                label.X = expand.X + expand.Width;
                label.Y = (HEIGHT - label.Height) / 2;

                options = new NiceButton(label.X + label.Width, 0, 25, HEIGHT, ButtonAction.Default, "*") { IsSelectable = false };
                options.ContextMenu = new ContextMenuControl();
                options.ContextMenu.Add(new ContextMenuItemEntry("New script", () =>
                {
                    InputRequest r = new InputRequest("Enter a name for this script. \nUse /c[#da6e22].lscript/cd or /c[#da6e22].py", "Create", "Cancel", (r, s) =>
                    {
                        if (r == InputRequest.Result.BUTTON1 && !string.IsNullOrEmpty(s))
                        {
                            if (!s.EndsWith(".lscript") && !s.EndsWith(".py"))
                            {
                                GameActions.Print("Script files must end with .lscript or .py", 32);
                                return;
                            }
                            try
                            {
                                string gPath = parentGroup == "" ? group : Path.Combine(parentGroup, group);
                                if (gPath == NOGROUPTEXT)
                                    gPath = string.Empty;
                                if (!File.Exists(Path.Combine(LegionScripting.ScriptPath, gPath, s)))
                                {
                                    File.WriteAllText(Path.Combine(LegionScripting.ScriptPath, gPath, s), SCRIPT_HEADER);
                                    ScriptManagerGump.RefreshContent = true;
                                }
                            }
                            catch (Exception e) { GameActions.Print(e.ToString(), 32); }
                        }
                    });
                    r.CenterXInScreen();
                    r.CenterYInScreen();
                    UIManager.Add(r);
                }));

                if (string.IsNullOrEmpty(parentGroup))
                    options.ContextMenu.Add(new ContextMenuItemEntry("New group", () =>
                    {
                        InputRequest r = new InputRequest("Enter a name for this group.", "Create", "Cancel", (r, s) =>
                        {
                            if (r == InputRequest.Result.BUTTON1 && !string.IsNullOrEmpty(s))
                            {
                                int p = s.IndexOf('.');
                                if (p != -1)
                                    s = s.Substring(0, p);

                                try
                                {
                                    string gname = group == NOGROUPTEXT ? "" : group;
                                    string path = Path.Combine(LegionScripting.ScriptPath, gname, s);
                                    if (!Directory.Exists(path))
                                    {
                                        Directory.CreateDirectory(path);
                                    }
                                    File.WriteAllText(Path.Combine(path, "Example.py"), EXAMPLE_LSCRIPT);
                                    ScriptManagerGump.RefreshContent = true;
                                }
                                catch (Exception e) { Console.WriteLine(e.ToString()); }
                            }
                        });
                        r.CenterXInScreen();
                        r.CenterYInScreen();
                        UIManager.Add(r);
                    }));

                if (group != NOGROUPTEXT && group != "")
                    options.ContextMenu.Add(new ContextMenuItemEntry("Delete group", () =>
                    {
                        QuestionGump g = new QuestionGump("Delete group?", (r) =>
                        {
                            if (r)
                            {
                                try
                                {
                                    string gPath = parentGroup == "" ? group : Path.Combine(parentGroup, group);
                                    gPath = Path.Combine(LegionScripting.ScriptPath, gPath);
                                    Directory.Delete(gPath, true);
                                    ScriptManagerGump.RefreshContent = true;
                                }
                                catch (Exception) { }
                            }
                        });
                        UIManager.Add(g);
                    }));

                options.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                        options.ContextMenu.Show();
                };

                Add(new AlphaBlendControl(0.35f) { Height = HEIGHT, Width = label.Width + expand.Width + options.Width });
                Add(expand);
                Add(label);
                Add(options);
                Add(dataBox);

                ForceSizeUpdate();
            }

            public void UpdateSize(int width)
            {
                Width = width;

                foreach (Control c in dataBox.Children)
                {
                    if (c is GroupControl gc)
                        gc.UpdateSize(width - GROUPINDENT);

                    if (c is ScriptControl sc)
                        sc.UpdateSize(width);
                }
                dataBox.ForceSizeUpdate(false);
            }

            private void Expand_MouseDown(object sender, MouseEventArgs e)
            {
                dataBox.IsVisible ^= true;

                if (parentGroup == "")
                    LegionScripting.SetGroupCollapsed(group, expanded: !dataBox.IsVisible);
                else
                    LegionScripting.SetGroupCollapsed(parentGroup, group, !dataBox.IsVisible);

                expand.TextLabel.Text = expandShrink;
                ForceSizeUpdate(false);
                GroupExpandedShrunk?.Invoke(this, null);
            }

            public void AddItems(List<ScriptFile> files)
            {
                foreach (ScriptFile file in files)
                    dataBox.Add(new ScriptControl(dataBox.Width, file));

                dataBox.ReArrangeChildren(V_SPACING);
                dataBox.ForceSizeUpdate();
                ForceSizeUpdate();
            }

            public void AddGroups(Dictionary<string, List<ScriptFile>> groups)
            {
                foreach (var obj in groups)
                {
                    if (!string.IsNullOrEmpty(obj.Key))
                    {
                        GroupControl subG = new GroupControl(obj.Key, Width - GROUPINDENT, group) { X = GROUPINDENT };
                        subG.AddItems(obj.Value);
                        subG.GroupExpandedShrunk += SubG_GroupExpandedShrunk;
                        dataBox.Add(subG);
                    }
                    else
                    {
                        AddItems(obj.Value);
                    }
                }

                dataBox.ReArrangeChildren(V_SPACING);
                dataBox.ForceSizeUpdate();
                ForceSizeUpdate();
            }

            private void SubG_GroupExpandedShrunk(object sender, EventArgs e)
            {
                dataBox.ReArrangeChildren(V_SPACING);
                dataBox.ForceSizeUpdate(false);
                ForceSizeUpdate(false);
                GroupExpandedShrunk?.Invoke(this, null);
            }

            public override void Dispose()
            {
                base.Dispose();
                GroupExpandedShrunk = null;
            }
        }

        internal class ScriptControl : Control
        {
            private readonly AlphaBlendControl background;
            private readonly TextBox label;
            private NiceButton playstop, menu;

            public ScriptFile Script { get; }
            private string ScriptDisplayName
            {
                get
                {
                    if (Script == null || string.IsNullOrEmpty(Script.FileName))
                        return string.Empty;

                    int lastDotIndex = Script.FileName.LastIndexOf('.');
                    return lastDotIndex == -1 ? Script.FileName : Script.FileName.Substring(0, lastDotIndex);
                }
            }
            private string playStopText
            {
                get
                {
                    if (Script == null)
                        return "Play";

                    if (Script.IsPlaying)
                        return "Stop";

                    return "Play";
                }
            }

            public ScriptControl(int w, ScriptFile script)
            {
                Width = w;
                Height = 25;
                Script = script;
                CanMove = true;

                SetTooltip(Script.FileName); //Full filename to show py or lscript

                Add(background = new AlphaBlendControl(0.35f) { Height = Height, Width = Width });

                label = TextBox.GetOne(ScriptDisplayName, TrueTypeLoader.EMBEDDED_FONT, 16, Color.White, TextBox.RTLOptions.Default(w - 130));
                label.AcceptMouseInput = false;
                Add(label);
                label.Y = 5;
                label.X = 5;

                Add(playstop = new NiceButton(w - 75, 0, 50, Height, ButtonAction.Default, playStopText) { IsSelectable = false });
                playstop.MouseUp += Play_MouseUp;

                Add(menu = new NiceButton(w - 25, 0, 25, Height, ButtonAction.Default, "+") { IsSelectable = false });
                menu.MouseDown += (s, e) => { ContextMenu?.Show(); };

                SetMenuColor();

                UpdateSize(w);
                SetBGColors();

                ContextMenu = new ContextMenuControl();

                ContextMenu.Add(new ContextMenuItemEntry(Script.FileName) { IsSelected = true });
                ContextMenu.Add(new ContextMenuItemEntry("Edit", () => { UIManager.Add(new ScriptEditor(Script)); }));
                ContextMenu.Add(new ContextMenuItemEntry("Edit Externally", () => { OpenFileWithDefaultApp(Script.FullPath); }));
                ContextMenu.Add(new ContextMenuItemEntry("Autostart", () => { GenAutostartContext().Show(); }));
                ContextMenu.Add(new ContextMenuItemEntry("Create macro button", () =>
                {
                    var mm = MacroManager.TryGetMacroManager();

                    if (mm != null)
                    {
                        Macro mac = new (script.FileName);
                        mac.Items = new MacroObjectString(MacroType.ClientCommand, MacroSubType.MSC_NONE, "togglelscript " + script.FileName);
                        mm.PushToBack(mac);

                        MacroButtonGump bg = new(mac, Mouse.Position.X, Mouse.Position.Y);
                        UIManager.Add(bg);
                    }
                }));
                ContextMenu.Add(new ContextMenuItemEntry("Delete", () =>
                {
                    QuestionGump g = new QuestionGump("Are you sure?", (r) =>
                    {
                        if (r)
                        {
                            try
                            {
                                File.Delete(Script.FullPath);
                                LegionScripting.LoadedScripts.Remove(Script);
                                Dispose();
                            }
                            catch (Exception) { }
                        }
                    });
                    UIManager.Add(g);
                }));

                LegionScripting.ScriptStartedEvent += ScriptStarted;
                LegionScripting.ScriptStoppedEvent += ScriptStopped;
            }

            public override void Dispose()
            {
                base.Dispose();
                LegionScripting.ScriptStoppedEvent -= ScriptStopped;
                LegionScripting.ScriptStartedEvent -= ScriptStarted;
            }

            private void ScriptStopped(object sender, ScriptInfoEvent e)
            {
                SetBGColors();
            }

            private void ScriptStarted(object sender, ScriptInfoEvent e)
            {
                SetBGColors();
            }

            private void SetMenuColor()
            {
                bool global = LegionScripting.AutoLoadEnabled(Script, true);
                bool chara = LegionScripting.AutoLoadEnabled(Script, false);

                if (global || chara)
                    menu.TextLabel.Hue = 1970;
                else
                    menu.TextLabel.Hue = ushort.MaxValue;
            }

            private static void OpenFileWithDefaultApp(string filePath)
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", filePath);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", filePath);
                    }
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
            private ContextMenuControl GenAutostartContext()
            {
                ContextMenuControl context = new ContextMenuControl();
                bool global = LegionScripting.AutoLoadEnabled(Script, true);
                bool chara = LegionScripting.AutoLoadEnabled(Script, false);

                context.Add(new ContextMenuItemEntry("All characters", () => { LegionScripting.SetAutoPlay(Script, true, !global); SetMenuColor(); }, true, global));
                context.Add(new ContextMenuItemEntry("This character", () => { LegionScripting.SetAutoPlay(Script, false, !chara); SetMenuColor(); }, true, chara));

                return context;
            }

            private void Play_MouseUp(object sender, MouseEventArgs e)
            {
                if (Script != null)
                {
                    if (Script.IsPlaying || (Script.GetScript != null && Script.GetScript.IsPlaying))
                        LegionScripting.StopScript(Script);
                    else
                        LegionScripting.PlayScript(Script);
                }
            }

            private void SetBGColors()
            {
                if (Script.IsPlaying || (Script.GetScript != null && Script.GetScript.IsPlaying))
                    background.BaseColor = Color.DarkGreen;
                else
                    background.BaseColor = Color.DarkRed;

                playstop.TextLabel.Text = playStopText;
            }

            public void UpdateSize(int w)
            {
                Width = w;
                background.Width = w;
                label.Text = ScriptDisplayName;
                label.Width = w - 80;
                label.Update(); //Force RTL to recreate the label so we can determine if we need to redo it..
                if (label.RTL.Lines.Count > 1)
                {
                    var msize = label.RTL.Lines[0].Count;
                    if (msize >= 3)
                        label.Text = ScriptDisplayName.Substring(0, msize - 3) + "...";
                }
                menu.X = w - menu.Width;
                playstop.X = w - menu.Width - playstop.Width;
            }

        }
    }
}
