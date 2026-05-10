// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    public class HealthbarCollectorGump : Gump
    {
        private readonly World _world;
        private const int WIDTH = 120;
        private const int MIN_HEIGHT = 70;
        private const int TOP_SECTION_HEIGHT = 50;
        private const int BORDER_WIDTH = 2;

        private AlphaBlendControl _background;
        private NiceButton _notorietiesButton;
        private NiceButton _sortButton;
        private ClickableColorBox _borderColorBox;
        private ModernScrollArea _scrollArea;
        private VBoxContainer _container;
        private ResizeHandle _resizeHandle;

        private readonly HashSet<NotorietyFlag> _enabledNotorieties = new();
        private readonly Dictionary<uint, CompactHealthBar> _healthbars = new();
        private ushort _borderHue;
        private bool _sortByDistance;
        private bool _sortRequested;
        private bool _filterParty;
        private bool _filterPets;

        public HealthbarCollectorGump(World world) : base(world, 0, 0)
        {
            _world = world;
            Width = WIDTH;
            Height = 300;

            CanMove = true;
            CanCloseWithRightClick = true;
            Build();
            EventSink.NotorietyFlagChanged += EventSinkOnNotorietyFlagChanged;
            EventSink.MobileCreated += EventSinkOnMobileCreated;
        }

        private void EventSinkOnMobileCreated(object sender, Mobile mob)
        {
            if (MobileMatchesFilter(mob))
                AddMobileHealthbar(mob);
        }

        private void EventSinkOnNotorietyFlagChanged(uint serial, NotorietyFlag e)
        {
            Entity ent = _world.Get(serial);

            if (ent is not Mobile mob) return;

            if (MobileMatchesFilter(mob))
                AddMobileHealthbar(mob);
        }

        public override bool AcceptMouseInput { get; set; } = true;

        private void Build()
        {
            // Create alpha blend background - full width behind border
            _background = new AlphaBlendControl(0.75f)
            {
                X = 0,
                Y = 0,
                Width = Width,
                Height = Height,
                Hue = 0,
                AcceptMouseInput = true,
                CanMove = true
            };
            Add(_background);

            // Title label
            var titleLabel = new Label("Healthbar Collector", true, 0x0481, font: 1)
            {
                X = 5,
                Y = 8
            };
            Add(titleLabel);

            // Notorieties button
            _notorietiesButton = new NiceButton(5, 28, 55, 20, ButtonAction.Activate, "Filter")
            {
                IsSelectable = false,
                ButtonParameter = 0
            };
            _notorietiesButton.MouseUp += OnNotorietiesButtonClick;
            Add(_notorietiesButton);

            // Sort button
            _sortButton = new NiceButton(62, 28, 40, 20, ButtonAction.Activate, "Sort")
            {
                IsSelectable = true,
                ButtonParameter = 1
            };
            _sortButton.MouseUp += OnSortButtonClick;
            Add(_sortButton);

            // Create border color picker
            _borderColorBox = new ClickableColorBox(_world, WIDTH - 22, 28, 16, 16, _borderHue, true)
            {
                AcceptMouseInput = true
            };
            _borderColorBox.OnHueChanged += OnBorderHueChanged;
            Add(_borderColorBox);

            // Create scroll area with VBoxContainer
            _container = new VBoxContainer(WIDTH - BORDER_WIDTH * 2 - 4, 2, 2);

            _scrollArea = new ModernScrollArea(
                BORDER_WIDTH + 2,
                TOP_SECTION_HEIGHT,
                WIDTH - BORDER_WIDTH * 2 - 4,
                Height - TOP_SECTION_HEIGHT - BORDER_WIDTH - 20
            );
            _scrollArea.Add(_container);
            Add(_scrollArea);

            // Add resize handle at bottom
            _resizeHandle = new ResizeHandle(WIDTH / 2 - 8, Height - 18);
            Add(_resizeHandle);
        }

        private void OnBorderHueChanged(object sender, ushort hue)
        {
            if (_borderColorBox != null && _borderColorBox.Hue != _borderHue) _borderHue = _borderColorBox.Hue;
        }

        public override GumpType GumpType => GumpType.HealthBarCollector;

        private void OnNotorietiesButtonClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left)
                return;

            // Create context menu with all notoriety types
            var contextMenu = new ContextMenuControl(this);

            // Add Party filter option
            contextMenu.Add(
                "Party",
                () => TogglePartyFilter(),
                canBeSelected: true,
                defaultValue: _filterParty
            );

            // Add Pets filter option
            contextMenu.Add(
                "Pets",
                () => TogglePetsFilter(),
                canBeSelected: true,
                defaultValue: _filterPets
            );

            NotorietyFlag[] notorieties =
            {
                NotorietyFlag.Innocent,
                NotorietyFlag.Ally,
                NotorietyFlag.Gray,
                NotorietyFlag.Criminal,
                NotorietyFlag.Enemy,
                NotorietyFlag.Murderer,
                NotorietyFlag.Invulnerable,
                NotorietyFlag.Unknown
            };

            foreach (NotorietyFlag flag in notorieties)
            {
                NotorietyFlag capturedFlag = flag;
                bool isSelected = _enabledNotorieties.Contains(capturedFlag);

                contextMenu.Add(
                    capturedFlag.ToString(),
                    () => ToggleNotoriety(capturedFlag),
                    canBeSelected: true,
                    defaultValue: isSelected
                );
            }

            contextMenu.Show();
        }

        private void OnSortButtonClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left)
                return;

            _sortByDistance = !_sortByDistance;
            _sortButton.IsSelected = _sortByDistance;

            if (_sortByDistance)
                SortHealthbarsByDistance();
        }

        private void ToggleNotoriety(NotorietyFlag flag)
        {
            if (!_enabledNotorieties.Add(flag))
                _enabledNotorieties.Remove(flag);

            // Rebuild the healthbar list when notorieties are changed
            RebuildHealthbarList();
        }

        private void TogglePartyFilter()
        {
            _filterParty = !_filterParty;
            RebuildHealthbarList();
        }

        private void TogglePetsFilter()
        {
            _filterPets = !_filterPets;
            RebuildHealthbarList();
        }

        public void RequestSorting()
        {
            if (!_sortByDistance)
                return;

            _sortRequested = true;
        }

        private void SortHealthbarsByDistance()
        {
            _sortRequested = false;

            if (_healthbars.Count < 2 || !_sortByDistance)
                return;

            // Get all healthbars and sort by distance
            var sortedBars = _healthbars.Values
                .OrderBy(bar => bar.Distance)
                .ToList();

            // Remove all from container without disposing
            foreach (CompactHealthBar bar in sortedBars) _container.Remove(bar);

            // Re-add in sorted order
            foreach (CompactHealthBar bar in sortedBars) _container.Add(bar);

            _container.Reposition();
        }

        private void RebuildHealthbarList()
        {
            // Clear existing healthbars
            foreach (CompactHealthBar bar in _healthbars.Values)
            {
                bar.Dispose();
                _container.Remove(bar);
            }

            _healthbars.Clear();

            bool hasAnyFilter = _enabledNotorieties.Count > 0 || _filterParty || _filterPets;
            if (!hasAnyFilter) return;

            // Add healthbars for mobiles that match enabled filters
            foreach (Mobile mobile in World.Mobiles.Values)
                if (mobile != null && !mobile.IsDestroyed && mobile.Serial != World.Player?.Serial)
                    if (MobileMatchesFilter(mobile))
                        AddMobileHealthbar(mobile);

            _container.Reposition();
        }

        private bool MobileMatchesFilter(Mobile mobile)
        {
            // Check party filter
            if (_filterParty && World.Party.Contains(mobile.Serial))
                return true;

            // Check pets filter (IsRenamable indicates player's pet)
            if (_filterPets && mobile.IsRenamable)
                return true;

            // Check notoriety filter
            if (_enabledNotorieties.Contains(mobile.NotorietyFlag))
                return true;

            return false;
        }

        public static void CheckAndAddMobile(World world, uint serial)
        {
            Entity ent = world.Get(serial);

            if (ent is not Mobile mob) return;

            foreach (HealthbarCollectorGump collectorGump in UIManager.Gumps.OfType<HealthbarCollectorGump>())
                if (collectorGump.MobileMatchesFilter(mob))
                    collectorGump.AddMobileHealthbar(mob);
        }

        public static void MobileDestroyed(uint serial)
        {
            foreach (HealthbarCollectorGump collectorGump in UIManager.Gumps.OfType<HealthbarCollectorGump>()) collectorGump.RemoveMobile(serial);
        }

        private void AddMobileHealthbar(Mobile mobile)
        {
            if (mobile == null || _healthbars.ContainsKey(mobile.Serial))
                return;

            // Don't add the player
            if (mobile.Serial == World.Player?.Serial)
                return;

            // Create compact healthbar
            var compactBar = new CompactHealthBar(World, mobile.Serial, this);
            _healthbars[mobile.Serial] = compactBar;
            _container.Add(compactBar);
        }

        public void RemoveMobile(uint serial)
        {
            if (_healthbars.TryGetValue(serial, out CompactHealthBar bar))
            {
                bar.Dispose();
                _healthbars.Remove(serial);
            }
        }

        private new void SetHeight(int height)
        {
            Height = height;

            _background?.Height = Height;

            _scrollArea?.UpdateHeight(Height - TOP_SECTION_HEIGHT - BORDER_WIDTH - 20);
        }

        public override void PreDraw()
        {
            base.PreDraw();

            if (_resizeHandle != null && _resizeHandle.IsDragging)
            {
                int newHeight = _resizeHandle.Y + 18;
                if (newHeight >= MIN_HEIGHT) SetHeight(newHeight);
            }

            if(_sortRequested)
                SortHealthbarsByDistance();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y)) return false;

            // Draw border as plain lines
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(_borderHue, false, 1.0f);

            // Top border
            batcher.Draw(
                SolidColorTextureCache.GetTexture(Color.White),
                new Rectangle(x, y, Width, BORDER_WIDTH),
                hueVector
            );

            // Bottom border
            batcher.Draw(
                SolidColorTextureCache.GetTexture(Color.White),
                new Rectangle(x, y + Height - BORDER_WIDTH, Width, BORDER_WIDTH),
                hueVector
            );

            // Left border
            batcher.Draw(
                SolidColorTextureCache.GetTexture(Color.White),
                new Rectangle(x, y, BORDER_WIDTH, Height),
                hueVector
            );

            // Right border
            batcher.Draw(
                SolidColorTextureCache.GetTexture(Color.White),
                new Rectangle(x + Width - BORDER_WIDTH, y, BORDER_WIDTH, Height),
                hueVector
            );

            return true;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            // Save enabled notorieties as comma-separated flags
            if (_enabledNotorieties.Count > 0)
                writer.WriteAttributeString("notorieties",
                    string.Join(",", _enabledNotorieties.Select(n => (int)n)));

            // Save border hue
            writer.WriteAttributeString("borderHue", _borderHue.ToString());

            // Save sort state
            writer.WriteAttributeString("sortByDistance", _sortByDistance.ToString());

            // Save party and pets filters
            writer.WriteAttributeString("filterParty", _filterParty.ToString());
            writer.WriteAttributeString("filterPets", _filterPets.ToString());

            writer.WriteAttributeString("height", Height.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            // Restore notorieties
            string notorietiesStr = xml.GetAttribute("notorieties");
            if (!string.IsNullOrEmpty(notorietiesStr))
            {
                _enabledNotorieties.Clear();
                foreach (string s in notorietiesStr.Split(','))
                    if (int.TryParse(s, out int value))
                        _enabledNotorieties.Add((NotorietyFlag)value);
            }

            // Restore border hue
            if (ushort.TryParse(xml.GetAttribute("borderHue"), out ushort hue))
            {
                _borderHue = hue;
                _borderColorBox?.Hue = hue;
                OnBorderHueChanged(null, hue);
            }

            // Restore sort state
            if (bool.TryParse(xml.GetAttribute("sortByDistance"), out bool sortByDistance))
            {
                _sortByDistance = sortByDistance;
                _sortButton?.IsSelected = sortByDistance;
            }

            // Restore party and pets filters
            if (bool.TryParse(xml.GetAttribute("filterParty"), out bool filterParty))
                _filterParty = filterParty;

            if (bool.TryParse(xml.GetAttribute("filterPets"), out bool filterPets))
                _filterPets = filterPets;

            // Restore height
            if (int.TryParse(xml.GetAttribute("height"), out int height))
            {
                SetHeight(height);
                _resizeHandle.X = WIDTH / 2 - 8;
                _resizeHandle.Y = Height - 18;
            }

            // Rebuild healthbar list based on restored notorieties
            RebuildHealthbarList();
        }

        public override void Dispose()
        {
            EventSink.NotorietyFlagChanged -= EventSinkOnNotorietyFlagChanged;
            EventSink.MobileCreated -= EventSinkOnMobileCreated;
            // Dispose all compact healthbars
            foreach (CompactHealthBar bar in _healthbars.Values) bar.Dispose();

            _healthbars.Clear();

            base.Dispose();
        }

        private class ResizeHandle : Control
        {
            private bool _isDragging;
            private int _dragStartY;
            private int _startY;

            public ResizeHandle(int x, int y)
            {
                X = x;
                Y = y;
                Width = 16;
                Height = 16;
                CanMove = false;
            }

            public override bool AcceptMouseInput { get; set; } = true;

            public bool IsDragging => _isDragging;

            public override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    _isDragging = true;
                    _dragStartY = Mouse.Position.Y;
                    _startY = Y;
                }
            }

            public override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left) _isDragging = false;
            }

            public override void Update()
            {
                base.Update();

                if (_isDragging)
                {
                    int deltaY = Mouse.Position.Y - _dragStartY;
                    int newY = _startY + deltaY;

                    // Calculate the minimum Y position based on MIN_HEIGHT
                    int minY = MIN_HEIGHT - 18;

                    // Clamp Y to valid range
                    if (newY < minY) newY = minY;

                    Y = newY;
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                // Draw resize handle (horizontal lines)
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, 0.5f);

                for (int i = 0; i < 3; i++)
                    batcher.Draw(
                        SolidColorTextureCache.GetTexture(Color.White),
                        new Rectangle(x, y + i * 4, Width, 2),
                        hueVector
                    );

                return true;
            }
        }

        private class CompactHealthBar : Control
        {
            private const int BAR_WIDTH = 100;
            private const int BAR_HEIGHT = 16;
            private readonly World _world;
            private readonly HealthbarCollectorGump _parent;
            private readonly Label _nameLabel, _percentLabel;
            private readonly HealthBarLine _hpBar;
            private readonly Mobile _mobile;
            private readonly Button _buttonHeal1,  _buttonHeal2;
            private int _lastPercent;
            public int Distance;

            private uint Serial { get; }

            public override bool AcceptMouseInput { get; set; } = true;

            public CompactHealthBar(World world, uint serial, HealthbarCollectorGump parent)
            {
                _world = world;
                _parent = parent;
                Serial = serial;

                Width = 100;
                Height = 30;
                CanMove = true;

                Entity entity = world.Get(serial);
                if (entity == null || entity is not Mobile mob)
                {
                    Dispose();
                    return;
                }

                _mobile =  mob;
                SetTooltip(mob);
                Distance = entity.Distance;

                // Name label (centered)
                ushort hue = Notoriety.GetHue((NotorietyFlag)(entity as Mobile)?.NotorietyFlag);
                _nameLabel = new Label(string.Empty, true, hue, font: 1, style: FontStyle.BlackBorder)
                {
                    X = 0,
                    Y = 0,
                    Width = BAR_WIDTH
                };
                SetName();
                Add(_nameLabel);

                // HP background (red/gray bar)
                var hpBackground = new HealthBarLine(0, 16, BAR_WIDTH, BAR_HEIGHT, Color.DarkRed);
                Add(hpBackground);

                // HP foreground (blue bar)
                _hpBar = new HealthBarLine(0, 16, BAR_WIDTH, BAR_HEIGHT, Color.DodgerBlue);
                Add(_hpBar);

                _percentLabel = new Label(string.Empty, true, 0, font: 1, style: FontStyle.BlackBorder)
                {
                    X = 5, Y = 15
                };
                Add(_percentLabel);

                Add(_buttonHeal1 = new Button(0, 0x0938, 0x093A, 0x0938)
                {
                    ButtonAction = ButtonAction.Activate,
                    X = BAR_WIDTH - 30,
                    Y = 14
                });

                Add(_buttonHeal2 = new Button(1, 0x0939, 0x093A, 0x0939)
                {
                    ButtonAction = ButtonAction.Activate,
                    X = BAR_WIDTH - 15,
                    Y = 14

                });

                CheckQuickHealButtons();

                WantUpdateSize = false;
            }

            private void CheckQuickHealButtons()
            {
                bool visible = _lastPercent < 100 &&
                               ((_mobile.NotorietyFlag is not NotorietyFlag.Invulnerable
                                   and not NotorietyFlag.Enemy
                                   and not NotorietyFlag.Murderer
                                   and not NotorietyFlag.Criminal
                                   and not NotorietyFlag.Gray) || _mobile.IsRenamable);

                _buttonHeal1.IsVisible = visible;
                _buttonHeal2.IsVisible = visible;
            }

            private void SetName()
            {
                if(Distance != _mobile.Distance)
                {
                    Distance = _mobile.Distance;
                    _parent.RequestSorting();
                }

                _nameLabel.Text = $"{_mobile.Name} ({Distance})";
            }

            public override void PreDraw()
            {
                base.PreDraw();

                if (_mobile.IsDestroyed)
                {
                    _parent.RemoveMobile(Serial);
                    return;
                }

                // Update name if changed
                if ((!string.IsNullOrEmpty(_mobile.Name) && _nameLabel.Text != _mobile.Name) || Distance != _mobile.Distance) SetName();

                // Update HP bar width
                if (_mobile.HitsMax > 0)
                {
                    int hpWidth = CalculatePercents(_mobile.HitsMax, _mobile.Hits, BAR_WIDTH);
                    _hpBar.BarWidth = hpWidth;

                    if (hpWidth > 0 && _lastPercent != hpWidth)
                    {
                        _lastPercent = hpWidth;

                        _percentLabel.Text = _lastPercent < 100 ? hpWidth.ToString() + "%" : string.Empty;

                        CheckQuickHealButtons();
                    }

                    // Change color based on status
                    if (_mobile.IsPoisoned)
                        _hpBar.BarColor = SolidColorTextureCache.GetTexture(Color.LimeGreen);
                    else if (_mobile.IsYellowHits)
                        _hpBar.BarColor = SolidColorTextureCache.GetTexture(Color.Orange);
                    else
                        _hpBar.BarColor = SolidColorTextureCache.GetTexture(Color.DodgerBlue);

                    // Update notoriety color
                    ushort hue = Notoriety.GetHue(_mobile.NotorietyFlag);
                    if (_nameLabel.Hue != hue) _nameLabel.Hue = hue;
                }
            }

            public override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    if(_world.TargetManager.IsTargeting)
                        _world.TargetManager.Target(Serial);

                    else if (Keyboard.Alt && !ProfileManager.CurrentProfile.DisableAutoFollowAlt) //Auto follow
                    {
                        ProfileManager.CurrentProfile.FollowingMode = true;
                        ProfileManager.CurrentProfile.FollowingTarget = Serial;
                    }
                    else if (!_world.Player.InWarMode)
                    {
                        _world.DelayedObjectClickManager.Set(
                            Serial,
                            Mouse.Position.X,
                            Mouse.Position.Y,
                            Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK
                        );
                    }

                    if (ProfileManager.CurrentProfile.SingleClickMobileSetsLastTarget)
                        World.Instance.TargetManager.LastTargetInfo.SetEntity(Serial);
                }
                base.OnMouseDown(x, y, button);
            }

            protected override void OnMouseEnter(int x, int y)
            {
                if (_mobile != null && !_mobile.IsDestroyed)
                {
                    SelectedObject.HealthbarObject = _mobile;
                    SelectedObject.Object = _mobile;
                }

                base.OnMouseEnter(x, y);
            }

            public override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    Entity entity = _world.Get(Serial);
                    if (entity != null)
                    {
                        if (entity != _world.Player)
                        {
                            if (_world.Player.InWarMode)
                                GameActions.Attack(_world, entity);
                            else if (!GameActions.OpenCorpse(_world, entity)) GameActions.DoubleClick(_world, entity);
                        }
                        else
                            GameActions.DoubleClick(_world, entity);
                    }
                    return true;
                }
                return false;
            }

            public override void OnButtonClick(int buttonID)
            {
                switch (buttonID)
                {
                    case 0:
                        GameActions.QuickHeal(_world, Serial);
                        break;

                    case 1:
                        GameActions.QuickCure(_world, Serial);
                        break;
                }

                Mouse.CancelDoubleClick = true;
                Mouse.LastLeftButtonClickTime = 0;
            }

            private static int CalculatePercents(int max, int current, int maxValue)
            {
                if (max > 0)
                {
                    max = current * 100 / max;

                    if (max > 100) max = 100;

                    if (max > 1) max = maxValue * max / 100;
                }

                return max;
            }

            private Vector3 _backgroundHueVector = new(0, 0, 0.3f);

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (IsDisposed) return false;

                if (MouseIsOver)
                {
                    batcher.Draw(SolidColorTextureCache.GetTexture(Color.White), new Rectangle(x, y, Width, Height), _backgroundHueVector);
                }

                return base.Draw(batcher, x, y);
            }

            private class HealthBarLine : Control
            {
                private Texture2D _texture;
                public int BarWidth { get; set; }

                public Texture2D BarColor
                {
                    set => _texture = value;
                }

                public HealthBarLine(int x, int y, int maxWidth, int height, Color color)
                {
                    X = x;
                    Y = y;
                    Width = maxWidth;
                    Height = height;
                    BarWidth = maxWidth;
                    _texture = SolidColorTextureCache.GetTexture(color);
                    CanMove = true;
                    AcceptMouseInput = false;
                }

                public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                {
                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, 1.0f);

                    batcher.Draw(
                        _texture,
                        new Rectangle(x, y, BarWidth, Height),
                        hueVector
                    );

                    return true;
                }
            }
        }
    }
}
