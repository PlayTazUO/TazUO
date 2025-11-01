// SPDX-License-Identifier: BSD-2-Clause


using System;
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;
using System.Threading.Tasks;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Configuration;
using ClassicUO.LegionScripting;

namespace ClassicUO.Game.Managers
{
    public sealed class CommandManager
    {
        private readonly Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();
        public Dictionary<string, Action<string[]>> Commands => _commands;

        private readonly World _world;

        public CommandManager(World world)
        {
            _world = world;
        }

        public void Initialize()
        {
            Register("sb", (s)=>ScriptBrowser.Show());

            Register("updateapi", (s) =>
            {
                LegionScripting.LegionScripting.DownloadAPIPy();
            });

            Register
            (
                "info",
                s =>
                {
                    if (_world.TargetManager.IsTargeting)
                    {
                        _world.TargetManager.CancelTarget();
                    }

                    _world.TargetManager.SetTargeting(CursorTarget.SetTargetClientSide, CursorType.Target, TargetType.Neutral);
                }
            );

            Register
            (
                "datetime",
                s =>
                {
                    if (_world.Player != null)
                    {
                        GameActions.Print(_world, string.Format(ResGeneral.CurrentDateTimeNowIs0, DateTime.Now));
                    }
                }
            );

            Register
            (
                "hue",
                s =>
                {
                    if (_world.TargetManager.IsTargeting)
                    {
                        _world.TargetManager.CancelTarget();
                    }

                    _world.TargetManager.SetTargeting(CursorTarget.HueCommandTarget, CursorType.Target, TargetType.Neutral);
                }
            );


            Register
            (
                "debug",
                s =>
                {
                    CUOEnviroment.Debug = !CUOEnviroment.Debug;

                }
            );

            Register
            (
                "colorpicker",
                s =>
                {
                    UIManager.Add(new UI.Gumps.ModernColorPicker(_world, null, 8787));

                }
            );

            Register("cast", s =>
            {
                string spell = "";
                for (int i = 1; i < s.Length; i++)
                {
                    spell += s[i] + " ";
                }
                spell = spell.Trim();

                if (SpellDefinition.TryGetSpellFromName(spell, out SpellDefinition spellDef))
                    GameActions.CastSpell(spellDef.ID);
            });

            var sortSkills = new List<Skill>(_world.Player.Skills);

            Register("skill", s =>
            {
                string skill = "";
                for (int i = 1; i < s.Length; i++)
                {
                    skill += s[i] + " ";
                }
                skill = skill.Trim().ToLower();

                if (skill.Length > 0)
                {
                    for (int i = 0; i < _world.Player.Skills.Length; i++)
                    {
                        if (_world.Player.Skills[i].Name.ToLower().Contains(skill))
                        {
                            GameActions.UseSkill(_world.Player.Skills[i].Index);
                            break;
                        }
                    }
                }
            });

            Register("version", s => { UIManager.Add(new VersionHistory(_world)); });
            Register("rain", s => { _world.Weather.Generate(WeatherType.WT_RAIN, 30, 75); });

            Register("marktile", s =>
            {
                if (s.Length > 1 && s[1] == "-r")
                {
                    if (s.Length == 2)
                    {
                        TileMarkerManager.Instance.RemoveTile(_world.Player.X, _world.Player.Y, _world.Map.Index);
                    }
                    else if (s.Length == 4)
                    {
                        if (int.TryParse(s[2], out int x))
                            if (int.TryParse(s[3], out int y))
                                TileMarkerManager.Instance.RemoveTile(x, y, _world.Map.Index);
                    }
                    else if (s.Length == 5)
                    {
                        if (int.TryParse(s[2], out int x))
                            if (int.TryParse(s[3], out int y))
                                if (int.TryParse(s[4], out int m))
                                    TileMarkerManager.Instance.RemoveTile(x, y, m);
                    }
                }
                else
                {
                    if (s.Length == 1)
                    {
                        TileMarkerManager.Instance.AddTile(_world.Player.X, _world.Player.Y, _world.Map.Index, 32);
                    }
                    else if (s.Length == 2)
                    {
                        if (ushort.TryParse(s[1], out ushort h))
                            TileMarkerManager.Instance.AddTile(_world.Player.X, _world.Player.Y, _world.Map.Index, h);
                    }
                    else if (s.Length == 4)
                    {
                        if (int.TryParse(s[1], out int x))
                            if (int.TryParse(s[2], out int y))
                                if (ushort.TryParse(s[3], out ushort h))
                                    TileMarkerManager.Instance.AddTile(x, y, _world.Map.Index, h);
                    }
                    else if (s.Length == 5)
                    {
                        if (int.TryParse(s[1], out int x))
                            if (int.TryParse(s[2], out int y))
                                if (int.TryParse(s[3], out int m))
                                    if (ushort.TryParse(s[4], out ushort h))
                                        TileMarkerManager.Instance.AddTile(x, y, m, h);
                    }
                }
            });

            Register("radius", s =>
            {
                ///-radius distance hue
                if (s.Length == 1)
                    ProfileManager.CurrentProfile.DisplayRadius ^= true;
                if (s.Length > 1)
                {
                    if (int.TryParse(s[1], out int dist))
                        ProfileManager.CurrentProfile.DisplayRadiusDistance = dist;
                    ProfileManager.CurrentProfile.DisplayRadius = true;
                }
                if (s.Length > 2)
                    if (ushort.TryParse(s[2], out ushort h))
                        ProfileManager.CurrentProfile.DisplayRadiusHue = h;
            });

            Register("paperdoll", (s) =>
            {
                if (ProfileManager.CurrentProfile.UseModernPaperdoll)
                {
                    UIManager.Add(new PaperDollGump(_world, _world.Player, true));
                }
                else
                {
                    UIManager.Add(new ModernPaperdoll(_world, _world.Player));
                }

            });

            Register("optlink", (s) =>
            {
                ModernOptionsGump g = UIManager.GetGump<ModernOptionsGump>();
                if (s.Length > 1)
                {
                    if (g != null)
                    {
                        g.GoToPage(s[1]);
                    }
                    else
                    {
                        UIManager.Add(g = new ModernOptionsGump(_world));
                        g.GoToPage(s[1]);
                    }
                }
                else
                {
                    if (g != null)
                    {
                        GameActions.Print(_world, g.GetPageString());
                    }
                }
            });

            Register("genspelldef", (s) =>
            {
                Task.Run(() => SpellDefinition.SaveAllSpellsToJson(_world));
            });

            Register("setinscreen", (s) =>
            {
                for (LinkedListNode<Gump> last = UIManager.Gumps.Last; last != null; last = last.Previous)
                {
                    Gump c = last.Value;

                    if (!c.IsDisposed)
                    {
                        c.SetInScreen();
                    }
                }
            });

            Register("updatedebug", (s) =>
            {
                UIManager.Add(new UI.Gumps.UpdateTimerViewer(_world));
            });

            Register("artbrowser", (s) => { UIManager.Add(new ArtBrowserGump(_world)); });

            Register("animbrowser", (s) => { UIManager.Add(new AnimBrowser(_world)); });

            Register("syncfps", (_) =>
            {
                Settings.GlobalSettings.FPS = GameController.SupportedRefreshRate;
                Settings.GlobalSettings.Save();
                Client.Game.SetRefreshRate(Settings.GlobalSettings.FPS);
                GameActions.Print($"FPS Limit updated to: {Settings.GlobalSettings.FPS}", 62);
            });

            Register("dressagent", (s) => DressAgentManager.Instance?.DressAgentCommand(s));
            Register("organize", (s) => OrganizerAgent.Instance?.OrganizerCommand(s));
            Register("organizer", (s) => OrganizerAgent.Instance?.OrganizerCommand(s));
            Register("organizerlist", (s) => OrganizerAgent.Instance?.ListOrganizers());
            Register("reply", (s) =>
            {
                if (DiscordManager.Instance.LastPrivateMessage == null)
                {
                    GameActions.Print("No message to reply to.", 32);
                    return;
                }

                string msg = "";
                for (int i = 1; i < s.Length; i++)
                    msg += s[i] + " ";

                DiscordManager.Instance.SendDm(DiscordManager.Instance.LastPrivateMessage.ChannelId(), msg);
            });
        }


        public void Register(string name, Action<string[]> callback)
        {
            name = name.ToLower();

            if (!_commands.ContainsKey(name))
            {
                _commands.Add(name, callback);
            }
            else
            {
                Log.Error($"Attempted to register command: '{name}' twice.");
            }
        }

        public void UnRegister(string name)
        {
            name = name.ToLower();

            if (_commands.ContainsKey(name))
            {
                _commands.Remove(name);
            }
        }

        public void UnRegisterAll() => _commands.Clear();

        public void Execute(string name, params string[] args)
        {
            name = name.ToLower();

            if (_commands.TryGetValue(name, out Action<string[]> action))
            {
                action.Invoke(args);
            }
            else
            {
                GameActions.Print(_world, string.Format(Language.Instance.ErrorsLanguage.CommandNotFound, name));
                Log.Warn($"Command: '{name}' not exists");
            }
        }

        public void OnHueTarget(Entity entity)
        {
            if (entity != null)
            {
                _world.TargetManager.Target(entity);
            }

            Mouse.LastLeftButtonClickTime = 0;
            GameActions.Print(_world, string.Format(ResGeneral.ItemID0Hue1, entity.Graphic, entity.Hue));
        }
    }
}
