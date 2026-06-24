#nullable enable
using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using SDL3;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.HotKeys;

public static class HotKeysTabContent
{
    // Opacity applied to a keyless (draft) binding row's cell widgets so it reads as muted.
    private const float DimOpacity = 0.5f;

    // Static unsubscribe actions for the independent capture paths so that
    // Cleanup() can stop any in-progress capture when the window closes.
    private static Action? _editorUnsubscribe;
    private static Action? _toggleUnsubscribe;

    // The HotKeyManager.Changed handler installed by Build(). Stored so Cleanup() can
    // detach it; otherwise reopening the window stacks handlers that fire into disposed widgets.
    private static Action? _changedHandler;

    /// <summary>Call when the owning window closes to unsubscribe any active key-capture handler.</summary>
    public static void Cleanup()
    {
        _editorUnsubscribe?.Invoke();
        _editorUnsubscribe = null;
        _toggleUnsubscribe?.Invoke();
        _toggleUnsubscribe = null;

        if (_changedHandler != null)
        {
            HotKeyManager.Changed -= _changedHandler;
            _changedHandler = null;
        }
    }

    private static readonly (string Label, int Graphic)[] FixedPotions =
    {
        ("Heal Potion", 0x0F0C),
        ("Cure Potion", 0x0F07),
        ("Refresh Potion", 0x0F0B),
        ("Strength Potion", 0x0F09),
        ("Agility Potion", 0x0F08),
    };

    public static Widget Build(MyraControl owner)
    {
        Profile? profile = ProfileManager.CurrentProfile;
        if (profile == null)
            return new MyraLabel("Profile not loaded", MyraLabel.TextStyle.P);

        World? world = Client.Game?.UO?.World;

        // ── Selection state (mirrors MacrosTabContent's selectedMacro) ───────
        HotKeyEntry? selectedEntry = null;

        var root = new VerticalStackPanel { Spacing = 6 };

        // ── General section: master toggle + dedicated "toggle all hotkeys" key ──
        root.Widgets.Add(new MyraSpacer(15, 5));
        root.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_section_general"), MyraLabel.TextStyle.H2));
        Widget masterRow = BuildMasterRow(profile, out Action? refreshToggleLabel);
        var generalPanel = MyraStyle.ApplySectionPanelStyle(new Panel());
        generalPanel.Widgets.Add(masterRow);
        root.Widgets.Add(generalPanel);

        // ── Toolbar (Add) and master/detail panels ───────────────────────────
        var toolbar = new HorizontalStackPanel { Spacing = 2 };

        var listPanel   = new VerticalStackPanel { Spacing = 2 };
        var editorPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildBindingList()
        {
            listPanel.Widgets.Clear();

            if (HotKeyManager.Entries.Count == 0)
            {
                listPanel.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_none"), MyraLabel.TextStyle.P));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto(TazLang.Get("hotkeys_settrigger")),
                GridColumnInfo.Fill(TazLang.Get("hotkeys_action")),
                GridColumnInfo.Auto(TazLang.Get("hotkeys_enable"), alignRight: true),
                GridColumnInfo.Auto(TazLang.Get("hotkeys_edit"), alignRight: true),
                GridColumnInfo.Auto(TazLang.Get("hotkeys_remove"), alignRight: true)
            );

            int dataRow = 1;
            foreach (HotKeyEntry entry in HotKeyManager.Entries)
            {
                HotKeyEntry captured = entry;

                // A keyless draft (Kind == None) never fires in dispatch (matchers require a
                // specific Kind) so render it muted until it has a real trigger. Only the
                // Trigger/Action TEXT is dimmed; the Enable/Edit/Remove controls stay full
                // opacity so they remain obviously usable.
                bool keyless = captured.Trigger == null || captured.Trigger.Kind == HotKeyTriggerKind.None;
                float textOpacity = keyless ? DimOpacity : 1f;

                string triggerText = keyless
                    ? TazLang.Get("hotkeys_nokey")
                    : captured.Trigger?.Describe() ?? TazLang.Get("hotkeys_none");
                var triggerLbl = new MyraLabel(triggerText, MyraLabel.TextStyle.P) { Opacity = textOpacity };
                var actionLbl = new MyraLabel(captured.Action.DisplayName(world), MyraLabel.TextStyle.P) { Opacity = textOpacity };
                var enableChk = MyraCheckButton.CreateWithCallback(
                    captured.Enabled,
                    b =>
                    {
                        captured.Enabled = b;
                        if (captured.Action.Type == HotKeyActionType.SelfHeal)
                            profile.SelfHeal_Enabled = b;
                        HotKeyManager.Save();
                    });
                enableChk.HorizontalAlignment = HorizontalAlignment.Center;

                var editBtn = new MyraButton(TazLang.Get("hotkeys_edit"), () =>
                {
                    selectedEntry = captured;
                    BuildBindingList();
                    BuildEditor();
                }) { HorizontalAlignment = HorizontalAlignment.Right };

                var removeBtn = MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("hotkeys_remove"), () =>
                {
                    if (captured.Action.Type == HotKeyActionType.SelfHeal)
                        profile.SelfHeal_Enabled = false;
                    HotKeyManager.Entries.Remove(captured);
                    if (ReferenceEquals(selectedEntry, captured))
                        selectedEntry = null;
                    HotKeyManager.Save();
                    BuildBindingList();
                    BuildEditor();
                }));
                removeBtn.HorizontalAlignment = HorizontalAlignment.Right;

                grid.AddWidget(triggerLbl, dataRow, 0);
                grid.AddWidget(actionLbl, dataRow, 1);
                grid.AddWidget(enableChk, dataRow, 2);
                grid.AddWidget(editBtn, dataRow, 3);
                grid.AddWidget(removeBtn, dataRow, 4);
                dataRow++;
            }

