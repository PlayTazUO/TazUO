using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    internal class ScriptRecordingGump : NineSliceGump
    {
        private ModernScrollArea _scrollArea;
        private NiceButton _recordButton;
        private NiceButton _pauseButton;
        private NiceButton _clearButton;
        private NiceButton _copyButton;
        private NiceButton _saveButton;
        private Label _titleBar;
        private Label _statusText;
        private Label _durationText;
        private Label _actionCountText;
        private VBoxContainer _actionList;
        private Checkbox _recordPausesCheckbox;

        private static int _lastX = 100, _lastY = 100;
        private static int _lastWidth = 400, _lastHeight = 500;
        private const int MIN_WIDTH = 350;
        private const int MIN_HEIGHT = 400;

        private List<RecordedAction> _displayedActions = new List<RecordedAction>();

        public ScriptRecordingGump() : base(World.Instance, _lastX, _lastY, _lastWidth, _lastHeight, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, false)
        {
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            CanMove = true;

            BuildGump();
            SubscribeToRecorderEvents();
            UpdateUI();
        }

        private void BuildGump()
        {
            // Title bar
            _titleBar = new Label("Script Recording - Stopped", true, 52, font: 1)
            {
                X = BorderSize + 10,
                Y = BorderSize + 10
            };
            Add(_titleBar);

            int currentY = _titleBar.Y + _titleBar.Height + 15;

            // Control buttons
            _recordButton = new NiceButton(BorderSize + 10, currentY, 80, 25, ButtonAction.Activate, "Record", 0, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                ButtonParameter = (int)RecordingAction.ToggleRecord,
                DisplayBorder = true
            };
            _recordButton.MouseUp += OnButtonClick;
            Add(_recordButton);

            _pauseButton = new NiceButton(BorderSize + 100, currentY, 60, 25, ButtonAction.Activate, "Pause", 0, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                ButtonParameter = (int)RecordingAction.Pause,
                IsEnabled = false,
                DisplayBorder = true
            };
            _pauseButton.MouseUp += OnButtonClick;
            Add(_pauseButton);

            _clearButton = new NiceButton(BorderSize + 170, currentY, 60, 25, ButtonAction.Activate, "Clear", 0, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                ButtonParameter = (int)RecordingAction.Clear,
                DisplayBorder = true
            };
            _clearButton.MouseUp += OnButtonClick;
            Add(_clearButton);

            currentY += 35;

            // Status information
            _statusText = new Label("Status: Ready", true, 0xFFFF, font: 1)
            {
                X = BorderSize + 10,
                Y = currentY
            };
            Add(_statusText);

            currentY += _statusText.Height + 5;

            _durationText = new Label("Duration: 0:00", true, 999, font: 1)
            {
                X = BorderSize + 10,
                Y = currentY
            };
            Add(_durationText);

            currentY += _durationText.Height + 5;

            _actionCountText = new Label("Actions: 0", true, 999, font: 1)
            {
                X = BorderSize + 10,
                Y = currentY
            };
            Add(_actionCountText);

            currentY += _actionCountText.Height + 10;

            // Record pauses option
            _recordPausesCheckbox = new Checkbox(0x00D2, 0x00D3, "Included pauses (timing delays)", 1, 0xFFFF)
            {
                X = BorderSize + 10,
                Y = currentY,
                IsChecked = true
            };
            Add(_recordPausesCheckbox);

            currentY += _recordPausesCheckbox.Height + 15;

            // Action list
            var actionListLabel = new Label("Recorded Actions:", true, 0x35, font: 1)
            {
                X = BorderSize + 10,
                Y = currentY
            };
            Add(actionListLabel);

            currentY += actionListLabel.Height + 5;

            // Scrollable action list
            int listHeight = Height - currentY - 80; // Leave space for bottom buttons
            _scrollArea = new ModernScrollArea(BorderSize + 10, currentY, Width - 2 * BorderSize - 20, listHeight)
            {
                AcceptMouseInput = true,
                ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView
            };
            Add(_scrollArea);

            _actionList = new VBoxContainer(Width - 2 * BorderSize - 35)
            {
                X = 0,
                Y = 0
            };
            _scrollArea.Add(_actionList);

            // Bottom buttons
            int bottomY = Height - BorderSize - 35;
            _copyButton = new NiceButton(BorderSize + 10, bottomY, 100, 25, ButtonAction.Activate, "Copy Script", 0, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                ButtonParameter = (int)RecordingAction.Copy,
                DisplayBorder = true
            };
            _copyButton.MouseUp += OnButtonClick;
            Add(_copyButton);

            _saveButton = new NiceButton(BorderSize + 120, bottomY, 100, 25, ButtonAction.Activate, "Save Script", 0, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                ButtonParameter = (int)RecordingAction.Save,
                DisplayBorder = true
            };
            _saveButton.MouseUp += OnButtonClick;
            Add(_saveButton);
        }

        private void SubscribeToRecorderEvents()
        {
            ScriptRecorder.Instance.RecordingStateChanged += OnRecordingStateChanged;
            ScriptRecorder.Instance.ActionRecorded += OnActionRecorded;
        }

        private void UnsubscribeFromRecorderEvents()
        {
            ScriptRecorder.Instance.RecordingStateChanged -= OnRecordingStateChanged;
            ScriptRecorder.Instance.ActionRecorded -= OnActionRecorded;
        }

        private void OnRecordingStateChanged(object sender, EventArgs e) => UpdateUI();

        private void OnActionRecorded(object sender, RecordedAction action)
        {
            _displayedActions.Add(action);
            UpdateActionList();
            UpdateActionCount();
        }

        private void OnButtonClick(object sender, MouseEventArgs e)
        {
            if (sender is NiceButton button)
            {
                var action = (RecordingAction)button.ButtonParameter;

                switch (action)
                {
                    case RecordingAction.ToggleRecord:
                        if (ScriptRecorder.Instance.IsRecording)
                            ScriptRecorder.Instance.StopRecording();
                        else
                            ScriptRecorder.Instance.StartRecording();
                        break;

                    case RecordingAction.Pause:
                        if (ScriptRecorder.Instance.IsPaused)
                            ScriptRecorder.Instance.ResumeRecording();
                        else
                            ScriptRecorder.Instance.PauseRecording();
                        break;

                    case RecordingAction.Clear:
                        ScriptRecorder.Instance.ClearRecording();
                        _displayedActions.Clear();
                        UpdateActionList();
                        break;

                    case RecordingAction.Copy:
                        CopyScriptToClipboard();
                        break;

                    case RecordingAction.Save:
                        SaveScriptToFile();
                        break;
                }
            }
        }

        private void OnActionButtonClick(object sender, MouseEventArgs e)
        {
            if (sender is NiceButton button)
            {
                string actionType = button.Tag as string;
                int index = button.ButtonParameter;

                switch (actionType)
                {
                    case "delete":
                        DeleteAction(index);
                        break;

                    case "moveup":
                        MoveActionUp(index);
                        break;

                    case "movedown":
                        MoveActionDown(index);
                        break;
                }
            }
        }

        private void DeleteAction(int index)
        {
            if (index >= 0 && index < _displayedActions.Count)
            {
                _displayedActions.RemoveAt(index);
                ScriptRecorder.Instance.RemoveActionAt(index);
                UpdateActionList();
                UpdateActionCount();
            }
        }

        private void MoveActionUp(int index)
        {
            if (index > 0 && index < _displayedActions.Count)
            {
                // Swap with previous action
                RecordedAction temp = _displayedActions[index];
                _displayedActions[index] = _displayedActions[index - 1];
                _displayedActions[index - 1] = temp;

                ScriptRecorder.Instance.SwapActions(index, index - 1);
                UpdateActionList();
            }
        }

        private void MoveActionDown(int index)
        {
            if (index >= 0 && index < _displayedActions.Count - 1)
            {
                // Swap with next action
                RecordedAction temp = _displayedActions[index];
                _displayedActions[index] = _displayedActions[index + 1];
                _displayedActions[index + 1] = temp;

                ScriptRecorder.Instance.SwapActions(index, index + 1);
                UpdateActionList();
            }
        }

        private void UpdateUI()
        {
            ScriptRecorder recorder = ScriptRecorder.Instance;

            // Update title
            string status = recorder.IsRecording
                ? (recorder.IsPaused ? "Paused" : "Recording")
                : "Stopped";

            _titleBar.Text = $"Script Recording - {status}";

            // Update buttons
            _recordButton.SetText(recorder.IsRecording ? "Stop" : "Record");
            _pauseButton.IsEnabled = recorder.IsRecording;
            _pauseButton.SetText(recorder.IsPaused ? "Resume" : "Pause");

            // Update status
            _statusText.Text = $"Status: {status}";

            // Update duration and action count
            UpdateDuration();
            UpdateActionCount();
        }

        private void UpdateDuration()
        {
            uint duration = ScriptRecorder.Instance.RecordingDuration;
            uint seconds = duration / 1000;
            uint minutes = seconds / 60;
            seconds %= 60;

            _durationText.Text = $"Duration: {minutes}:{seconds:D2}";
        }

        private void UpdateActionCount() => _actionCountText.Text = $"Actions: {ScriptRecorder.Instance.ActionCount}";

        private void UpdateActionList()
        {
            _actionList.Clear();

            // Show all actions, not just recent ones, to allow proper manipulation
            for (int i = 0; i < _displayedActions.Count; i++)
            {
                Control actionContainer = CreateActionRowContainer(_displayedActions[i], i);
                _actionList.Add(actionContainer);
            }
        }

        private Control CreateActionRowContainer(RecordedAction action, int index)
        {
            var container = new HitBox(0, 0, _actionList.Width - 10, 25, alpha: 0.0f);

            // Action text label
            string actionText = FormatActionForDisplay(action);
            var actionLabel = new Label(actionText, true, 0xFFFF, font: 1, maxwidth: container.Width - 90)
            {
                X = 0,
                Y = 2
            };
            container.Add(actionLabel);

            // Delete button
            var deleteButton = new NiceButton(container.Width - 85, 2, 25, 20, ButtonAction.Activate, "×", 0, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                ButtonParameter = index,
                DisplayBorder = true,
                Tag = "delete"
            };
            deleteButton.MouseUp += OnActionButtonClick;
            container.Add(deleteButton);

            // Move up button
            var moveUpButton = new NiceButton(container.Width - 57, 2, 25, 20, ButtonAction.Activate, "↑", 0, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                ButtonParameter = index,
                DisplayBorder = true,
                IsEnabled = index > 0,
                Tag = "moveup"
            };
            moveUpButton.MouseUp += OnActionButtonClick;
            container.Add(moveUpButton);

            // Move down button
            var moveDownButton = new NiceButton(container.Width - 29, 2, 25, 20, ButtonAction.Activate, "↓", 0, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                ButtonParameter = index,
                DisplayBorder = true,
                IsEnabled = index < _displayedActions.Count - 1,
                Tag = "movedown"
            };
            moveDownButton.MouseUp += OnActionButtonClick;
            container.Add(moveDownButton);

            return container;
        }

        private string FormatActionForDisplay(RecordedAction action)
        {
            switch (action.ActionType.ToLower())
            {
                case "walk":
                    string walkDir = action.Parameters.ContainsKey("direction") ? Utility.GetDirectionString(Utility.GetDirection(action.Parameters["direction"].ToString())) : "?";
                    return $"Walk {walkDir}";
                case "run":
                    string runDir = action.Parameters.ContainsKey("direction") ? Utility.GetDirectionString(Utility.GetDirection(action.Parameters["direction"].ToString())) : "?";
                    return $"Run {runDir}";
                case "cast":
                    object spell = action.Parameters.ContainsKey("spell") ? action.Parameters["spell"] : "?";
                    return $"Cast \"{spell}\"";
                case "say":
                    string message = action.Parameters.ContainsKey("message") ? action.Parameters["message"].ToString() : "?";
                    if (message.Length > 30)
                        message = message.Substring(0, 27) + "...";
                    return $"Say \"{message}\"";
                case "useitem":
                    object serial = action.Parameters.ContainsKey("serial") ? action.Parameters["serial"] : "?";
                    return $"Use Item 0x{serial:X8}";
                case "dragdrop":
                    object from = action.Parameters.ContainsKey("from") ? action.Parameters["from"] : "?";
                    object to = action.Parameters.ContainsKey("to") ? action.Parameters["to"] : "?";
                    return $"DragDrop 0x{from:X8} → 0x{to:X8}";
                case "target":
                    object targetSerial = action.Parameters.ContainsKey("serial") ? action.Parameters["serial"] : "?";
                    return $"Target 0x{targetSerial:X8}";
                case "targetlocation":
                    object targX = action.Parameters.ContainsKey("x") ? action.Parameters["x"] : "?";
                    object targY = action.Parameters.ContainsKey("y") ? action.Parameters["y"] : "?";
                    object targZ = action.Parameters.ContainsKey("z") ? action.Parameters["z"] : "?";
                    return $"Target Loc ({targX}, {targY}, {targZ})";
                case "opencontainer":
                    object openSerial = action.Parameters.ContainsKey("serial") ? action.Parameters["serial"] : "?";
                    object openType = action.Parameters.ContainsKey("type") ? action.Parameters["type"] : "container";
                    return $"Open {openType} 0x{openSerial:X8}";
                case "closecontainer":
                    object closeSerial = action.Parameters.ContainsKey("serial") ? action.Parameters["serial"] : "?";
                    object closeType = action.Parameters.ContainsKey("type") ? action.Parameters["type"] : "container";
                    return $"Close {closeType} 0x{closeSerial:X8}";
                case "attack":
                    object attackSerial = action.Parameters.ContainsKey("serial") ? action.Parameters["serial"] : "?";
                    return $"Attack 0x{attackSerial:X8}";
                case "bandageself":
                    return "Bandage Self";
                case "contextmenu":
                    object contextSerial = action.Parameters.ContainsKey("serial") ? action.Parameters["serial"] : "?";
                    object contextIndex = action.Parameters.ContainsKey("index") ? action.Parameters["index"] : "?";
                    return $"Context Menu 0x{contextSerial:X8} [{contextIndex}]";
                case "useskill":
                    object skillName = action.Parameters.ContainsKey("skill") ? action.Parameters["skill"] : "?";
                    return $"Use Skill \"{skillName}\"";
                case "equipitem":
                    object equipSerial = action.Parameters.ContainsKey("serial") ? action.Parameters["serial"] : "?";
                    object layer = action.Parameters.ContainsKey("layer") ? action.Parameters["layer"] : "?";
                    return $"Equip 0x{equipSerial:X8} ({layer})";
                case "replygump":
                    object gumpButton = action.Parameters.ContainsKey("button") ? action.Parameters["button"] : "?";
                    object gumpId = action.Parameters.ContainsKey("gumpid") ? action.Parameters["gumpid"] : "?";
                    return $"Gump Button {gumpButton} (0x{gumpId:X8})";
                case "headmsg":
                    string headMsgText = action.Parameters.ContainsKey("message") ? action.Parameters["message"].ToString() : "?";
                    object headSerial = action.Parameters.ContainsKey("serial") ? action.Parameters["serial"] : "?";
                    if (headMsgText.Length > 20) headMsgText = headMsgText.Substring(0, 17) + "...";
                    return $"Head Msg \"{headMsgText}\" (0x{headSerial:X8})";
                case "partymsg":
                    string partyMsgText = action.Parameters.ContainsKey("message") ? action.Parameters["message"].ToString() : "?";
                    if (partyMsgText.Length > 25) partyMsgText = partyMsgText.Substring(0, 22) + "...";
                    return $"Party: \"{partyMsgText}\"";
                case "guildmsg":
                    string guildMsgText = action.Parameters.ContainsKey("message") ? action.Parameters["message"].ToString() : "?";
                    if (guildMsgText.Length > 25) guildMsgText = guildMsgText.Substring(0, 22) + "...";
                    return $"Guild: \"{guildMsgText}\"";
                case "allymsg":
                    string allyMsgText = action.Parameters.ContainsKey("message") ? action.Parameters["message"].ToString() : "?";
                    if (allyMsgText.Length > 25) allyMsgText = allyMsgText.Substring(0, 22) + "...";
                    return $"Ally: \"{allyMsgText}\"";
                case "whispermsg":
                    string whisperMsgText = action.Parameters.ContainsKey("message") ? action.Parameters["message"].ToString() : "?";
                    if (whisperMsgText.Length > 25) whisperMsgText = whisperMsgText.Substring(0, 22) + "...";
                    return $"Whisper: \"{whisperMsgText}\"";
                case "yellmsg":
                    string yellMsgText = action.Parameters.ContainsKey("message") ? action.Parameters["message"].ToString() : "?";
                    if (yellMsgText.Length > 25) yellMsgText = yellMsgText.Substring(0, 22) + "...";
                    return $"Yell: \"{yellMsgText}\"";
                case "emotemsg":
                    string emoteMsgText = action.Parameters.ContainsKey("message") ? action.Parameters["message"].ToString() : "?";
                    if (emoteMsgText.Length > 25) emoteMsgText = emoteMsgText.Substring(0, 22) + "...";
                    return $"Emote: \"{emoteMsgText}\"";
                case "mount":
                    object mountSerial = action.Parameters.ContainsKey("serial") ? action.Parameters["serial"] : "?";
                    return $"Mount 0x{mountSerial:X8}";
                case "dismount":
                    return "Dismount";
                case "toggleability":
                    object ability = action.Parameters.ContainsKey("ability") ? action.Parameters["ability"] : "?";
                    return $"Toggle Ability \"{ability}\"";
                case "virtue":
                    object virtue = action.Parameters.ContainsKey("virtue") ? action.Parameters["virtue"] : "?";
                    return $"Invoke Virtue \"{virtue}\"";
                case "waitforgump":
                    return "Wait for gump";
                default:
                    return $"{action.ActionType}(...)";
            }
        }

        private void CopyScriptToClipboard()
        {
            try
            {
                string script = ScriptRecorder.Instance.GenerateScript(_recordPausesCheckbox.IsChecked);
                SDL3.SDL.SDL_SetClipboardText(script);
                GameActions.Print("Script copied to clipboard!");
            }
            catch (Exception ex)
            {
                GameActions.Print($"Failed to copy script: {ex.Message}");
            }
        }

        private void SaveScriptToFile()
        {
            try
            {
                string script = ScriptRecorder.Instance.GenerateScript(_recordPausesCheckbox.IsChecked);
                string fileName = $"recorded_script_{DateTime.Now:yyyyMMdd_HHmmss}.py";
                string filePath = System.IO.Path.Combine(LegionScripting.ScriptPath, fileName);

                System.IO.File.WriteAllText(filePath, script);
                GameActions.Print($"Script saved as {fileName}");
            }
            catch (Exception ex)
            {
                GameActions.Print($"Failed to save script: {ex.Message}");
            }
        }

        public override void Update()
        {
            base.Update();

            // Update duration display if recording
            if (ScriptRecorder.Instance.IsRecording && !ScriptRecorder.Instance.IsPaused)
            {
                UpdateDuration();
            }
        }

        protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            base.OnResize(oldWidth, oldHeight, newWidth, newHeight);

            // Save position and size
            _lastX = X;
            _lastY = Y;
            _lastWidth = Width;
            _lastHeight = Height;

            // Rebuild gump with new dimensions
            BuildGump();
            UpdateUI();
        }

        public override void Dispose()
        {
            UnsubscribeFromRecorderEvents();
            base.Dispose();
        }

        private enum RecordingAction
        {
            ToggleRecord,
            Pause,
            Clear,
            Copy,
            Save
        }
    }
}
