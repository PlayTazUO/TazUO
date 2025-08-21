using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ClassicUO.Game.Data;
using ClassicUO.Utility;

namespace ClassicUO.LegionScripting
{
    /// <summary>
    /// Records user actions and generates Python script code
    /// </summary>
    public class ScriptRecorder
    {
        private static ScriptRecorder _instance;
        private static readonly object _lock = new object();

        public static ScriptRecorder Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ScriptRecorder();
                    }
                }
                return _instance;
            }
        }

        private readonly List<RecordedAction> _recordedActions = new List<RecordedAction>();
        private readonly object _actionsLock = new object();
        private bool _isRecording = false;
        private bool _isPaused = false;
        private uint _startTime = 0;
        private uint _lastActionTime = 0;
        private int _lastPlayerX = -1;
        private int _lastPlayerY = -1;
        private Direction _lastDirection = Direction.North;

        public bool IsRecording => _isRecording;
        public bool IsPaused => _isPaused;
        public int ActionCount
        {
            get
            {
                lock (_actionsLock)
                {
                    return _recordedActions.Count;
                }
            }
        }

        public uint RecordingDuration => _isRecording ? (Time.Ticks - _startTime) : 0;

        public event EventHandler RecordingStateChanged;
        public event EventHandler<RecordedAction> ActionRecorded;

        private ScriptRecorder() { }

        public void StartRecording()
        {
            lock (_actionsLock)
            {
                _isRecording = true;
                _isPaused = false;
                _startTime = Time.Ticks;
                _lastActionTime = _startTime;
                _recordedActions.Clear();
            }
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void StopRecording()
        {
            lock (_actionsLock)
            {
                _isRecording = false;
                _isPaused = false;
            }
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void PauseRecording()
        {
            if (_isRecording)
            {
                _isPaused = true;
                RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ResumeRecording()
        {
            if (_isRecording && _isPaused)
            {
                _isPaused = false;
                _lastActionTime = Time.Ticks; // Reset timing for next action
                RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ClearRecording()
        {
            lock (_actionsLock)
            {
                _recordedActions.Clear();
            }
        }

        public void RecordAction(string actionType, Dictionary<string, object> parameters)
        {
            if (!_isRecording || _isPaused)
                return;

            uint currentTime = Time.Ticks;
            uint delayFromPrevious = currentTime - _lastActionTime;

            var action = new RecordedAction(actionType, parameters, currentTime, delayFromPrevious);

            lock (_actionsLock)
            {
                _recordedActions.Add(action);
            }

            _lastActionTime = currentTime;
            ActionRecorded?.Invoke(this, action);
        }

        // Specific recording methods for common actions
        public void RecordWalk(Direction direction)
        {
            RecordAction("walk", new Dictionary<string, object> { { "direction", direction } });
        }

        public void RecordRun(Direction direction)
        {
            RecordAction("run", new Dictionary<string, object> { { "direction", direction } });
        }

        public void RecordUseItem(uint serial)
        {
            RecordAction("useitem", new Dictionary<string, object> { { "serial", serial } });
        }

        public void RecordCastSpell(string spellName)
        {
            RecordAction("cast", new Dictionary<string, object> { { "spell", spellName } });
        }

        public void RecordSay(string message)
        {
            RecordAction("say", new Dictionary<string, object> { { "message", message } });
        }

        public void RecordDragDrop(uint fromSerial, uint toSerial, int amount = -1, int x = -1, int y = -1)
        {
            var parameters = new Dictionary<string, object>
            {
                { "from", fromSerial },
                { "to", toSerial }
            };

            if (amount > 0) parameters["amount"] = amount;
            if (x >= 0) parameters["x"] = x;
            if (y >= 0) parameters["y"] = y;

            RecordAction("dragdrop", parameters);
        }

        public void RecordTarget(uint serial)
        {
            RecordAction("target", new Dictionary<string, object> { { "serial", serial } });
        }

        public void RecordTargetLocation(ushort x, ushort y, short z, ushort graphic = 0)
        {
            var parameters = new Dictionary<string, object>
            {
                { "x", x },
                { "y", y },
                { "z", z }
            };

            if (graphic > 0) parameters["graphic"] = graphic;

            RecordAction("targetlocation", parameters);
        }

        public void RecordCloseContainer(uint serial, string containerType = "container")
        {
            RecordAction("closecontainer", new Dictionary<string, object>
            {
                { "serial", serial },
                { "type", containerType }
            });
        }

        public void UpdatePlayerPosition(int x, int y, Direction direction, bool isRunning)
        {
            if (!_isRecording || _isPaused)
                return;

            // Only record if position actually changed
            if (_lastPlayerX != -1 && _lastPlayerY != -1 &&
                (_lastPlayerX != x || _lastPlayerY != y))
            {
                if (isRunning)
                    RecordRun(direction);
                else
                    RecordWalk(direction);
            }

            _lastPlayerX = x;
            _lastPlayerY = y;
            _lastDirection = direction;
        }

        public void RecordAttack(uint serial)
        {
            RecordAction("attack", new Dictionary<string, object> { { "serial", serial } });
        }

        public void RecordBandageSelf()
        {
            RecordAction("bandageself", new Dictionary<string, object>());
        }

        public void RecordContextMenu(uint serial, ushort index)
        {
            RecordAction("contextmenu", new Dictionary<string, object>
            {
                { "serial", serial },
                { "index", index }
            });
        }

        public void RecordUseSkill(string skillName)
        {
            RecordAction("useskill", new Dictionary<string, object> { { "skill", skillName } });
        }

        public void RecordEquipItem(uint serial, Layer layer)
        {
            RecordAction("equipitem", new Dictionary<string, object>
            {
                { "serial", serial },
                { "layer", layer.ToString() }
            });
        }

        public void RecordReplyGump(uint gumpId, int button, uint[] switches = null, Tuple<ushort, string>[] entries = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "gumpid", gumpId },
                { "button", button }
            };

            if (switches != null && switches.Length > 0)
                parameters["switches"] = string.Join(",", switches);

            if (entries != null && entries.Length > 0)
                parameters["entries"] = string.Join(";", entries.Select(e => $"{e.Item1}:{e.Item2}"));

            RecordAction("replygump", parameters);
        }

        public void RecordVendorBuy(uint vendorSerial, Tuple<uint, ushort>[] items)
        {
            var parameters = new Dictionary<string, object>
            {
                { "vendor", vendorSerial },
                { "items", string.Join(";", items.Select(i => $"{i.Item1}:{i.Item2}")) }
            };

            RecordAction("vendorbuy", parameters);
        }

        public void RecordVendorSell(uint vendorSerial, Tuple<uint, ushort>[] items)
        {
            var parameters = new Dictionary<string, object>
            {
                { "vendor", vendorSerial },
                { "items", string.Join(";", items.Select(i => $"{i.Item1}:{i.Item2}")) }
            };

            RecordAction("vendorsell", parameters);
        }

        public void RecordMsg(string message)
        {
            RecordAction("msg", new Dictionary<string, object> { { "message", message } });
        }

        public void RecordPartyMsg(string message)
        {
            RecordAction("partymsg", new Dictionary<string, object> { { "message", message } });
        }

        public void RecordGuildMsg(string message)
        {
            RecordAction("guildmsg", new Dictionary<string, object> { { "message", message } });
        }

        public void RecordAllyMsg(string message)
        {
            RecordAction("allymsg", new Dictionary<string, object> { { "message", message } });
        }

        public void RecordWhisperMsg(string message)
        {
            RecordAction("whispermsg", new Dictionary<string, object> { { "message", message } });
        }

        public void RecordYellMsg(string message)
        {
            RecordAction("yellmsg", new Dictionary<string, object> { { "message", message } });
        }

        public void RecordEmoteMsg(string message)
        {
            RecordAction("emotemsg", new Dictionary<string, object> { { "message", message } });
        }

        public void RecordUseType(uint graphic, ushort hue, uint container)
        {
            var parameters = new Dictionary<string, object>
            {
                { "graphic", graphic },
                { "hue", hue },
                { "container", container }
            };

            RecordAction("usetype", parameters);
        }

        public void RecordMount(uint serial)
        {
            RecordAction("mount", new Dictionary<string, object> { { "serial", serial } });
        }

        public void RecordDismount()
        {
            RecordAction("dismount", new Dictionary<string, object>());
        }

        public void RecordTurn(string direction)
        {
            RecordAction("turn", new Dictionary<string, object> { { "direction", direction } });
        }

        public void RecordRename(uint serial, string name)
        {
            RecordAction("rename", new Dictionary<string, object>
            {
                { "serial", serial },
                { "name", name }
            });
        }

        public void RecordToggleAbility(string ability)
        {
            RecordAction("toggleability", new Dictionary<string, object> { { "ability", ability } });
        }

        public void RecordVirtue(string virtue)
        {
            RecordAction("virtue", new Dictionary<string, object> { { "virtue", virtue } });
        }

        public void RecordSetSkillLock(string skill, string lockState)
        {
            RecordAction("setskilllock", new Dictionary<string, object>
            {
                { "skill", skill },
                { "state", lockState }
            });
        }

        public void RecordSetStatLock(string stat, string lockState)
        {
            RecordAction("setstatlock", new Dictionary<string, object>
            {
                { "stat", stat },
                { "state", lockState }
            });
        }

        public List<RecordedAction> GetRecordedActions()
        {
            lock (_actionsLock)
            {
                return new List<RecordedAction>(_recordedActions);
            }
        }

        public void RemoveActionAt(int index)
        {
            lock (_actionsLock)
            {
                if (index >= 0 && index < _recordedActions.Count)
                {
                    _recordedActions.RemoveAt(index);
                }
            }
        }

        public void SwapActions(int index1, int index2)
        {
            lock (_actionsLock)
            {
                if (index1 >= 0 && index1 < _recordedActions.Count &&
                    index2 >= 0 && index2 < _recordedActions.Count &&
                    index1 != index2)
                {
                    var temp = _recordedActions[index1];
                    _recordedActions[index1] = _recordedActions[index2];
                    _recordedActions[index2] = temp;
                }
            }
        }

        public string GenerateScript(bool includePauses = true)
        {
            List<RecordedAction> actions;
            lock (_actionsLock)
            {
                actions = new List<RecordedAction>(_recordedActions);
            }

            if (actions.Count == 0)
                return "# No actions recorded";

            var script = new StringBuilder();
            script.AppendLine("# Generated Legion Py Script");
            script.AppendLine("# Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            script.AppendLine();
            script.AppendLine("import API");
            script.AppendLine("import time");
            script.AppendLine();

            bool firstAction = true;
            foreach (var action in actions)
            {
                // Add timing delay (skip for first action)
                if (includePauses && !firstAction && action.DelayFromPrevious > 100) // Only add delays > 100ms
                {
                    double delaySeconds = action.DelayFromPrevious / 1000.0;
                    script.AppendLine($"time.sleep({delaySeconds:F2})");
                }
                firstAction = false;

                // Generate API call based on action type
                switch (action.ActionType.ToLower())
                {
                    case "walk":
                        if (action.Parameters.TryGetValue("direction", out object walkDir))
                            script.AppendLine($"API.Walk(\"{Utility.GetDirectionString((Direction)walkDir)}\")");
                        break;

                    case "run":
                        if (action.Parameters.TryGetValue("direction", out object runDir))
                            script.AppendLine($"API.Run(\"{Utility.GetDirectionString((Direction)runDir)}\")");
                        break;

                    case "useitem":
                        if (action.Parameters.TryGetValue("serial", out object serial))
                            script.AppendLine($"API.UseObject(0x{serial:X8})");
                        break;

                    case "cast":
                        if (action.Parameters.TryGetValue("spell", out object spell))
                            script.AppendLine($"API.Cast(\"{spell}\")");
                        break;

                    case "dragdrop":
                        if (action.Parameters.TryGetValue("from", out object from) &&
                            action.Parameters.TryGetValue("to", out object to))
                        {
                            string dragDropCall = $"API.DragDrop(0x{from:X8}, 0x{to:X8}";

                            if (action.Parameters.TryGetValue("amount", out object amount))
                                dragDropCall += $", {amount}";
                            else
                                dragDropCall += ", -1";

                            if (action.Parameters.TryGetValue("x", out object x) &&
                                action.Parameters.TryGetValue("y", out object y))
                                dragDropCall += $", {x}, {y}";

                            dragDropCall += ")";
                            script.AppendLine(dragDropCall);
                        }
                        break;

                    case "target":
                        if (action.Parameters.TryGetValue("serial", out object targetSerial))
                            script.AppendLine($"API.Target(0x{targetSerial:X8})");
                        break;

                    case "targetlocation":
                        if (action.Parameters.TryGetValue("x", out object targX) &&
                            action.Parameters.TryGetValue("y", out object targY) &&
                            action.Parameters.TryGetValue("z", out object targZ))
                        {
                            if (action.Parameters.TryGetValue("graphic", out object graphic))
                                script.AppendLine($"API.Target({targX}, {targY}, {targZ}, {graphic})");
                            else
                                script.AppendLine($"API.Target({targX}, {targY}, {targZ})");
                        }
                        break;

                    case "attack":
                        if (action.Parameters.TryGetValue("serial", out object attackSerial))
                            script.AppendLine($"API.Attack(0x{attackSerial:X8})");
                        break;

                    case "bandageself":
                        script.AppendLine("API.BandageSelf()");
                        break;

                    case "contextmenu":
                        if (action.Parameters.TryGetValue("serial", out object contextSerial) &&
                            action.Parameters.TryGetValue("index", out object contextIndex))
                            script.AppendLine($"API.ContextMenu(0x{contextSerial:X8}, {contextIndex})");
                        break;

                    case "useskill":
                        if (action.Parameters.TryGetValue("skill", out object skill))
                            script.AppendLine($"API.UseSkill(\"{skill}\")");
                        break;

                    case "equipitem":
                        if (action.Parameters.TryGetValue("serial", out object equipSerial))
                            script.AppendLine($"API.EquipItem(0x{equipSerial:X8})");
                        break;

                    case "replygump":
                        if (action.Parameters.TryGetValue("button", out object gumpButton))
                        {
                            if (action.Parameters.TryGetValue("gumpid", out object gumpId))
                                script.AppendLine($"API.ReplyGump({gumpButton}, 0x{gumpId:X8})");
                            else
                                script.AppendLine($"API.ReplyGump({gumpButton})");
                        }
                        break;

                    case "vendorbuy":
                        if (action.Parameters.TryGetValue("vendor", out object buyVendor) &&
                            action.Parameters.TryGetValue("items", out object buyItems))
                            script.AppendLine($"# Vendor Buy: 0x{buyVendor:X8} items: {buyItems} (no direct API - use gump interactions)");
                        break;

                    case "vendorsell":
                        if (action.Parameters.TryGetValue("vendor", out object sellVendor) &&
                            action.Parameters.TryGetValue("items", out object sellItems))
                            script.AppendLine($"# Vendor Sell: 0x{sellVendor:X8} items: {sellItems} (no direct API - use gump interactions)");
                        break;

                    case "msg" or "say":
                        if (action.Parameters.TryGetValue("message", out object msgText))
                            script.AppendLine($"API.Msg(\"{msgText}\")");
                        break;

                    case "partymsg":
                        if (action.Parameters.TryGetValue("message", out object partyMessage))
                            script.AppendLine($"API.PartyMsg(\"{partyMessage}\")");
                        break;

                    case "guildmsg":
                        if (action.Parameters.TryGetValue("message", out object guildMessage))
                            script.AppendLine($"API.GuildMsg(\"{guildMessage}\")");
                        break;

                    case "allymsg":
                        if (action.Parameters.TryGetValue("message", out object allyMessage))
                            script.AppendLine($"API.AllyMsg(\"{allyMessage}\")");
                        break;

                    case "whispermsg":
                        if (action.Parameters.TryGetValue("message", out object whisperMessage))
                            script.AppendLine($"API.WhisperMsg(\"{whisperMessage}\")");
                        break;

                    case "yellmsg":
                        if (action.Parameters.TryGetValue("message", out object yellMessage))
                            script.AppendLine($"API.YellMsg(\"{yellMessage}\")");
                        break;

                    case "emotemsg":
                        if (action.Parameters.TryGetValue("message", out object emoteMessage))
                            script.AppendLine($"API.EmoteMsg(\"{emoteMessage}\")");
                        break;

                    case "usetype":
                        if (action.Parameters.TryGetValue("graphic", out object useGraphic) &&
                            action.Parameters.TryGetValue("hue", out object useHue) &&
                            action.Parameters.TryGetValue("container", out object useContainer))
                        {
                            if ((ushort)useHue == ushort.MaxValue && (uint)useContainer == uint.MaxValue)
                                script.AppendLine($"API.UseType(0x{useGraphic:X4})");
                            else if ((uint)useContainer == uint.MaxValue)
                                script.AppendLine($"API.UseType(0x{useGraphic:X4}, {useHue})");
                            else
                                script.AppendLine($"API.UseType(0x{useGraphic:X4}, {useHue}, 0x{useContainer:X8})");
                        }
                        break;

                    case "mount":
                        if (action.Parameters.TryGetValue("serial", out object mountSerial))
                            script.AppendLine($"API.Mount(0x{mountSerial:X8})");
                        break;

                    case "dismount":
                        script.AppendLine("API.Dismount()");
                        break;

                    case "turn":
                        if (action.Parameters.TryGetValue("direction", out object turnDirection))
                            script.AppendLine($"API.Turn(\"{turnDirection}\")");
                        break;

                    case "rename":
                        if (action.Parameters.TryGetValue("serial", out object renameSerial) &&
                            action.Parameters.TryGetValue("name", out object renameName))
                            script.AppendLine($"API.Rename(0x{renameSerial:X8}, \"{renameName}\")");
                        break;

                    case "toggleability":
                        if (action.Parameters.TryGetValue("ability", out object toggleAbility))
                            script.AppendLine($"API.ToggleAbility(\"{toggleAbility}\")");
                        break;

                    case "virtue":
                        if (action.Parameters.TryGetValue("virtue", out object virtueType))
                            script.AppendLine($"API.Virtue(\"{virtueType}\")");
                        break;

                    case "setskilllock":
                        if (action.Parameters.TryGetValue("skill", out object skillName) &&
                            action.Parameters.TryGetValue("state", out object skillLockState))
                            script.AppendLine($"API.SetSkillLock(\"{skillName}\", \"{skillLockState}\")");
                        break;

                    case "setstatlock":
                        if (action.Parameters.TryGetValue("stat", out object statName) &&
                            action.Parameters.TryGetValue("state", out object statLockState))
                            script.AppendLine($"API.SetStatLock(\"{statName}\", \"{statLockState}\")");
                        break;

                    default:
                        script.AppendLine($"# Unknown action: {action.ActionType}");
                        break;
                }
            }

            return script.ToString();
        }
    }

    /// <summary>
    /// Represents a single recorded action
    /// </summary>
    public class RecordedAction
    {
        public string ActionType { get; }
        public Dictionary<string, object> Parameters { get; }
        public uint Timestamp { get; }
        public uint DelayFromPrevious { get; }

        public RecordedAction(string actionType, Dictionary<string, object> parameters, uint timestamp, uint delayFromPrevious)
        {
            ActionType = actionType;
            Parameters = parameters ?? new Dictionary<string, object>();
            Timestamp = timestamp;
            DelayFromPrevious = delayFromPrevious;
        }

        public override string ToString()
        {
            var paramStr = string.Join(", ", Parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return $"{ActionType}({paramStr})";
        }
    }
}
