using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    public class ScriptManagerGump : NineSliceGump
    {
        private ModernScrollArea scrollArea;
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
        public ScriptManagerGump() : base(World.Instance, lastX, lastY, lastWidth, lastHeight, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, true, MIN_WIDTH, 200)
        {
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            CanMove = true;
            LegionScripting.LoadScriptsFromFile();

            title = TextBox.GetOne("Script Manager", TrueTypeLoader.EMBEDDED_FONT, 18, Color.DarkOrange, TextBox.RTLOptions.Default(Width - 2 * BorderSize));
            title.X = BorderSize;
            title.Y = BorderSize;
            title.AcceptMouseInput = false;
            Add(title);

            Add(refresh = new NiceButton(Width - REFRESH_BUTTON_WIDTH - BorderSize, BorderSize, REFRESH_BUTTON_WIDTH, 25, ButtonAction.Default, "Menu")
            {
                IsSelectable = false
            });

            refresh.ContextMenu = new ContextMenuControl(this);
            refresh.ContextMenu.Add(new ContextMenuItemEntry("Refresh", () =>
            {
                Refresh();
            }));
            refresh.ContextMenu.Add(new ContextMenuItemEntry("Public Script Browser", () =>
            {
                UIManager.Add(new ScriptBrowser(World));
            }));
            refresh.ContextMenu.Add(new ContextMenuItemEntry("Script Recording", () =>
            {
                UIManager.Add(new ScriptRecordingGump());
            }));
            refresh.ContextMenu.Add(new ContextMenuItemEntry("Scripting Info", () =>
            {
                ScriptingInfoGump.Show();
            }));
            refresh.ContextMenu.Add(new ContextMenuItemEntry("Persistent Variables", () =>
            {
                Game.UI.ImGuiControls.PersistentVarsWindow.Show();
            }));
            refresh.ContextMenu.Add(new ContextMenuItemEntry("Disable Module Cache", () =>
            {
                LegionScripting.LScriptSettings.DisableModuleCache = !LegionScripting.LScriptSettings.DisableModuleCache;
            }, true, LegionScripting.LScriptSettings.DisableModuleCache));

            refresh.MouseDown += (s, e) =>
            {
                refresh.ContextMenu?.Show();
            };

            Add(scrollArea = new ModernScrollArea(BorderSize, refresh.Height + refresh.Y, Width - (BorderSize * 2), Height - (BorderSize * 2) - 25));
            scrollArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            BuildGump();

            if (lastX == -1 && lastY == -1)
            {
                CenterXInViewPort();
                CenterYInViewPort();
            }

            OnResizeComplete();
        }

        public void Refresh()
        {
            Dispose();
            ScriptManagerGump g = new ScriptManagerGump() { X = X, Y = Y };
            g.Width = Width;
            g.Height = Height;
            UIManager.Add(g);
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
                var g = new GroupControl(World, this, group.Key == "" ? NOGROUPTEXT : group.Key, Width - 12 - 2 - GROUPINDENT) { Y = y }; // ModernScrollArea uses SCROLLBAR_WIDTH = 12
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

        public override void PreDraw()
        {
            base.PreDraw();
            if (RefreshContent)
            {
                RefreshContent = false;
                Dispose();
                ScriptManagerGump g = new ScriptManagerGump() { X = X, Y = Y };
                g.Width = Width;
                g.Height = Height;
                UIManager.Add(g);
            }
        }
        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            if (int.TryParse(xml.GetAttribute("rw"), out int width) && width > 0)
                Width = width;

            if (int.TryParse(xml.GetAttribute("rh"), out int height) && height > 0)
                Height = height;

            int.TryParse(xml.GetAttribute("x"), out X);
            int.TryParse(xml.GetAttribute("y"), out Y);

            OnResizeComplete();
        }
        protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            base.OnResize(oldWidth, oldHeight, newWidth, newHeight);
            OnResizeComplete();
        }

        private void OnResizeComplete()
        {
            if (title != null) //Quick check to see if the gump has been built yet
            {
                title.Width = Width - REFRESH_BUTTON_WIDTH - (BorderSize * 2);

                refresh.X = Width - BorderSize - refresh.Width;

                scrollArea.UpdateWidth(Width - (BorderSize * 2));
                scrollArea.UpdateHeight(Height - BorderSize - (refresh.Y + refresh.Height));

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
                    gc.UpdateSize(scrollArea.Width - 12 - 2); // ModernScrollArea uses SCROLLBAR_WIDTH = 12
                }
            }
        }

        public class GroupControl : Control
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

            private World World;
            private Gump parentGump;
            public GroupControl(World world, Gump parentGump, string group, int width, string parentGroup = "")
            {
                this.parentGump = parentGump;
                World = world;
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
                label.X = expand.X + expand.Width;
                label.Y = (HEIGHT - label.Height) / 2;

                options = new NiceButton(label.X + label.Width, 0, 25, HEIGHT, ButtonAction.Default, "*") { IsSelectable = false };
                options.ContextMenu = new ContextMenuControl(parentGump);
                options.ContextMenu.Add(new ContextMenuItemEntry("New script", () =>
                {
                    InputRequest r = new InputRequest(world, "Enter a name for this script. \nUse /c[#da6e22].lscript/cd or /c[#da6e22].py", "Create", "Cancel", (r, s) =>
                    {
                        if (r == InputRequest.Result.BUTTON1 && !string.IsNullOrEmpty(s))
                        {
                            if (!s.EndsWith(".lscript") && !s.EndsWith(".py"))
                            {
                                GameActions.Print(world, "Script files must end with .lscript or .py", 32);
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
                            catch (Exception e) { GameActions.Print(world, e.ToString(), 32); }
                        }
                    });
                    r.CenterXInScreen();
                    r.CenterYInScreen();
                    UIManager.Add(r);
                }));

                if (string.IsNullOrEmpty(parentGroup))
                    options.ContextMenu.Add(new ContextMenuItemEntry("New group", () =>
                    {
                        InputRequest r = new InputRequest(world, "Enter a name for this group.", "Create", "Cancel", (r, s) =>
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
                        QuestionGump g = new QuestionGump(world, "Delete group?", (r) =>
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
                    dataBox.Add(new ScriptControl(parentGump, dataBox.Width, file));

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
                        GroupControl subG = new GroupControl(World, parentGump, obj.Key, Width - GROUPINDENT, group) { X = GROUPINDENT };
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

        public class ScriptControl : Control
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
            private World World;
            private Gump parentGump;

            public ScriptControl(Gump parentGump, int w, ScriptFile script)
            {
                this.parentGump = parentGump;
                World = parentGump.World;
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

                ContextMenu = new ContextMenuControl(parentGump);

                ContextMenu.Add(new ContextMenuItemEntry(Script.FileName) { IsSelected = true });
                ContextMenu.Add(new ContextMenuItemEntry("Edit", () => { UIManager.Add(new ScriptEditor(World, Script)); }));
                ContextMenu.Add(new ContextMenuItemEntry("Edit Externally", () => { OpenFileWithDefaultApp(Script.FullPath); }));
                ContextMenu.Add(new ContextMenuItemEntry("Autostart", () => { GenAutostartContext().Show(); }));
                ContextMenu.Add(new ContextMenuItemEntry("Create macro button", () =>
                {
                    var mm = MacroManager.TryGetMacroManager(World);

                    if (mm != null)
                    {
                        Macro mac = new (script.FileName);
                        mac.Items = new MacroObjectString(MacroType.ClientCommand, MacroSubType.MSC_NONE, "togglelscript " + script.FileName);
                        mm.PushToBack(mac);

                        MacroButtonGump bg = new(World, mac, Mouse.Position.X, Mouse.Position.Y);
                        UIManager.Add(bg);
                    }
                }));
                ContextMenu.Add(new ContextMenuItemEntry("Delete", () =>
                {
                    QuestionGump g = new QuestionGump(World, "Are you sure?", (r) =>
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
                ContextMenuControl context = new ContextMenuControl(parentGump);
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
                    background.BaseColor = Color.DarkOliveGreen;
                else
                    background.BaseColor = Color.DarkSlateBlue;

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
