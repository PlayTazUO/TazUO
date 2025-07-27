using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class VersionHistory : Gump
    {
        private static string[] updateTexts = {
            "/c[white][4.0.1]/cd\n" +
            "- Added hud toggle to macros and assistant menu\n" +
            "- Added resync macro\n" +
            "- Added a new macro type to toggle forced house transparency\n" +
            "- Added some backend UI improvements for future use\n" +
            "- Added option to not make enemies gray\n" +
            "\n",

            "/c[white][4.0.0]/cd\n" +
            "- Prevent autoloot, move item queue from moving items while you are holding something.\n" +
            "- Change multi item move to use shared move item queue\n" +
            "- Prevent closing containers when changing facets\n" +
            "- Added Create macro button for legion scripts\n" +
            "- Potential Crash fix from CUO\n" +
            "- Python API changes\n" +
            "- Change how skill message frequency option works - fuzzlecutter\n" +
            "- Added an option to default to old container style with the option to switch to grid container style\n" +
            "- Added option to remove System prefix in journal\n" +
            "- Minor bug fixes\n" +
            "- Spellbar!\n" +
            "- Implemented Async networking\n",

            "/c[white][3.32.0]/cd\n" +
            "- Added simple progress bar control for Python API gumps.\n" +
            "- Generate user friendly html crash logs and open them on crash\n" +
            "- Some fixes for nearby corpse loot gump\n" +
            "- Very slightly increased minimum distance to start dragging a gump. Hopefully it should prevent accidental drags instead of clicks\n" +
            "- Nearby loot gump now stays open after relogging\n" +
            "- Moved some assistant-like options to their own menu.\n" +
            "- XML Gumps save locked status now(Ctrl + Alt + Click to lock)\n" +
            "- Python API created gumps will automatically close when the script stops, unless marked keep open." +
            "- Various bug fixes\n",

            "/c[white][3.31.0]/cd\n" +
            "- Fix for Python API EquipItem\n" +
            "- Fix for legion scripting useability commands\n" +
            "- Added basic scavenger agent(Uses autoloot)\n" +
            "- Nearby item gump and grid container quick loot now use move item queue\n" +
            "- Combine duplicate system messages\n" +
            "- Default visual spell indicator setup embedded now\n" +
            "- Various bug fixes\n",

            "/c[white][3.30.0]/cd\n" +
            "- Implementing Discord Social features\n" +
            "- Added more python API methods\n" +
            "- Better python API error handling\n" +
            "- Other minor bug fixes",

            "/c[white][3.29.0]/cd\n" +
                "- Moved tooltip override options into main menu\n"+
                "- Expanded Python API\n"+
                "- Prevent moving gumps outside the client window\n"+
                "- Reworked internal TTF fonts for better performance\n"+
                "- Fixed a bug in tooltips, likely not noticable but should be a significant performance boost while a tooltip is shown.\n"+
                "- Added -artbrowser and -animbrowser commands\n"+
                "- Added option to disable targeting grid containers directly\n"+
                "- Added some new fonts in\n" +
                "- Added option to disable controller\n" +
                "- Added some standard python libs in for python scripting\n" +

                "- Various bug fixes\n",

            "/c[white][3.28.0]/cd\n" +
                "- Added auto buy and sell agents\n" +
                "- Added Python scripting language support to legion scripting\n" +
                "- Added graphic replacement option\n" +
                "- Better item stacking in original containers while using grid containers \n" +
                "- Added a hotkeys page in options\n" +
                "- Improved autolooting\n",

            "/c[white][3.27.0]/cd\n" +
                "- Added forced tooltip option for pre-tooltip servers\n" +
                "- Added global scaling\n" +
                "- Add regex matching for autoloot\n" +
                "- Improved modern shop gump asthetics\n" +
                "- Counter bars can now be assigned spells\n" +
                "- Removed unused scripting system\n" +
                "- Added adjustable turn delay\n",

            "/c[white][3.26.1]/cd\n" +
                "- Fix for replygump command in legion scripting\n" +

            "/c[white][3.26.0]/cd\n" +
                "- Added optional regex to tooltip overrides\n" +
                "- Minor improvements in tooltip overrides\n" +
                "- Fix whitespace error during character creation\n" +
                "- Nearby item gump will close if moved, or 30 seconds has passed\n" +

            "/c[white][3.25.2]/cd\n" +
                "- Nearby item gump moved to macros\n" +

            "/c[white][3.25.1]/cd\n" +
                "- Added DPS meter\n" +
                "- Legion Scripting bug fix\n" +

            "/c[white][3.25.0]/cd\n" +
                "- Added the Legion scripting engine, see wiki for details\n" +
                "- Updated some common default settings that are usually used\n" +
                "- More controller QOL improvements\n" +
                "- Added tooltips for counterbar items\n" +
                "- Added a nearby items feature(See wiki for details)\n" +
                "- Various bug fixes\n" +

            "/c[white][3.24.2]/cd\n" +
                "- Fix Invisible items in Osi New Legacy Server\n" +
                "- Fix added more slots for show items layer in paperdoll \n" +
                "- Add scrollbar to cooldowns in options  \n" +
                "- Created progress bar for auto loot \n" +
                "- Fix skill progress bars \n" +
                "- Fix scroll area in autoloot options \n" +
                "- Create gump toggle mcros gumps for controller gameplay \n" +
                "- Save position of durability gump while in game \n" +

            "/c[white][3.24.2]/cd\n" +
                "- Fix Render Maps for Server Osi New Legacy\n" +
                "- Fix Ignore List \n" +
                "- Fix Big Tags in Weapons props \n" +
                "- Fix Pathfinding algorithm using Z more efficiently from ghzatomic \n" +

             "/c[white][3.24.1]/cd\n" +
                "- Fix for Modern Paperdoll not loading\n" +
                "- Fix Using Weapons Abilitys\n" +

            "/c[white][3.24.0]/cd\n" +
                "- Updated the algorithm for reading mul encryption\n" +
                "- Fix scrolling in the infobar manager\n" +
                "- Fix ignoring player in chat too\n" +
                "- Add auto avoid obstacles",

            "/c[white][3.23.2]/cd\n" +
                "- Fixed Disarm and Stun ability AOS",

            "/c[white][3.23.1]/cd\n" +
                "- Fixed Weird lines with nameplate",

            "/c[white][3.23.0]/cd\n" +
                "- Nameplate healthbar poison and invul/paralyzed colors from Elderwyn\n" +
                "- Target indiciator option from original client from Elderwyn\n" +
                "- Advanced skill gump improvements from Elderwyn",

            "/c[white][3.22.0]/cd\n" +
                "- Spell book icon fix\n" +
                "- Add option to add treasure maps as map markers instead of goto only\n" +
                "- Added the same option for SOS messages\n" +
                "- Fix text height for nameplates\n" +
                "- Added option to disable auto follow",

            "/c[white][3.21.4]/cd\n" +
                "- Various bug fixes\n" +
                "- Removed gump closing animation. Too many unforeseen issues with it.",

            "/c[white][3.21.3]/cd\n" +
            "- Changes to improve gump closing animations",

            "/c[white][3.21.2]/cd\n" +
                "- A bugfix release for 3.21 causing crashes",

            "/c[white][3.21.0]/cd\n" +
                "- A few bug fixes\n" +
                "- A few fixes from CUO\n" +
                "- Converted nameplates to use TTF fonts\n" +
                "- Added an available client commands gump\n" +
                "- World map alt lock now works, and middle mouse click will toggle freeview",

            "/c[white][3.20.0]/cd\n" +
                "- Being frozen wont cancel auto follow\n" +
                "- Fix from CUO for buffs\n" +
                "- Add ability to load custom spell definitions from an external file\n" +
                "- Customize the options gump via ui file\n" +
                "- Added saveposition tag for xml gumps\n" +
                "- Can now open multiple journals\n",

            "/c[white][3.19.0]/cd\n" +
                "- SOS Gump ID configurable in settings\n" +
                "- Added macro option to execute a client-side command\n" +
                "- Added a command doesn't exist message\n" +
                "- Follow party members on world map option\n" +
                "- Added option to override party member body hues\n" +
                "- Bug fix",

             "/c[white][3.18.0]/cd\n" +
                "- Added a language file that will contain UI text for easy language translations\n",

             "/c[white][3.17.0]/cd\n" +
                "- Added original paperdoll to customizable gump system\n" +
                "- Imroved script loading time",

             "/c[white][3.16.0]/cd\n" +
                "- Some small improvements for input boxes and the new option menu\n" +
                "- Added player position offset option in TazUO->Misc\n" +
                "- Fix for health indicator percentage\n" +
                "- Fix tooltip centered text\n" +
                "- Added a modding system almost identical to ServUO's script system\n" +
                "- Added macros to use items from your counter bar\n" +
                "- Simple auto loot improvements\n" +
                "- Hold ctrl and drop an item anywhere on the game window to drop it",

            "/c[white][3.15.0]/cd\n" +
                "- Mouse interaction for overhead text can be disabled\n" +
                "- Visable layers option added in Options->TazUO\n" +
                "- Added custom XML Gumps -> see wiki\n" +
                "- Added some controller support for movement and macros",

            "/c[white][3.14.0]/cd\n" +
                "- New options menu\n" +
                "- Small null ref bug fix\n" +
                "- No max width on item count text for smaller scaling\n" +
                "- Auto loot shift-click will no long work if you have shift for context menu or split stacks.\n" +
                "- Skill progress bars will save their position if you move them\n" +
                "- Changed skill progress bars to a queue instead of all showing at once\n" +
                "- Fix art png loading\n" +
                "- Added /c[green]-paperdoll/cd command\n" +
                "- Added an auto resync option under Options->TazUO->Misc\n" +
                "- Alt + Click paperdoll preview in modern paperdoll to copy a screenshot of it\n" +
                "- Added `both` option to auto close gumps range or dead\n" +
                "- Added shift + double click to advanced shop gump to buy/sell all of that item\n" +
                "- Added use one health bar for last attack option\n" +
                "- Added `-optlink` command",

            "/c[white][3.13.0]/cd\n" +
                "- Fix item unintentional stacking\n" +
                "- Potential small bug fix\n" +
                "- Option to close anchored healthbars automatically\n" +
                "- Added optional freeze on cast to spell indicator system\n" +
                "- Save server side gump positions\n" +
                "- Added addition equipment slots to the original paperdoll gump",

            "/c[white][3.12.0]/cd\n" +
                "- Added Exclude self to advanced nameplate options\n" +
                "- Bug fix for spell indicator loading\n" +
                "- Added override profile for same server characters only\n",

            "/c[white][3.11.0]/cd\n" +
                "- Modern shop gump fix\n" +
                "- Pull in latest changes from CUO\n" +
                "- Update client-side version checking\n" +
                "- Infobar bug fixes\n" +
                "- Other small bug fixes\n" +
                "- Modern paperdoll being anchored will be remembered now\n" +
                "- Added an option for Cooldown bars to use the position of the last moved bar\n" +
                "- Added advanced nameplate options\n" +
                "- Moved TTF Font settings to their own category\n" +
                "- Journal tabs are now fully customizable",

            "/c[white][3.10.1]/cd\n" +
                "- Bug fix for floating damage numbers\n" +
                "- Bug fix for health line color\n" +
                "- Fix skill progress bar positioning\n",

            "/c[white][3.10.0]/cd\n" +
                "- Added the option to download a spell indicator config from an external source\n" +
                "- Added a simple auto loot system\n" +
                "- Updated to ClassicUO's latest version\n" +
                "- Auto sort is container specific now\n" +
                "- InfoBar can now be resized and is using the new font rendering system\n" +
                "- InfoBar font and font size can be customized now (TazUO->Misc)\n" +
                "- Journal will now remember the last tab you were on\n" +
                "- Upgraded item comparisons, see wiki on tooltip overrides for more info\n" +
                "- Spell indicators can now be overridden with a per-character config",

            "\n\n/c[white]For further history please visit our discord."
        };

        public VersionHistory() : base(0, 0)
        {
            Width = 400;
            Height = 500;
            CanCloseWithRightClick = true;
            CanMove = true;

            BorderControl bc = new BorderControl(0, 0, Width, Height, 36);
            bc.T_Left = 39925;
            bc.H_Border = 39926;
            bc.T_Right = 39927;
            bc.V_Border = 39928;
            bc.V_Right_Border = 39930;
            bc.B_Left = 39931;
            bc.B_Right = 39933;
            bc.H_Bottom_Border = 39932;

            Add(new GumpPicTiled(39929) { X = bc.BorderSize, Y = bc.BorderSize, Width = Width - (bc.BorderSize * 2), Height = Height - (bc.BorderSize * 2) });

            Add(bc);

            TextBox _;
            Add(_ = TextBox.GetOne(Language.Instance.TazuoVersionHistory, TrueTypeLoader.EMBEDDED_FONT, 30, Color.White, TextBox.RTLOptions.DefaultCentered(Width)));
            _.Y = 5;

            int y = _.Y + _.Height + 5;
            Add(_ = TextBox.GetOne(Language.Instance.CurrentVersion + CUOEnviroment.Version.ToString(), TrueTypeLoader.EMBEDDED_FONT, 20, Color.Orange, TextBox.RTLOptions.DefaultCentered(Width)));
            _.Y = y;

            ScrollArea scroll = new ScrollArea(10, _.Y + _.Height, Width - 20, Height - (_.Y + _.Height) - 20, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(new AlphaBlendControl(0.45f) { Width = scroll.Width, Height = scroll.Height, X = scroll.X, Y = scroll.Y });

            y = 0;
            foreach (string s in updateTexts)
            {
                scroll.Add(_ = TextBox.GetOne(s, TrueTypeLoader.EMBEDDED_FONT, 15, Color.Orange, TextBox.RTLOptions.Default(scroll.Width - scroll.ScrollBarWidth())));
                _.Y = y;
                _.AcceptMouseInput = false;
                y += _.Height + 10;
            }

            Add(scroll);


            HitBox _hit;
            _ = TextBox.GetOne(Language.Instance.TazUOWiki, TrueTypeLoader.EMBEDDED_FONT, 15, Color.Orange, TextBox.RTLOptions.Default(200));
            _.X = 25;
            _.Y = Height - 20;
            Add(_);

            Add(_hit = new HitBox(_.X, _.Y, _.MeasuredSize.X, _.MeasuredSize.Y));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://github.com/PlayTazUO/TazUO/wiki");
            };

            _ = TextBox.GetOne(Language.Instance.TazUODiscord, TrueTypeLoader.EMBEDDED_FONT, 15, Color.Orange, TextBox.RTLOptions.Default(200));
            _.X = 280;
            _.Y = Height - 20;
            Add(_);

            Add(_hit = new HitBox(_.X, _.Y, _.MeasuredSize.X, _.MeasuredSize.Y));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://discord.gg/QvqzkB95G4");
            };

            CenterXInViewPort();
            CenterYInViewPort();
        }
    }
}
