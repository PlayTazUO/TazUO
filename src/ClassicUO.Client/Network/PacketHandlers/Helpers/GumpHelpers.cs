using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.LegionScripting;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Network.PacketHandlers.Helpers;

internal static class GumpHelpers
{
    [ThreadStatic] private static TextFileParser _parser;
    [ThreadStatic] private static TextFileParser _cmdParser;

    private static TextFileParser GetParser()
    {
        if (_parser == null)
            _parser = new TextFileParser(
                string.Empty,
                new[] { ' ' },
                new char[] { },
                new[] { '{', '}' }
            );
        return _parser;
    }

    private static TextFileParser GetCmdParser()
    {
        if (_cmdParser == null)
            _cmdParser = new TextFileParser(
                string.Empty,
                new[] { ' ', ',' },
                new char[] { },
                new[] { '@', '@' }
            );
        return _cmdParser;
    }

    public static Gump CreateGump(
        World world,
        uint sender,
        uint gumpID,
        int x,
        int y,
        string layout,
        string[] lines
    )
    {
        ScriptRecorder.Instance.RecordWaitForGump(gumpID);
        ScriptingInfoGump.AddOrUpdateInfo("Last Gump Opened", $"0x{gumpID:X}");

        if (string.IsNullOrEmpty(layout))
            return null;

        List<string> cmdlist = GetParser().GetTokens(layout);
        int cmdlen = cmdlist.Count;

        if (cmdlen <= 0)
            return null;

        Gump gump = UIManager.GetGumpServer(gumpID);

        if (gump != null && (gump.IsDisposed || gump.LocalSerial != sender))
            gump = null;

        bool mustBeAdded = gump == null;

        if (UIManager.GetGumpCachePosition(gumpID, out Point pos))
        {
            x = pos.X;
            y = pos.Y;
        }
        else
            UIManager.SavePosition(gumpID, new Point(x, y));

        if (mustBeAdded)
            gump = new Gump(world, sender, gumpID)
            {
                X = x,
                Y = y,
                CanMove = true,
                CanCloseWithRightClick = true,
                CanCloseWithEsc = true,
                InvalidateContents = false,
                IsFromServer = true
            };
        else
        {
            //Reusing existing gump, need to clear it out
            gump.Clear();
            gump.CleanUpDisposedChildren();
        }

        var gumpTextBuilder = new StringBuilder(string.Join("\n", lines));

        int group = 0;
        int page = 0;

        bool textBoxFocused = false;

        for (int cnt = 0; cnt < cmdlen; cnt++)
        {
            List<string> gparams = GetCmdParser().GetTokens(cmdlist[cnt], false);

            if (gparams.Count == 0)
                continue;

            string entry = gparams[0];
            gumpTextBuilder.Append(string.Join(" ", gparams)).Append('\n');

            if (string.Equals(entry, "button", StringComparison.InvariantCultureIgnoreCase))
                gump.Add(new Button(gparams), page);
            else if (
                string.Equals(
                    entry,
                    "buttontileart",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
                gump.Add(new ButtonTileArt(gparams), page);
            else if (
                string.Equals(
                    entry,
                    "checkertrans",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                var checkerTrans = new CheckerTrans(gparams);
                gump.Add(checkerTrans, page);
                ApplyTrans(
                    gump,
                    page,
                    checkerTrans.X,
                    checkerTrans.Y,
                    checkerTrans.Width,
                    checkerTrans.Height
                );
            }
            else if (
                string.Equals(entry, "croppedtext", StringComparison.InvariantCultureIgnoreCase)
            )
                gump.Add(new CroppedText(gparams, lines), page);
            else if (
                string.Equals(entry, "tilepicasgumppic", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(entry, "gumppic", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                GumpPic pic;
                bool isVirtue = gparams.Count >= 6
                                && gparams[5].IndexOf(
                                    "virtuegumpitem",
                                    StringComparison.InvariantCultureIgnoreCase
                                ) >= 0;

                if (isVirtue)
                {
                    pic = new VirtueGumpPic(world, gparams);
                    pic.ContainsByBounds = true;

                    string s,
                        lvl;

                    switch (pic.Hue)
                    {
                        case 2403:
                            lvl = "";

                            break;

                        case 1154:
                        case 1547:
                        case 2213:
                        case 235:
                        case 18:
                        case 2210:
                        case 1348:
                            lvl = "Seeker of ";

                            break;

                        case 2404:
                        case 1552:
                        case 2216:
                        case 2302:
                        case 2118:
                        case 618:
                        case 2212:
                        case 1352:
                            lvl = "Follower of ";

                            break;

                        case 43:
                        case 53:
                        case 1153:
                        case 33:
                        case 318:
                        case 67:
                        case 98:
                            lvl = "Knight of ";

                            break;

                        case 2406:
                            if (pic.Graphic == 0x6F)
                                lvl = "Seeker of ";
                            else
                                lvl = "Knight of ";

                            break;

                        default:
                            lvl = "";

                            break;
                    }

                    switch (pic.Graphic)
                    {
                        case 0x69:
                            s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 2);

                            break;

                        case 0x6A:
                            s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 7);

                            break;

                        case 0x6B:
                            s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 5);

                            break;

                        case 0x6D:
                            s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 6);

                            break;

                        case 0x6E:
                            s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 1);

                            break;

                        case 0x6F:
                            s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 3);

                            break;

                        case 0x70:
                            s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 4);

                            break;

                        case 0x6C:
                        default:
                            s = Client.Game.UO.FileManager.Clilocs.GetString(1051000);

                            break;
                    }

                    if (string.IsNullOrEmpty(s))
                        s = "Unknown virtue";

                    pic.SetTooltip(lvl + s, 100);
                }
                else
                    pic = new GumpPic(gparams);

                gump.Add(pic, page);
            }
            else if (
                string.Equals(
                    entry,
                    "gumppictiled",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
                gump.Add(new GumpPicTiled(gparams), page);
            else if (
                string.Equals(entry, "htmlgump", StringComparison.InvariantCultureIgnoreCase)
            )
                gump.Add(new HtmlControl(gparams, lines), page);
            else if (
                string.Equals(entry, "xmfhtmlgump", StringComparison.InvariantCultureIgnoreCase)
            )
                gump.Add(
                    new HtmlControl(
                        int.Parse(gparams[1]),
                        int.Parse(gparams[2]),
                        int.Parse(gparams[3]),
                        int.Parse(gparams[4]),
                        int.Parse(gparams[6]) == 1,
                        int.Parse(gparams[7]) != 0,
                        gparams[6] != "0" && gparams[7] == "2",
                        Client.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[5].Replace("#", ""))),
                        0,
                        true
                    ) { IsFromServer = true },
                    page
                );
            else if (
                string.Equals(
                    entry,
                    "xmfhtmlgumpcolor",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                int color = int.Parse(gparams[8]);

                if (color == 0x7FFF)
                    color = 0x00FFFFFF;

                gump.Add(
                    new HtmlControl(
                        int.Parse(gparams[1]),
                        int.Parse(gparams[2]),
                        int.Parse(gparams[3]),
                        int.Parse(gparams[4]),
                        int.Parse(gparams[6]) == 1,
                        int.Parse(gparams[7]) != 0,
                        gparams[6] != "0" && gparams[7] == "2",
                        Client.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[5].Replace("#", ""))),
                        color,
                        true
                    ) { IsFromServer = true },
                    page
                );
            }
            else if (
                string.Equals(entry, "xmfhtmltok", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                int color = int.Parse(gparams[7]);

                if (color == 0x7FFF)
                    color = 0x00FFFFFF;

                StringBuilder sb = null;

                if (gparams.Count >= 9)
                {
                    sb = new StringBuilder();

                    for (int i = 9; i < gparams.Count; i++)
                    {
                        sb.Append('\t');
                        sb.Append(gparams[i]);
                    }
                }

                gump.Add(
                    new HtmlControl(
                        int.Parse(gparams[1]),
                        int.Parse(gparams[2]),
                        int.Parse(gparams[3]),
                        int.Parse(gparams[4]),
                        int.Parse(gparams[5]) == 1,
                        int.Parse(gparams[6]) != 0,
                        gparams[5] != "0" && gparams[6] == "2",
                        sb == null
                            ? Client.Game.UO.FileManager.Clilocs.GetString(
                                int.Parse(gparams[8].Replace("#", ""))
                            )
                            : Client.Game.UO.FileManager.Clilocs.Translate(
                                int.Parse(gparams[8].Replace("#", "")),
                                sb.ToString().Trim('@').Replace('@', '\t')
                            ),
                        color,
                        true
                    ) { IsFromServer = true },
                    page
                );
            }
            else if (string.Equals(entry, "page", StringComparison.InvariantCultureIgnoreCase))
            {
                if (gparams.Count >= 2)
                    page = int.Parse(gparams[1]);
            }
            else if (
                string.Equals(entry, "resizepic", StringComparison.InvariantCultureIgnoreCase)
            )
                gump.Add(new ResizePic(gparams), page);
            else if (string.Equals(entry, "text", StringComparison.InvariantCultureIgnoreCase))
            {
                if (gparams.Count >= 5)
                    gump.Add(new Label(gparams, lines), page);
            }
            else if (
                string.Equals(
                    entry,
                    "textentrylimited",
                    StringComparison.InvariantCultureIgnoreCase
                )
                || string.Equals(
                    entry,
                    "textentry",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                var textBox = new StbTextBox(gparams, lines);

                if (!textBoxFocused)
                {
                    textBox.SetKeyboardFocus();
                    textBoxFocused = true;
                }

                gump.Add(textBox, page);
            }
            else if (
                string.Equals(entry, "tilepichue", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(entry, "tilepic", StringComparison.InvariantCultureIgnoreCase)
            )
                gump.Add(new StaticPic(gparams), page);
            else if (
                string.Equals(entry, "noclose", StringComparison.InvariantCultureIgnoreCase)
            )
                gump.CanCloseWithRightClick = false;
            else if (
                string.Equals(entry, "nodispose", StringComparison.InvariantCultureIgnoreCase)
            )
                gump.CanCloseWithEsc = false;
            else if (
                string.Equals(entry, "nomove", StringComparison.InvariantCultureIgnoreCase)
            )
                gump.CanMove = false;
            else if (
                string.Equals(entry, "group", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(entry, "endgroup", StringComparison.InvariantCultureIgnoreCase)
            )
                group++;
            else if (string.Equals(entry, "radio", StringComparison.InvariantCultureIgnoreCase))
                gump.Add(new RadioButton(group, gparams, lines), page);
            else if (
                string.Equals(entry, "checkbox", StringComparison.InvariantCultureIgnoreCase)
            )
                gump.Add(new Checkbox(gparams, lines), page);
            else if (
                string.Equals(entry, "tooltip", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                string text = null;

                if (gparams.Count > 2 && gparams[2].Length != 0)
                {
                    string args = gparams[2];

                    for (int i = 3; i < gparams.Count; i++)
                        args += '\t' + gparams[i];

                    if (args.Length == 0)
                    {
                        text = Client.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[1]));
                        Log.Error(
                            $"String '{args}' too short, something wrong with gump tooltip: {text}"
                        );
                    }
                    else
                        text = Client.Game.UO.FileManager.Clilocs.Translate(
                            int.Parse(gparams[1]),
                            args
                        );
                }
                else
                    text = Client.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[1]));

                IGui last =
                    gump.Children.Count != 0 ? gump.Children[gump.Children.Count - 1] : null;

                if (last != null)
                {
                    if (last.HasTooltip)
                    {
                        if (last.Tooltip is string s)
                        {
                            s += '\n' + text;
                            last.SetTooltip(s);
                        }
                    }
                    else
                        last.SetTooltip(text);

                    last.Priority = ClickPriority.High;
                    last.AcceptMouseInput = true;
                }
            }
            else if (
                string.Equals(
                    entry,
                    "itemproperty",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                if (world.ClientFeatures.TooltipsEnabled && gump.Children.Count != 0)
                {
                    gump.Children[gump.Children.Count - 1].SetTooltip(
                        SerialHelper.Parse(gparams[1])
                    );

                    if (
                        uint.TryParse(gparams[1], out uint s)
                        && (!world.OPL.TryGetRevision(s, out uint rev) || rev == 0)
                    )
                        SharedStore.AddMegaCliLocRequest(s);
                }
            }
            else if (
                string.Equals(entry, "noresize", StringComparison.InvariantCultureIgnoreCase)
            ) { }
            else if (
                string.Equals(entry, "mastergump", StringComparison.InvariantCultureIgnoreCase)
            )
                gump.MasterGumpSerial = gparams.Count > 0 ? SerialHelper.Parse(gparams[1]) : 0;
            else if (string.Equals(entry, "picinpichued", StringComparison.InvariantCultureIgnoreCase) ||
                     string.Equals(entry, "picinpicphued", StringComparison.InvariantCultureIgnoreCase) ||
                     string.Equals(entry, "picinpic", StringComparison.InvariantCultureIgnoreCase)
                    )
            {
                if (gparams.Count > 7)
                    gump.Add(new GumpPicInPic(gparams), page);
            }
            else if (string.Equals(entry, "\0", StringComparison.InvariantCultureIgnoreCase))
                //This gump is null terminated: Breaking
                break;
            else if (string.Equals(entry, "gumppichued", StringComparison.InvariantCultureIgnoreCase) ||
                     string.Equals(entry, "gumppicphued", StringComparison.InvariantCultureIgnoreCase))
            {
                if (gparams.Count >= 3)
                    gump.Add(new GumpPic(gparams));
            }
            else if (string.Equals(entry, "togglelimitgumpscale", StringComparison.InvariantCultureIgnoreCase))
            {
                // ??
            }
            else if (string.Equals(entry, "maparea", StringComparison.InvariantCultureIgnoreCase))
            {
                if (gparams.Count >= 10)
                    if (int.TryParse(gparams[1], out int cx) &&
                        int.TryParse(gparams[2], out int cy) &&
                        int.TryParse(gparams[3], out int width) &&
                        int.TryParse(gparams[4], out int height) &&
                        int.TryParse(gparams[5], out int mapindex) &&
                        int.TryParse(gparams[6], out int mapx) &&
                        int.TryParse(gparams[7], out int mapy) &&
                        int.TryParse(gparams[8], out int mapex) &&
                        int.TryParse(gparams[9], out int mapey))
                        gump.Add(
                            new RenderedMapArea(mapindex, new Rectangle(mapx, mapy, mapex - mapx, mapey - mapy), cx, cy,
                                width, height), page);
            }
            else
                Log.Warn($"Invalid Gump Command: \"{gparams[0]}\"");
        }

        gump.PacketGumpText = gumpTextBuilder.ToString();

        if (mustBeAdded)
            UIManager.Add(gump);

        gump.Update();
        gump.SetInScreen();

        if (CUOEnviroment.Debug)
            GameActions.Print(world, $"GumpID: {gumpID}");

        if (ProfileManager.CurrentProfile != null)
            if (gumpID == ProfileManager.CurrentProfile.SOSGumpID) //SOS message gump
                for (int i = 0; i < gump.Children.Count; i++)
                    if (gump.Children[i] is HtmlControl)
                    {
                        string pattern = @"(\d+('N)?('S)?('E)?('W)?)";

                        string[] loc = new string[4];

                        int c = 0;
                        foreach (Match m in Regex.Matches(((HtmlControl)gump.Children[i]).Text, pattern,
                                     RegexOptions.Multiline))
                        {
                            if (c > 3)
                                break;
                            loc[c] = m.Value.Replace("'", "");
                            c++;
                        }

                        if (loc[0] == null || loc[1] == null || loc[2] == null || loc[3] == null)
                            break;

                        int xlong = 0, ylat = 0, xmins = 0, ymins = 0;
                        bool xeast = true, ysouth = true;

                        if (loc[1].Contains("N"))
                            ysouth = false;

                        if (loc[3].Contains("W"))
                            xeast = false;

                        xlong = int.Parse(loc[2]);
                        ylat = int.Parse(loc[0]);
                        xmins = int.Parse(loc[3].Substring(0, loc[3].Length - 1));
                        ;
                        ymins = int.Parse(loc[1].Substring(0, loc[1].Length - 1));
                        Vector3 location = LocationHelpers.ReverseLookup(xlong, ylat, xmins, ymins, xeast, ysouth);
                        GameActions.Print(world,
                            $"If I am on the correct facet I think these coords should be somewhere near.. {location.X} and {location.Y}..");

                        var menu = new MenuButton(25, Color.Black.PackedValue, 0.75f, "Menu")
                        {
                            X = gump.Width - 46, Y = 6
                        };
                        menu.MouseUp += (s, e) =>
                        {
                            menu.ContextMenu?.Show();
                        };

                        menu.ContextMenu = new ContextMenuControl(gump);
                        menu.ContextMenu.Add(new ContextMenuItemEntry("Locate on world map", () =>
                        {
                            WorldMapGump gump = UIManager.GetGump<WorldMapGump>();
                            if (gump == null)
                            {
                                gump = new WorldMapGump(world);
                                UIManager.Add(gump);
                            }

                            gump.GoToMarker((int)location.X, (int)location.Y, true);
                        }));

                        menu.ContextMenu.Add(new ContextMenuItemEntry("Add marker on world map", () =>
                        {
                            WorldMapGump gump = UIManager.GetGump<WorldMapGump>();
                            if (gump == null)
                            {
                                gump = new WorldMapGump(world);
                                UIManager.Add(gump);
                            }

                            gump.AddUserMarker("SOS", (int)location.X, (int)location.Y, world.Map.Index);
                        }));

                        menu.ContextMenu.Add(new ContextMenuItemEntry("Close", () =>
                        {
                            gump.Dispose();
                        }));
                        gump.Add(menu);
                    }

        if (world.Player != null)
            world.Player.LastGumpID = gumpID;

        if (gump.X == 0 && gump.Y == 0)
        {
            gump.CenterXInViewPort();
            gump.CenterYInViewPort();
        }

        NextGumpConfig.Apply(gump);

        return gump;
    }

    public static void ApplyTrans(Gump gump, int current_page, int x, int y, int width, int height)
    {
        int x2 = x + width;
        int y2 = y + height;
        for (int i = 0; i < gump.Children.Count; i++)
        {
            IGui child = gump.Children[i];
            bool canDraw = child.Page == 0 || current_page == child.Page;

            bool overlap =
                x < child.X + child.Width
                && child.X < x2
                && y < child.Y + child.Height
                && child.Y < y2;

            if (canDraw && child.IsVisible && overlap)
                child.Alpha = 0.5f;
        }
    }
}