            listPanel.Widgets.Add(grid);
        }

        // (Re)build the detail editor for the currently selected entry.
        void BuildEditor()
        {
            // Switching editors must unsubscribe any in-progress capture handler.
            _editorUnsubscribe?.Invoke();
            _editorUnsubscribe = null;

            editorPanel.Widgets.Clear();

            if (selectedEntry == null)
            {
                editorPanel.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_select_or_add"), MyraLabel.TextStyle.H3));
                return;
            }

            editorPanel.Widgets.Add(BuildEntryEditor(owner, world, selectedEntry, BuildBindingList));
        }

        // ── Add button: create a dimmed keyless draft and open its editor ────
        toolbar.Widgets.Add(new MyraButton(TazLang.Get("hotkeys_add"), () =>
        {
            SpellDefinition[] spells = SpellDefinition.GetAllSpells();
            var draft = new HotKeyEntry
            {
                Enabled = true,
                Trigger = new HotKeyTrigger { Kind = HotKeyTriggerKind.None },
                Action = new HotKeyAction
                {
                    Type = HotKeyActionType.Spell,
                    SpellId = spells.Length > 0 ? spells[0].ID : 0,
                },
            };
            HotKeyManager.Entries.Add(draft);
            HotKeyManager.Save();
            selectedEntry = draft;
            BuildBindingList();
            BuildEditor();
        }));

        // ── Vertical layout: editor (always present) → Add → binding list ────
        // The editor panel takes the vertical spot the removed SelfHeal row vacated.
        root.Widgets.Add(new MyraSpacer(15, 5));
        root.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_section_editor"), MyraLabel.TextStyle.H2));
        var editorFrame = MyraStyle.ApplySectionPanelStyle(new Panel());
        editorFrame.Widgets.Add(editorPanel);
        root.Widgets.Add(editorFrame);

        root.Widgets.Add(new MyraSpacer(15, 5));
        root.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_section_bindings"), MyraLabel.TextStyle.H2));
        root.Widgets.Add(toolbar);
        var listScroll = new ScrollViewer { MaxHeight = 450, Content = listPanel };
        root.Widgets.Add(listScroll);

        BuildBindingList();
        BuildEditor();

        // ── Live cross-context refresh ───────────────────────────────────────
        // When data changes elsewhere (e.g. the Macros tab updates a macro key and
        // OnMacroKeyChanged rewrites the matching HotKey entry's trigger), re-render
        // the already-built list + the toggle key label. BuildBindingList
        // only READS Entries and builds widgets — the enable/remove callbacks call
        // Save() on user action, never during a rebuild — so there is no re-entrant
        // Save→Changed loop.
        //
        // Any previously installed handler (from a prior window that wasn't cleaned up)
        // is detached first as a safety net; Cleanup() is the normal detach path.
        if (_changedHandler != null)
            HotKeyManager.Changed -= _changedHandler;

        _changedHandler = () =>
        {
            BuildBindingList();
            refreshToggleLabel?.Invoke();
        };
        HotKeyManager.Changed += _changedHandler;

        // ── Reconcile SelfHeal → profile ─────────────────────────────────────
        // SelfHealManager reads the profile (not HotKeyManager). After load, push the
        // FIRST SelfHeal entry that has a keyboard trigger into the profile so the
        // manager uses current values.
        ReconcileSelfHeal(profile);

        return root;
    }

    // Push the first keyboard-triggered SelfHeal entry's values into the profile.
    private static void ReconcileSelfHeal(Profile profile)
    {
        foreach (HotKeyEntry e in HotKeyManager.Entries)
        {
            if (e.Action?.Type != HotKeyActionType.SelfHeal)
                continue;
            if (e.Trigger == null || e.Trigger.Kind != HotKeyTriggerKind.Keyboard)
                continue;

            profile.SelfHeal_Key = e.Trigger.Key;
            profile.SelfHeal_Mod = (int)HotKeyTrigger.NormalizeMods((SDL.SDL_Keymod)e.Trigger.Mod);
            profile.SelfHeal_UseChivalry = e.Action.SelfHealChivalry;
            profile.SelfHeal_Enabled = e.Enabled;
            return;
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    // Master row: "Disable all hotkeys" checkbox + dedicated keyboard control to
    // bind the single ToggleHotkeys entry (keyboard-only capture, modeled on the
    // SelfHeal row). The toggle binding is stored as a HotKeyEntry in
    // HotKeyManager.Entries with Action.Type == ToggleHotkeys.
    // refreshLabel re-reads the current binding and updates the displayed key.
    // ───────────────────────────────────────────────────────────────────────
    private static Widget BuildMasterRow(Profile profile, out Action refreshLabel)
    {
        var panel = new VerticalStackPanel { Spacing = 4 };

        // "Disable all hotkeys" is the master kill-switch; give it its own line.
        var disableChk = MyraCheckButton.CreateWithCallback(
            profile.DisableHotkeys,
            b => profile.DisableHotkeys = b,
            TazLang.Get("hotkeys_disableall"),
            TazLang.Get("hotkeys_disableall_tooltip"));
        panel.Widgets.Add(disableChk);

        // Toggle-key control on its own sub-row beneath the checkbox.
        var row = new HorizontalStackPanel { Spacing = 4 };

        static HotKeyEntry? FindToggleEntry()
        {
            foreach (HotKeyEntry e in HotKeyManager.Entries)
                if (e.Action?.Type == HotKeyActionType.ToggleHotkeys)
                    return e;
            return null;
        }

        string DisplayKey()
        {
            HotKeyEntry? e = FindToggleEntry();
            return e?.Trigger != null ? e.Trigger.Describe() : TazLang.Get("hotkeys_none");
        }

        var keyLabel = new MyraLabel(DisplayKey(), MyraLabel.TextStyle.P);

        var normalPanel = new HorizontalStackPanel { Spacing = 4 };
        var editPanel = new HorizontalStackPanel { Spacing = 4, Visible = false };

        SDL.SDL_Keycode capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
        SDL.SDL_Keymod capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
        Action? unsubscribe = null;

        void Stop()
        {
            unsubscribe?.Invoke();
            unsubscribe = null;
            _toggleUnsubscribe = null;
            capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
            capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            keyLabel.Text = DisplayKey();
            normalPanel.Visible = true;
            editPanel.Visible = false;
        }

        normalPanel.Widgets.Add(new MyraButton(TazLang.Get("hotkeys_settrigger"), () =>
        {
            Stop();
            keyLabel.Text = TazLang.Get("hotkeys_pressakey");
            normalPanel.Visible = false;
            editPanel.Visible = true;

            void Handler(string hotkey)
            {
                (capturedKey, capturedMod) = ParseHotKeyString(hotkey);
                keyLabel.Text = KeysTranslator.TryGetKey(capturedKey, capturedMod);
            }

            Keyboard.KeyDownEvent += Handler;
            unsubscribe = () => Keyboard.KeyDownEvent -= Handler;
            _toggleUnsubscribe = unsubscribe;
        }));

        normalPanel.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("hotkeys_clear"), () =>
        {
            Stop();
            HotKeyManager.Entries.RemoveAll(e => e.Action?.Type == HotKeyActionType.ToggleHotkeys);
            HotKeyManager.Save();
            keyLabel.Text = DisplayKey();
        })));

        // Apply the captured key to the single ToggleHotkeys entry, running a
        // conflict check first (reusing the shared dialog).
        void Apply()
        {
            if (capturedKey == SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                Stop();
                return;
            }

            var trigger = new HotKeyTrigger
            {
                Kind = HotKeyTriggerKind.Keyboard,
                Key = (int)capturedKey,
                Mod = (int)HotKeyTrigger.NormalizeMods(capturedMod),
            };

            HotKeyEntry? existing = FindToggleEntry();

            void Commit()
            {
                HotKeyEntry? toggle = FindToggleEntry();
                if (toggle != null)
                {
                    toggle.Trigger = trigger;
                }
                else
                {
                    HotKeyManager.Entries.Add(new HotKeyEntry
                    {
                        Trigger = trigger,
                        Enabled = true,
                        Action = new HotKeyAction { Type = HotKeyActionType.ToggleHotkeys },
                    });
                }
                HotKeyManager.Save();
                Stop();
            }

            List<string> conflicts = HotKeyManager.FindConflicts(trigger, existing);
            if (conflicts.Count == 0)
            {
                Commit();
                return;
            }

            ShowConflictDialog(conflicts, ok =>
            {
                if (!ok)
                {
                    Stop();
                    return;
                }

                // Assign anyway: clear the old binding for this trigger everywhere
                // (HotKey entries, macro, SpellBar) except the toggle entry itself.
                HotKeyManager.ClearTriggerEverywhere(trigger, existing, null);

                Commit();
            });
        }

        editPanel.Widgets.Add(new MyraButton(TazLang.Get("hotkeys_apply"), Apply));
        editPanel.Widgets.Add(new MyraButton(TazLang.Get("hotkeys_cancel"), Stop));

        row.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_toggle_key"), MyraLabel.TextStyle.P));
        row.Widgets.Add(keyLabel);

        var actions = new VerticalStackPanel();
        actions.Widgets.Add(normalPanel);
        actions.Widgets.Add(editPanel);
        row.Widgets.Add(actions);

        panel.Widgets.Add(row);

        // Live-refresh: only touch the label when not mid-capture (don't clobber
        // the "Press a key..." prompt).
        refreshLabel = () =>
        {
            if (normalPanel.Visible)
                keyLabel.Text = DisplayKey();
        };

        return panel;
    }

    // ───────────────────────────────────────────────────────────────────────
    // Detail editor: edits the SELECTED entry live. category -> action picker ->
    // trigger capture -> Apply (commits the captured trigger to the entry). Action
    // edits are written through to selectedEntry.Action + Save() as the user picks.
    // ───────────────────────────────────────────────────────────────────────
    private static Widget BuildEntryEditor(MyraControl owner, World? world, HotKeyEntry entry, Action rebuildList)
    {
        var panel = new VerticalStackPanel { Spacing = 4 };

        // The action being edited IS the entry's action; edits write through + Save.
        HotKeyAction action = entry.Action;

        // SelfHeal write-through targets the profile (SelfHealManager reads it, not HotKeyManager).
        Profile? profile = ProfileManager.CurrentProfile;

        // Mouse/wheel trigger buttons (assigned below). Held here so BuildActionPicker's
        // UpdateTriggerButtons() can toggle their visibility (SelfHeal is keyboard-only).
        MyraButton[] mouseButtons = Array.Empty<MyraButton>();

        // Trigger capture rows. Declared up front so BuildActionPicker's UpdateTriggerButtons()
        // (which can run via owner.Defer before the capture section is built) sees them assigned.
        var triggerStack = new VerticalStackPanel { Spacing = 4 };
        var keyboardRow = new HorizontalStackPanel { Spacing = 4 };
        var mouseRow = new HorizontalStackPanel { Spacing = 4 };

        // Trigger capture state (for the Apply button).
        HotKeyTriggerKind triggerKind = HotKeyTriggerKind.None;
        SDL.SDL_Keycode capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
        SDL.SDL_Keymod capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
        int capturedButton = 0;
        bool capturedWheelUp = false;
        Action? unsubscribe = null;
        // Tracks whether the user has explicitly captured a trigger this editor session.
        // Macro adopt-on-pick only pre-fills the trigger when this is still false.
        bool userCapturedTrigger = false;

        // ── Category combo ───────────────────────────────────────────────────
        // ToggleHotkeys is intentionally NOT listed here: it is bound via the dedicated
        // key control on the master "Disable all hotkeys" row (single source of truth).
        var categoryNames = new[] { "Spell", "Macro", "Script", "Skill", "Consumable", "Ability", TazLang.Get("hotkeys_selfheal") };
        var categoryValues = new[]
        {
            HotKeyActionType.Spell, HotKeyActionType.Macro, HotKeyActionType.Script,
            HotKeyActionType.Skill, HotKeyActionType.Consumable, HotKeyActionType.Ability,
            HotKeyActionType.SelfHeal,
        };

        var categoryRow = new HorizontalStackPanel { Spacing = 4 };
        categoryRow.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_category"), MyraLabel.TextStyle.P));

#pragma warning disable CS0612, CS0618
        var categoryCombo = new ComboBox { Width = 200, VerticalAlignment = VerticalAlignment.Center };
        foreach (string n in categoryNames)
            categoryCombo.Items.Add(new ListItem(n));

        int initialCategoryIndex = 0;
        int foundCat = Array.IndexOf(categoryValues, action.Type);
        if (foundCat >= 0) initialCategoryIndex = foundCat;
        categoryCombo.SelectedIndex = initialCategoryIndex;
#pragma warning restore CS0612, CS0618
        categoryRow.Widgets.Add(categoryCombo);
        panel.Widgets.Add(categoryRow);

        // ── Action picker (rebuilt per category) ─────────────────────────────
        var actionPickerPanel = new VerticalStackPanel { Spacing = 4 };
        panel.Widgets.Add(actionPickerPanel);

        var triggerLabel = new MyraLabel(entry.Trigger?.Describe() ?? TazLang.Get("hotkeys_none"), MyraLabel.TextStyle.TableHeader);

        SDL.SDL_Keymod CurrentMods()
        {
            SDL.SDL_Keymod m = SDL.SDL_Keymod.SDL_KMOD_NONE;
            if (Keyboard.Ctrl) m |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
            if (Keyboard.Shift) m |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
            if (Keyboard.Alt) m |= SDL.SDL_Keymod.SDL_KMOD_ALT;
            return m;
        }

        // Pre-fill the captured trigger from an existing HotKeyTrigger (initial state or macro adopt).
        // userCaptured: true when the user explicitly captured this (locks out macro adopt).
        void PrefillTrigger(HotKeyTrigger? t, bool userCaptured)
        {
            if (t == null || t.Kind == HotKeyTriggerKind.None)
                return;

            triggerKind = t.Kind;
            capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
            capturedButton = 0;
            capturedWheelUp = false;
            capturedMod = (SDL.SDL_Keymod)t.Mod;

            switch (t.Kind)
            {
                case HotKeyTriggerKind.Keyboard:    capturedKey = (SDL.SDL_Keycode)t.Key; break;
                case HotKeyTriggerKind.MouseButton: capturedButton = t.Button; break;
                case HotKeyTriggerKind.MouseWheel:  capturedWheelUp = t.WheelUp; break;
            }

            if (userCaptured)
                userCapturedTrigger = true;

            triggerLabel.Text = t.Describe();
        }

        // True when a macro should adopt its current key into the (still-empty) trigger.
        bool ShouldAdoptMacroTrigger() => !userCapturedTrigger && triggerKind == HotKeyTriggerKind.None
                                          && (entry.Trigger == null || entry.Trigger.Kind == HotKeyTriggerKind.None);

        void BuildActionPicker()
        {
            actionPickerPanel.Widgets.Clear();

            HotKeyActionType type = categoryCombo.SelectedIndex is { } idx && idx >= 0 && idx < categoryValues.Length
                ? categoryValues[idx]
                : HotKeyActionType.Spell;

            action.Type = type;

            var row = new HorizontalStackPanel { Spacing = 4 };
            row.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_action"), MyraLabel.TextStyle.P));

            switch (type)
            {
                case HotKeyActionType.Spell:
                {
                    SpellDefinition[] spells = SpellDefinition.GetAllSpells();
#pragma warning disable CS0612, CS0618
                    var combo = new ComboBox { Width = 200, VerticalAlignment = VerticalAlignment.Center };
                    foreach (SpellDefinition s in spells)
                        combo.Items.Add(new ListItem(s.Name));

                    int sel = -1;
                    if (action.Type == HotKeyActionType.Spell)
                    {
                        for (int i = 0; i < spells.Length; i++)
                            if (spells[i].ID == action.SpellId) { sel = i; break; }
                    }
                    if (sel < 0) sel = spells.Length > 0 ? 0 : -1;
                    combo.SelectedIndex = sel >= 0 ? sel : null;
                    if (sel >= 0) action.SpellId = spells[sel].ID; // initial build: set without Save
                    combo.SelectedIndexChanged += (_, _) =>
                    {
                        if (combo.SelectedIndex is { } i && i >= 0 && i < spells.Length)
                        {
                            action.SpellId = spells[i].ID;
                            HotKeyManager.Save();
                        }
                    };
#pragma warning restore CS0612, CS0618
                    row.Widgets.Add(combo);
                    break;
                }

                case HotKeyActionType.Macro:
                {
                    List<Macro> macros = world?.Macros?.GetAllMacros() ?? new List<Macro>();
#pragma warning disable CS0612, CS0618
                    var combo = new ComboBox { Width = 200, VerticalAlignment = VerticalAlignment.Center };
                    foreach (Macro m in macros)
                        combo.Items.Add(new ListItem(m.Name));

                    int sel = -1;
                    if (action.Type == HotKeyActionType.Macro)
                    {
                        for (int i = 0; i < macros.Count; i++)
                            if (macros[i].Name == action.MacroName) { sel = i; break; }
                    }
                    if (sel < 0) sel = macros.Count > 0 ? 0 : -1;
                    combo.SelectedIndex = sel >= 0 ? sel : null;

                    void ApplyMacro(int i, bool save)
                    {
                        if (i < 0 || i >= macros.Count) return;
                        action.MacroName = macros[i].Name;
                        // Adopt-on-pick: when the entry has no trigger yet, pre-fill the
                        // trigger from the macro's current key and write it to the entry.
                        if (ShouldAdoptMacroTrigger())
                        {
                            HotKeyTrigger adopted = HotKeyManager.TriggerFromMacro(macros[i]);
                            PrefillTrigger(adopted, userCaptured: false);
                            if (adopted.Kind != HotKeyTriggerKind.None)
                                entry.Trigger = adopted;
                        }
                        if (save) HotKeyManager.Save();
                    }
                    ApplyMacro(sel, save: false); // initial build: set without Save
                    combo.SelectedIndexChanged += (_, _) =>
                    {
                        if (combo.SelectedIndex is { } i) ApplyMacro(i, save: true);
                    };
#pragma warning restore CS0612, CS0618
                    row.Widgets.Add(combo);
                    break;
                }

                case HotKeyActionType.Script:
                {
                    List<ClassicUO.LegionScripting.ScriptFile> scripts = ClassicUO.LegionScripting.LegionScripting.LoadedScripts;
#pragma warning disable CS0612, CS0618
                    var combo = new ComboBox { Width = 200, VerticalAlignment = VerticalAlignment.Center };
                    foreach (ClassicUO.LegionScripting.ScriptFile sf in scripts)
                        combo.Items.Add(new ListItem(sf.FileName));

                    int sel = -1;
                    if (action.Type == HotKeyActionType.Script)
                    {
                        for (int i = 0; i < scripts.Count; i++)
                            if (scripts[i].RelativePath == action.ScriptPath) { sel = i; break; }
                    }
                    if (sel < 0) sel = scripts.Count > 0 ? 0 : -1;
                    combo.SelectedIndex = sel >= 0 ? sel : null;
                    if (sel >= 0) action.ScriptPath = scripts[sel].RelativePath; // initial build: set without Save
                    combo.SelectedIndexChanged += (_, _) =>
                    {
                        if (combo.SelectedIndex is { } i && i >= 0 && i < scripts.Count)
                        {
                            action.ScriptPath = scripts[i].RelativePath;
                            HotKeyManager.Save();
                        }
                    };
#pragma warning restore CS0612, CS0618
                    row.Widgets.Add(combo);
                    break;
                }

                case HotKeyActionType.Skill:
                {
                    Skill[] skills = world?.Player?.Skills ?? Array.Empty<Skill>();
#pragma warning disable CS0612, CS0618
                    var combo = new ComboBox { Width = 200, VerticalAlignment = VerticalAlignment.Center };
                    foreach (Skill sk in skills)
                        combo.Items.Add(new ListItem(sk.Name));

                    int sel = -1;
                    if (action.Type == HotKeyActionType.Skill &&
                        action.SkillIndex >= 0 && action.SkillIndex < skills.Length)
                        sel = action.SkillIndex;
                    if (sel < 0) sel = skills.Length > 0 ? 0 : -1;
                    combo.SelectedIndex = sel >= 0 ? sel : null;
                    action.SkillIndex = sel >= 0 ? sel : -1; // initial build: set without Save
                    combo.SelectedIndexChanged += (_, _) =>
                    {
                        if (combo.SelectedIndex is { } i && i >= 0 && i < skills.Length)
                        {
                            action.SkillIndex = i;
                            HotKeyManager.Save();
                        }
                    };
#pragma warning restore CS0612, CS0618
                    row.Widgets.Add(combo);
                    break;
                }

                case HotKeyActionType.Consumable:
                {
                    var options = new List<(string Label, int Graphic, int Hue)>();
                    foreach (var p in FixedPotions)
                        options.Add((p.Label, p.Graphic, -1));
                    foreach (CustomConsumable c in HotKeyManager.CustomConsumables)
                        options.Add((c.Label, c.Graphic, c.Hue));

#pragma warning disable CS0612, CS0618
                    var combo = new ComboBox { Width = 200, VerticalAlignment = VerticalAlignment.Center };
                    foreach (var o in options)
                        combo.Items.Add(new ListItem(o.Label));

                    int sel = -1;
                    if (action.Type == HotKeyActionType.Consumable)
                    {
                        for (int i = 0; i < options.Count; i++)
                            if (options[i].Graphic == action.ConsumableGraphic &&
                                options[i].Hue == action.ConsumableHue) { sel = i; break; }
                    }
                    if (sel < 0) sel = options.Count > 0 ? 0 : -1;
                    combo.SelectedIndex = sel >= 0 ? sel : null;

                    void ApplyConsumable(int i, bool save)
                    {
                        if (i < 0 || i >= options.Count) return;
                        action.ConsumableLabel = options[i].Label;
                        action.ConsumableGraphic = options[i].Graphic;
                        action.ConsumableHue = options[i].Hue;
                        if (save) HotKeyManager.Save();
                    }
                    ApplyConsumable(sel, save: false); // initial build: set without Save
                    combo.SelectedIndexChanged += (_, _) =>
                    {
                        if (combo.SelectedIndex is { } i) ApplyConsumable(i, save: true);
                    };
#pragma warning restore CS0612, CS0618
                    row.Widgets.Add(combo);

                    row.Widgets.Add(new MyraButton(TazLang.Get("hotkeys_targetitem"), () =>
                        TargetCustomConsumable(BuildActionPicker)));
                    break;
                }

                case HotKeyActionType.Ability:
                {
                    var abilityNames = new[] { "Primary", "Secondary" };
#pragma warning disable CS0612, CS0618
                    var combo = new ComboBox { Width = 200, VerticalAlignment = VerticalAlignment.Center };
                    foreach (string n in abilityNames)
                        combo.Items.Add(new ListItem(n));

                    int sel = 0;
                    if (action.Type == HotKeyActionType.Ability)
                        sel = action.AbilityPrimary ? 0 : 1;
                    combo.SelectedIndex = sel;
                    action.AbilityPrimary = sel == 0; // initial build: set without Save
                    combo.SelectedIndexChanged += (_, _) =>
                    {
                        action.AbilityPrimary = combo.SelectedIndex == 0;
                        HotKeyManager.Save();
                    };
#pragma warning restore CS0612, CS0618
                    row.Widgets.Add(combo);
                    break;
                }

                case HotKeyActionType.SelfHeal:
                {
                    var schoolNames = new[] { TazLang.Get("hotkeys_magery"), TazLang.Get("hotkeys_chivalry") };
#pragma warning disable CS0612, CS0618
                    var combo = new ComboBox { Width = 200, VerticalAlignment = VerticalAlignment.Center };
                    foreach (string n in schoolNames)
                        combo.Items.Add(new ListItem(n));

                    int sel = action.SelfHealChivalry ? 1 : 0;
                    combo.SelectedIndex = sel;

                    void ApplySchool()
                    {
                        action.SelfHealChivalry = combo.SelectedIndex == 1;
                        if (profile != null)
                            profile.SelfHeal_UseChivalry = action.SelfHealChivalry;
                        HotKeyManager.Save();
                    }
                    ApplySchool();
                    combo.SelectedIndexChanged += (_, _) => ApplySchool();
#pragma warning restore CS0612, CS0618
                    row.Widgets.Add(combo);
                    break;
                }
            }

            actionPickerPanel.Widgets.Add(row);
            UpdateTriggerButtons();
        }

        categoryCombo.SelectedIndexChanged += (_, _) =>
        {
            categoryCombo.Desktop?.HideContextMenu();
            owner.Defer(BuildActionPicker);
        };

        // ── Trigger capture rows (Keyboard sub-row + Mouse sub-row) ───────────
        keyboardRow.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_keyboard"), MyraLabel.TextStyle.P));
        mouseRow.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_mouse"), MyraLabel.TextStyle.P));

        void StopCapture()
        {
            unsubscribe?.Invoke();
            unsubscribe = null;
            _editorUnsubscribe = null;
        }

        // Reset the entry's trigger to Kind=None (re-dims the row) and clears capture state.
        void ClearTrigger()
        {
            StopCapture();
            triggerKind = HotKeyTriggerKind.None;
            capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
            capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            capturedButton = 0;
            capturedWheelUp = false;
            userCapturedTrigger = false;
            triggerLabel.Text = TazLang.Get("hotkeys_none");
            entry.Trigger = new HotKeyTrigger { Kind = HotKeyTriggerKind.None };
            // SelfHeal entry: also disable the profile-driven hold-to-heal.
            if (action.Type == HotKeyActionType.SelfHeal && profile != null)
            {
                profile.SelfHeal_Key = 0;
                profile.SelfHeal_Enabled = false;
            }
            HotKeyManager.Save();
            rebuildList();
        }

        HotKeyTrigger? BuildTrigger()
        {
            switch (triggerKind)
            {
                case HotKeyTriggerKind.Keyboard:
                    if (capturedKey == SDL.SDL_Keycode.SDLK_UNKNOWN) return null;
                    return new HotKeyTrigger
                    {
                        Kind = HotKeyTriggerKind.Keyboard,
                        Key = (int)capturedKey,
                        Mod = (int)HotKeyTrigger.NormalizeMods(capturedMod),
                    };
                case HotKeyTriggerKind.MouseButton:
                    return new HotKeyTrigger
                    {
                        Kind = HotKeyTriggerKind.MouseButton,
                        Button = capturedButton,
                        Mod = (int)HotKeyTrigger.NormalizeMods(capturedMod),
                    };
                case HotKeyTriggerKind.MouseWheel:
                    return new HotKeyTrigger
                    {
                        Kind = HotKeyTriggerKind.MouseWheel,
                        WheelUp = capturedWheelUp,
                        Mod = (int)HotKeyTrigger.NormalizeMods(capturedMod),
                    };
                default:
                    return null;
            }
        }

        // Keyboard live-capture button.
        keyboardRow.Widgets.Add(new MyraButton(TazLang.Get("hotkeys_pressakey"), () =>
        {
            StopCapture();
            triggerLabel.Text = TazLang.Get("hotkeys_pressakey");

            void Handler(string hotkey)
            {
                (capturedKey, capturedMod) = ParseHotKeyString(hotkey);
                triggerKind = HotKeyTriggerKind.Keyboard;
                userCapturedTrigger = true;
                triggerLabel.Text = KeysTranslator.TryGetKey(capturedKey, capturedMod);
            }

            Keyboard.KeyDownEvent += Handler;
            unsubscribe = () => Keyboard.KeyDownEvent -= Handler;
            _editorUnsubscribe = unsubscribe;
        }));

        void SetMouseButton(int button, string display)
        {
            StopCapture();
            capturedMod = CurrentMods();
            capturedButton = button;
            triggerKind = HotKeyTriggerKind.MouseButton;
            userCapturedTrigger = true;
            triggerLabel.Text = display;
        }

        void SetWheel(bool up, string display)
        {
            StopCapture();
            capturedMod = CurrentMods();
            capturedWheelUp = up;
            triggerKind = HotKeyTriggerKind.MouseWheel;
            userCapturedTrigger = true;
            triggerLabel.Text = display;
        }

        // SelfHeal hold-to-heal needs key-up, so it is keyboard-only: hide the
        // mouse/wheel buttons for that category. UpdateTriggerButtons() is called from
        // BuildActionPicker so the visibility tracks category changes.
        mouseButtons = new[]
        {
            new MyraButton(TazLang.Get("hotkeys_mouse3"),
                () => SetMouseButton((int)MouseButtonType.Middle, TazLang.Get("hotkeys_mouse3"))) { Tooltip = "Middle mouse button" },
            new MyraButton(TazLang.Get("hotkeys_mouse4"),
                () => SetMouseButton((int)MouseButtonType.XButton1, TazLang.Get("hotkeys_mouse4"))) { Tooltip = "Side button (back)" },
            new MyraButton(TazLang.Get("hotkeys_mouse5"),
                () => SetMouseButton((int)MouseButtonType.XButton2, TazLang.Get("hotkeys_mouse5"))) { Tooltip = "Side button (forward)" },
            new MyraButton(TazLang.Get("hotkeys_wheelup"),
                () => SetWheel(true, TazLang.Get("hotkeys_wheelup"))) { Tooltip = TazLang.Get("hotkeys_wheelup") },
            new MyraButton(TazLang.Get("hotkeys_wheeldown"),
                () => SetWheel(false, TazLang.Get("hotkeys_wheeldown"))) { Tooltip = TazLang.Get("hotkeys_wheeldown") },
        };
        foreach (MyraButton b in mouseButtons)
            mouseRow.Widgets.Add(b);

        // SelfHeal is keyboard-only: hide the whole Mouse sub-row (and its buttons) for it.
        void UpdateTriggerButtons()
        {
            bool keyboardOnly = action.Type == HotKeyActionType.SelfHeal;
            mouseRow.Visible = !keyboardOnly;
            foreach (MyraButton b in mouseButtons)
                b.Visible = !keyboardOnly;
        }

        triggerStack.Widgets.Add(keyboardRow);
        triggerStack.Widgets.Add(mouseRow);
        panel.Widgets.Add(triggerStack);

        var captureLabelRow = new HorizontalStackPanel { Spacing = 4 };
        captureLabelRow.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_currenttrigger"), MyraLabel.TextStyle.P));
        captureLabelRow.Widgets.Add(triggerLabel);
        panel.Widgets.Add(captureLabelRow);

        // ── Apply / Clear ─────────────────────────────────────────────────────
        var buttonRow = new HorizontalStackPanel { Spacing = 4 };
        buttonRow.Widgets.Add(new MyraButton(TazLang.Get("hotkeys_apply"), () =>
        {
            HotKeyTrigger? trigger = BuildTrigger();
            if (trigger == null)
            {
                GameActions.Print(world, "HotKey: set a trigger first.", 32);
                return;
            }

            // Finalize: commit the trigger to the entry, persist, refresh.
            void Finalize(HotKeyTrigger finalTrigger)
            {
                entry.Trigger = finalTrigger;
                // SelfHeal write-through: SelfHealManager reads these profile fields.
                if (action.Type == HotKeyActionType.SelfHeal && profile != null &&
                    finalTrigger.Kind == HotKeyTriggerKind.Keyboard)
                {
                    profile.SelfHeal_Key = finalTrigger.Key;
                    profile.SelfHeal_Mod = (int)HotKeyTrigger.NormalizeMods((SDL.SDL_Keymod)finalTrigger.Mod);
                    profile.SelfHeal_Enabled = true;
                }
                HotKeyManager.Save();
                StopCapture();
                rebuildList();
            }

            // Runs the HotKey/Macro/SpellBar conflict check, then finalizes with the given trigger.
            void ConflictThenFinalize(HotKeyTrigger finalTrigger)
            {
                List<string> conflicts = HotKeyManager.FindConflicts(finalTrigger, entry);
                if (conflicts.Count == 0)
                {
                    Finalize(finalTrigger);
                    return;
                }

                ShowConflictDialog(conflicts, ok =>
                {
                    if (!ok) return;
                    // Don't wipe the entry's own macro when this is a Macro entry.
                    string? keepMacroName = action.Type == HotKeyActionType.Macro ? action.MacroName : null;
                    RemoveConflictingEntries(finalTrigger, entry, keepMacroName);
                    Finalize(finalTrigger);
                });
            }

            // Macro category: collapse macro-binding + conflict resolution into ONE dialog.
            if (action.Type == HotKeyActionType.Macro)
            {
                Macro? target = world?.Macros?.FindMacro(action.MacroName);
                HotKeyTrigger existing = HotKeyManager.TriggerFromMacro(target);

                bool sameAsExisting = existing.Kind == trigger.Kind &&
                                      existing.Key == trigger.Key &&
                                      existing.Button == trigger.Button &&
                                      existing.WheelUp == trigger.WheelUp &&
                                      existing.Mod == trigger.Mod;

                if (target == null || existing.Kind == HotKeyTriggerKind.None || sameAsExisting)
                {
                    // Unbound / already matches: write-through then finalize with the new trigger.
                    if (target != null)
                    {
                        HotKeyManager.ApplyTriggerToMacro(target, trigger);
                        world?.Macros?.Save();
                    }
                    ConflictThenFinalize(trigger);
                }
                else
                {
                    // The macro has a DIFFERENT binding: a single OK/Cancel dialog with a
                    // default-checked "update the macro's key" tick, plus any conflict warnings.
                    List<string> conflicts = HotKeyManager.FindConflicts(trigger, entry);

                    var content = new VerticalStackPanel { Spacing = 4 };
                    foreach (string c in conflicts)
                        content.Widgets.Add(new MyraLabel(c, MyraLabel.TextStyle.P));
                    content.Widgets.Add(new MyraLabel(
                        TazLang.Get("hotkeys_macro_conflict_body") + "\n" +
                        action.MacroName + ": " + existing.Describe(),
                        MyraLabel.TextStyle.P));
                    content.Widgets.Add(new MyraSpacer(10, 2));

                    var updateCheck = new MyraCheckButton(TazLang.Get("hotkeys_macro_update"), isChecked: true);
                    content.Widgets.Add(updateCheck);

                    _ = new MyraDialog(TazLang.Get("hotkeys_macro_conflict_title"), content, ok =>
                    {
                        if (!ok) return; // Cancel: leave the entry's trigger unchanged.

                        HotKeyTrigger finalTrigger;
                        if (updateCheck.IsChecked)
                        {
                            // Update the macro's key to the new trigger.
                            HotKeyManager.ApplyTriggerToMacro(target, trigger);
                            world?.Macros?.Save();
                            finalTrigger = trigger;
                        }
                        else
                        {
                            // Keep the macro's key: finalize the entry with the macro's EXISTING trigger.
                            finalTrigger = existing;
                        }

                        // Remove any conflicting bindings sharing the FINAL trigger (except this
                        // entry and its own macro — Update just set it / Keep keeps its key).
                        RemoveConflictingEntries(finalTrigger, entry, action.MacroName);
                        Finalize(finalTrigger);
                    });
                }

                return;
            }

            // Non-macro categories: standard conflict check + finalize.
            ConflictThenFinalize(trigger);
        }));

        buttonRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("hotkeys_clear"), ClearTrigger)));

        panel.Widgets.Add(buttonRow);

        // Build the initial action picker for the selected category.
        BuildActionPicker();

        // Pre-fill the captured trigger from the entry's current trigger (if any).
        PrefillTrigger(entry.Trigger, userCaptured: true);

        return panel;
    }

    // "Assign anyway": clear the old binding for finalTrigger everywhere it conflicts —
    // HotKey entries (except the one being edited), the bound macro (except the entry's own
    // macro, identified by keepMacroName), the SpellBar slot, and Profile.SelfHeal.
    private static void RemoveConflictingEntries(HotKeyTrigger finalTrigger, HotKeyEntry? keep, string? keepMacroName)
    {
        HotKeyManager.ClearTriggerEverywhere(finalTrigger, keep, keepMacroName);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Conflict dialog
    // ───────────────────────────────────────────────────────────────────────
    private static void ShowConflictDialog(List<string> conflicts, Action<bool> onResult)
    {
        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel(TazLang.Get("hotkeys_conflict_body"), MyraLabel.TextStyle.P));
        foreach (string c in conflicts)
            content.Widgets.Add(new MyraLabel(c, MyraLabel.TextStyle.P));
        content.Widgets.Add(new MyraSpacer(10, 2));
        content.Widgets.Add(new MyraLabel(
            "OK = " + TazLang.Get("hotkeys_assignanyway") + "  /  Cancel = " + TazLang.Get("hotkeys_cancel"),
            MyraLabel.TextStyle.P));

        // OK => assign anyway (removes old); Cancel => discard.
        _ = new MyraDialog(TazLang.Get("hotkeys_conflict_title"), content, onResult);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Target an item to set (custom consumable)
    // ───────────────────────────────────────────────────────────────────────
    private static void TargetCustomConsumable(Action onAdded)
    {
        World? world = Client.Game?.UO?.World;
        if (world?.TargetManager == null)
            return;

        if (HotKeyManager.CustomConsumables.Count >= HotKeyManager.MaxCustomConsumables)
        {
            GameActions.Print(world, $"HotKey: custom consumable limit ({HotKeyManager.MaxCustomConsumables}) reached.", 32);
            return;
        }

        GameActions.Print(world, TazLang.Get("hotkeys_targetitem"));
        world.TargetManager.SetTargeting(targeted =>
        {
            if (targeted is not Entity entity || !SerialHelper.IsItem(entity))
                return;

            int graphic = entity.Graphic;
            int hue = entity.Hue;

            var labelBox = new MyraInputBox { MinWidth = 150, Text = entity.Name ?? "" };
            _ = new MyraDialog(TazLang.Get("hotkeys_targetitem"), labelBox, ok =>
            {
                if (!ok) return;
                string label = string.IsNullOrWhiteSpace(labelBox.Text) ? $"Item 0x{graphic:X4}" : labelBox.Text;
                bool added = HotKeyManager.AddCustomConsumable(new CustomConsumable
                {
                    Label = label,
                    Graphic = graphic,
                    Hue = hue,
                });
                if (!added)
                    GameActions.Print(world, $"HotKey: custom consumable limit ({HotKeyManager.MaxCustomConsumables}) reached.", 32);
                else
                    HotKeyManager.Save();
                onAdded();
            });
        });
    }

    // ───────────────────────────────────────────────────────────────────────
    // Helpers
    // ───────────────────────────────────────────────────────────────────────
    private static (SDL.SDL_Keycode key, SDL.SDL_Keymod mod) ParseHotKeyString(string hotkey)
    {
        SDL.SDL_Keycode key = SDL.SDL_Keycode.SDLK_UNKNOWN;
        SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;

        if (string.IsNullOrEmpty(hotkey))
            return (key, mod);

        foreach (string part in hotkey.Split('+'))
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL": mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL; break;
                case "SHIFT": mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT; break;
                case "ALT": mod |= SDL.SDL_Keymod.SDL_KMOD_ALT; break;
                default:
                    if (Enum.TryParse<SDL.SDL_Keycode>(part, true, out SDL.SDL_Keycode parsed))
                        key = parsed;
                    break;
            }
        }

        return (key, mod);
    }
}
