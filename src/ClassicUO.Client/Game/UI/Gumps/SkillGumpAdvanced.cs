// SPDX-License-Identifier: BSD-2-Clause


using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    public class SkillGumpAdvanced : ScalableGump
    {
        private const int WIDTH = 400;

        // Design-space (unscaled) height of the gump. The visible Height field is this value scaled by GumpScale.
        private int _designHeight = 310;

        private static readonly Dictionary<Buttons, string> _buttonsToSkillsValues = new()
        {
            { Buttons.SortName, "Name" },
            { Buttons.SortReal, "Base" },
            { Buttons.SortBase, "Value" },
            { Buttons.SortCap, "Cap" },
            { Buttons.SortLock, "Lock" }
        };

        private DataBox _databox;
        private List<SkillListEntry> _skillListEntries = new();

        public static bool Dragging;

        private static bool _sortAsc;
        private static string _sortField = "Name";
        private GumpPic _sortOrderIndicator;
        private double _totalReal, _totalValue;
        private bool _updateSkillsNeeded;
        private Button resizeDrag;
        private Area BottomArea;
        private int dragStartH;
        private Label real, value;
        private AlphaBlendControl background;

        private ScrollArea area;
        private static int last_x = 100, last_y = 100, last_button = (int)Buttons.SortName;

        public SkillGumpAdvanced(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = false;

            GumpScale = ProfileManager.CurrentProfile?.SkillsGumpScale ?? 1.0;
            // The gump is built in design space and scaled explicitly (statics at the end of Build,
            // dynamic content in BuildGump), so keep the base Add() from also scaling children.
            AutoScaleChildren = false;

            _designHeight = 310;
            if (ProfileManager.CurrentProfile != null)
                _designHeight = ProfileManager.CurrentProfile.AdvancedSkillsGumpHeight;

            Width = ScaleHelper.Scaled(WIDTH, GumpScale);
            Height = ScaleHelper.Scaled(_designHeight, GumpScale);

            Build();
        }

        private void Build()
        {

            Add
            (background =
                new AlphaBlendControl(0.65f)
                {
                    X = 1,
                    Y = 1,
                    Width = WIDTH - 1,
                    Height = _designHeight - 1
                }
            );

            area = new ScrollArea
            (
                5,
                40,
                WIDTH - 10,
                _designHeight - 60,
                true
            )
            {
                AcceptMouseInput = true
            };

            Add(area);

            _databox = new DataBox(0, 0, 1, 1);
            _databox.WantUpdateSize = true;

            area.Add(_databox);

            NiceButton _;
            Add
            (_ =
                new NiceButton
                (
                    5,
                    5,
                    180,
                    25,
                    ButtonAction.Activate,
                    ResGumps.Name
                )
                {
                    ButtonParameter = (int)Buttons.SortName,
                    IsSelected = last_button == (int)Buttons.SortName
                }
            );

            Add
            (_ =
                new NiceButton
                (
                    _.X + _.Width + 10,
                    _.Y,
                    50,
                    25,
                    ButtonAction.Activate,
                    ResGumps.Real
                )
                {
                    ButtonParameter = (int)Buttons.SortReal,
                    IsSelected = last_button == (int)Buttons.SortReal
                }
            );

            Add
            (_ =
                new NiceButton
                (
                    _.X + _.Width,
                    _.Y,
                    50,
                    25,
                    ButtonAction.Activate,
                    ResGumps.Base
                )
                {
                    ButtonParameter = (int)Buttons.SortBase,
                    IsSelected = last_button == (int)Buttons.SortBase
                }
            );

            Add
            (_ =
                new NiceButton
                (
                    _.X + _.Width,
                    _.Y,
                    50,
                    25,
                    ButtonAction.Activate,
                    ResGumps.Cap
                )
                {
                    ButtonParameter = (int)Buttons.SortCap,
                    IsSelected = last_button == (int)Buttons.SortCap
                }
            );

            Add
            (_ =
                new NiceButton
                (
                    _.X + _.Width,
                    _.Y,
                    50,
                    25,
                    ButtonAction.Activate,
                    "Lock"
                )
                {
                    ButtonParameter = (int)Buttons.SortLock,
                    IsSelected = last_button == (int)Buttons.SortLock
                }
            );

            Add
            (
                new Line
                (
                    area.X,
                    area.Y - 1,
                    area.Width,
                    1,
                    0xFFFFFFFF
                )
            );

            BottomArea = new Area()
            {
                X = 1,
                Y = area.Height + area.Y - 1,
                AcceptMouseInput = true,
                WantUpdateSize = false,
                CanMove = true,
                Width = WIDTH,
                Height = 20
            };
            Checkbox showGrp;
            BottomArea.Add(showGrp = new Checkbox
            (
                0x00D2,
                0x00D3,
                "Show Groups",
                0xFF,
                1153
            ));
            showGrp.IsChecked = World.SkillsGroupManager.IsActive;
            showGrp.ValueChanged += (sender, e) =>
            {
                World.SkillsGroupManager.IsActive = showGrp.IsChecked;
                ForceUpdate();
                World.SkillsGroupManager.Save();
            };




            Add(BottomArea);

            Add(_sortOrderIndicator = new GumpPic(0, 0, 0x985, 0));

            Add(resizeDrag = new Button(0, 0x837, 0x838, 0x838));
            resizeDrag.MouseDown += ResizeDrag_MouseDown;
            resizeDrag.MouseUp += ResizeDrag_MouseUp;
            resizeDrag.X = WIDTH - 10;
            resizeDrag.Y = _designHeight - 10;

            if(X == 0)
                X = last_x;
            if(Y == 0)
                Y = last_y;

            // Everything above was laid out in design space; bake GumpScale into the whole static tree.
            foreach (Control c in Children)
                ApplyScaleRecursive(c, scaleRootPosition: true);

            SetSortIndicatorPosition();
            ForceUpdate();
        }

        private void SetSortIndicatorPosition()
        {
            if (FindControls<NiceButton>().Any(s => s.ButtonParameter == last_button))
            {
                NiceButton btn = FindControls<NiceButton>()
                    .First(s => s.ButtonParameter == last_button);

                ushort g = (ushort)(_sortAsc ? 0x985 : 0x983);

                _sortOrderIndicator.Graphic = g;
                _sortOrderIndicator.X = btn.X + btn.Width - ScaleHelper.Scaled(15, GumpScale);
                _sortOrderIndicator.Y = btn.Y + ScaleHelper.Scaled(5, GumpScale);
                btn.IsSelected = true;
            }
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);
            last_x = X;
            last_y = Y;
        }

        public override void Dispose()
        {
            base.Dispose();
            last_x = X;
            last_y = Y;
        }

        public override GumpType GumpType => GumpType.SkillMenu;

        public override void OnButtonClick(int buttonID)
        {
            last_button = buttonID;
            if (_buttonsToSkillsValues.TryGetValue((Buttons)buttonID, out string fieldValue))
            {
                if (_sortField == fieldValue)
                {
                    _sortAsc = !_sortAsc;
                }

                _sortField = fieldValue;
            }

            SetSortIndicatorPosition();

            _updateSkillsNeeded = true;
        }

        private void BuildGump()
        {
            _totalReal = 0;
            _totalValue = 0;
            _databox.Clear();

            foreach (SkillListEntry entry in _skillListEntries)
            {
                entry.Clear();
                entry.Dispose();
            }

            _skillListEntries.Clear();
            PropertyInfo pi = typeof(Skill).GetProperty(_sortField);

            if (World.SkillsGroupManager.IsActive)
            {
                World.SkillsGroupManager.Groups.Sort((s1, s2) =>
                {
                    Match m1 = Regex.Match(s1.Name, "^\\d+");
                    Match m2 = Regex.Match(s2.Name, "^\\d+");
                    if (!m1.Success || !m2.Success)
                    {
                        return s1.Name.CompareTo(s2.Name);
                    }

                    if (!int.TryParse(m1.Value, out int v1) || !int.TryParse(m2.Value, out int v2))
                    {
                        return s1.Name.CompareTo(s2.Name);
                    }
                    return v1.CompareTo(v2);

                });
                if (_sortAsc)
                {
                    World.SkillsGroupManager.Groups.Reverse();
                }

                foreach (SkillsGroup g in World.SkillsGroupManager.Groups)
                {
                    var skillEntries = new List<SkillListEntry>();
                    var a = new Area();
                    a.AcceptMouseInput = true;
                    a.WantUpdateSize = false;
                    a.CanMove = true;
                    a.Height = 26;
                    a.Width = WIDTH - 26;
                    a.Tag = g.IsMaximized;
                    a.MouseUp += (sender, e) =>
                    {
                        g.IsMaximized = !g.IsMaximized;
                        var _a = (Area)sender;
                        bool newState = !(bool)_a.Tag;
                        _a.Tag = newState;
                        foreach (SkillListEntry entry in skillEntries)
                        {
                            entry.IsVisible = newState;
                        }
                        _databox.WantUpdateSize = true;
                        _databox.ReArrangeChildren();
                    };


                    var skills = new List<Skill>();
                    for (int i = 0; i < g.Count; i++)
                    {
                        byte index = g.GetSkill(i);
                        if (index < Client.Game.UO.FileManager.Skills.SkillsCount)
                        {
                            skills.Add(World.Player.Skills[index]);
                        }
                    }

                    skills = skills.OrderBy(s => pi.GetValue(s, null)).ToList();
                    if (_sortAsc)
                    {
                        skills.Reverse();
                    }

                    float grpReal = skills.Sum(s => s.Base);
                    float grpVal = skills.Sum(s => s.Value);
                    _totalReal += grpReal;
                    _totalValue += grpVal;
                    ;

                    foreach (Skill s in skills)
                    {
                        skillEntries.Add(new SkillListEntry(World, this, s));
                    }
                    a.Add
                    (
                            new ResizePic(0x0BB8)
                            {
                                X = 1,
                                Y = 3,
                                Width = 180,
                                Height = 22
                            }
                    );
                    StbTextBox _textbox;
                    a.Add
                    (
                        _textbox = new StbTextBox
                        (
                            3,
                            -1,
                            200,
                            false,
                            FontStyle.Fixed
                        )
                        {
                            X = 5,
                            Y = 3,
                            Width = 180,
                            Height = 17,
                            IsEditable = false
                        }
                    );

                    _textbox.SetText(g.Name);
                    _textbox.IsEditable = false;

                    _textbox.MouseDown += (s, e) =>
                    {
                        if (!g.IsMaximized)
                        {
                            a.InvokeMouseUp(e.Location, e.Button);
                        }
                        UIManager.KeyboardFocusControl = _textbox;
                        _textbox.SetKeyboardFocus();
                        _textbox.IsEditable = true;
                        _textbox.AllowSelection = true;
                    };

                    _textbox.FocusLost += (s, e) =>
                    {
                        _textbox.IsEditable = false;
                        _textbox.AllowSelection = false;
                        UIManager.KeyboardFocusControl = null;
                        UIManager.SystemChat.SetFocus();
                    };
                    _textbox.TextChanged += (s, e) =>
                    {
                        g.Name = _textbox.Text;
                    };
                    a.Add(new Label(grpReal.ToString("F1"), true, 1153) { X = 205, Y = 3 });
                    a.Add(new Label(grpVal.ToString("F1"), true, 1153) { X = 255, Y = 3 });

                    _databox.Add(a);
                    foreach (SkillListEntry entry in skillEntries)
                    {
                        entry.IsVisible = g.IsMaximized;
                        _skillListEntries.Add(entry);
                        _databox.Add(entry);
                    }
                }
            }
            else
            {
                var sortSkills = new List<Skill>(World.Player.Skills.OrderBy(x => pi.GetValue(x, null)));
                if (_sortAsc)
                {
                    sortSkills.Reverse();
                }
                foreach (Skill skill in sortSkills)
                {
                    _totalReal += skill.Base;
                    _totalValue += skill.Value;
                    _skillListEntries.Add(new SkillListEntry(World, this, skill));
                }
                foreach (SkillListEntry entry in _skillListEntries)
                {
                    _databox.Add(entry);
                }
            }


            // Entries/group headers were built in design space; scale each subtree before the databox
            // stacks them so ReArrangeChildren works entirely in scaled space.
            foreach (Control c in _databox.Children)
                ApplyScaleRecursive(c, scaleRootPosition: true);

            _databox.WantUpdateSize = true;
            _databox.ReArrangeChildren();

            int realX = ScaleHelper.Scaled(205, GumpScale);
            int valueX = ScaleHelper.Scaled(255, GumpScale);
            int bottomY = Height - ScaleHelper.Scaled(20, GumpScale);

            Add(real = new Label(_totalReal.ToString("F1"), true, 1153) { X = realX, Y = bottomY, AcceptMouseInput = false});
            Add(value = new Label(_totalValue.ToString("F1"), true, 1153) { X = valueX, Y = bottomY, AcceptMouseInput = false});
            real.SetInternalScale(GumpScale);
            value.SetInternalScale(GumpScale);

            SetSortIndicatorPosition();
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("sortasc", _sortAsc.ToString());
            writer.WriteAttributeString("sortfield", _sortField);
            writer.WriteAttributeString("lastbutton", last_button.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            if(xml.HasAttribute("sortasc"))
                bool.TryParse(xml.GetAttribute("sortasc"), out _sortAsc);
            if(xml.HasAttribute("sortfield"))
                _sortField = xml.GetAttribute("sortfield");
            if(xml.HasAttribute("lastbutton"))
                int.TryParse(xml.GetAttribute("lastbutton"), out last_button);
        }

        private void ResizeDrag_MouseUp(object sender, Input.MouseEventArgs e) => Dragging = false;

        private void ResizeDrag_MouseDown(object sender, Input.MouseEventArgs e)
        {
            dragStartH = Height;
            Dragging = true;
        }

        public override void Update()
        {
            base.Update();

            if (_updateSkillsNeeded)
            {
                foreach (Label label in Children.OfType<Label>())
                {
                    label.Dispose();
                }

                BuildGump();

                _updateSkillsNeeded = false;
            }

            int steps = Mouse.LDragOffset.Y;

            if (Dragging && steps != 0)
            {
                Height = dragStartH + steps;
                int minHeight = ScaleHelper.Scaled(170, GumpScale);
                if (Height < minHeight)
                    Height = minHeight;
                // Persist the design-space height so the stored value is independent of the current scale.
                _designHeight = ScaleHelper.Unscaled(Height, GumpScale);
                ProfileManager.CurrentProfile.AdvancedSkillsGumpHeight = _designHeight;

                area.Height = Height - ScaleHelper.Scaled(60, GumpScale);
                background.Height = Height - ScaleHelper.Scaled(1, GumpScale);
                _databox.WantUpdateSize = true;
                resizeDrag.Y = Height - ScaleHelper.Scaled(11, GumpScale);
                real.Y = Height - ScaleHelper.Scaled(20, GumpScale);
                value.Y = Height - ScaleHelper.Scaled(20, GumpScale);
                BottomArea.Y = area.Height + area.Y - 1;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!IsVisible) return false;

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width,
                Height,
                hueVector
            );

            return base.Draw(batcher, x, y);
        }

        public void ForceUpdate() => _updateSkillsNeeded = true;

        private enum Buttons
        {
            SortName = 1,
            SortReal = 2,
            SortBase = 3,
            SortCap = 4,
            SortLock = 5,
        }
    }


    public class SkillListEntry : Control
    {
        private readonly SkillGumpAdvanced _gump;
        private readonly Button _activeUse;
        private readonly Skill _skill;
        public SkillListEntry(World world, SkillGumpAdvanced gump, Skill skill)
        {
            _gump = gump;
            Height = 20;
            var skillName = new Label(skill.Name, true, 1153, font: 3) {AcceptMouseInput = skill.IsClickable, CanMove = true};
            if(skill.IsClickable)
            {
                skillName.MouseDoubleClick += skillDoubleClick;

                void skillDoubleClick(object sender, MouseDoubleClickEventArgs e)
                {
                    GetSpellFloatingButton(_skill.Index)?.Dispose();

                    ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x24B8);

                    var skillButtonGump = new SkillButtonGump(
                        world,
                        _skill,
                        Mouse.LClickPosition.X - (gumpInfo.UV.Width >> 1),
                        Mouse.LClickPosition.Y - (gumpInfo.UV.Height >> 1)
                    );

                    UIManager.Add(skillButtonGump);
                    UIManager.AttemptDragControl(skillButtonGump, true);
                }
            }

            var skillValueBase = new Label(skill.Base.ToString(), true, 1153, font: 3);
            var skillValue = new Label(skill.Value.ToString(), true, 1153, font: 3);
            var skillCap = new Label(skill.Cap.ToString(), true, 1153, font: 3);

            _skill = skill;
            CanMove = true;
            AcceptMouseInput = true;

            if (skill.IsClickable)
            {
                Add
                (
                    _activeUse = new Button((int)Buttons.ActiveSkillUse, 0x837, 0x838)
                    {
                        X = 0,
                        Y = 4,
                        ButtonAction = ButtonAction.Activate
                    }
                );
            }

            skillName.X = 20;
            Add(skillName);

            skillValueBase.X = 205;
            Add(skillValueBase);

            skillValue.X = 255;
            Add(skillValue);

            skillCap.X = 305;
            Add(skillCap);

            var loc = new GumpPic(355, 4, (ushort)(skill.Lock == Lock.Up ? 0x983 : skill.Lock == Lock.Down ? 0x985 : 0x82C), 0);

            Add(loc);

            loc.MouseUp += (sender, e) =>
            {
                switch (_skill.Lock)
                {
                    case Lock.Up:
                        _skill.Lock = Lock.Down;
                        GameActions.ChangeSkillLockStatus((ushort)_skill.Index, (byte)Lock.Down);
                        loc.Graphic = 0x985;

                        break;

                    case Lock.Down:
                        _skill.Lock = Lock.Locked;
                        GameActions.ChangeSkillLockStatus((ushort)_skill.Index, (byte)Lock.Locked);
                        loc.Graphic = 0x82C;

                        break;

                    case Lock.Locked:
                        _skill.Lock = Lock.Up;
                        GameActions.ChangeSkillLockStatus((ushort)_skill.Index, (byte)Lock.Up);
                        loc.Graphic = 0x983;

                        break;
                }
            };
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (!_skill.IsClickable)
                base.OnDragEnd(x, y);
        }

        public override void OnMouseOver(int x, int y)
        {
            base.OnMouseOver(x, y);

            if (Mouse.LButtonPressed && Math.Abs(Mouse.LDragOffset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(Mouse.LDragOffset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
            {
                InvokeDragBegin(Mouse.Position);
            }
        }

        private static SkillButtonGump GetSpellFloatingButton(int id)
        {
            for (LinkedListNode<IGui> i = UIManager.Gumps.Last; i != null; i = i.Previous)
            {
                if (i.Value is SkillButtonGump g && g.SkillID == id)
                {
                    return g;
                }
            }

            return null;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons)buttonID)
            {
                case Buttons.ActiveSkillUse:
                    GameActions.UseSkill(_skill.Index);

                    break;
            }
        }

        private enum Buttons
        {
            ActiveSkillUse = 1
        }
    }
}
