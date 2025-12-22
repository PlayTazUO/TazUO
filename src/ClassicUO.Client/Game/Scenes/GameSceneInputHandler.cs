#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Network.Encryption;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene : Scene
    {
        private bool _isDraggingSelection = false;
        private Point _selectionStart;
        private Point _selectionEnd;


        private bool DragSelectModifierActive()
        {
            Keyboard.RefreshModifiers();

            // src: https://github.com/andreakarasho/ClassicUO/issues/621
            // drag-select should be disabled when using nameplates
            if ((Keyboard.Ctrl && Keyboard.Shift) && ProfileManager.CurrentProfile.DragSelect_NameplateModifier == 0)
            {
                return false;
            }

            if (ProfileManager.CurrentProfile.DragSelectModifierKey == 0)
            {
                return true;
            }

            if (ProfileManager.CurrentProfile.DragSelectModifierKey == 1 && Keyboard.Ctrl)
            {
                return true;
            }

            if (ProfileManager.CurrentProfile.DragSelectModifierKey == 2 && Keyboard.Shift)
            {
                return true;
            }

            if (ProfileManager.CurrentProfile.DragSelectModifierKey == 3 && Keyboard.Alt)
            {
                return true;
            }

            return false;
        }

        
        private void DoDragSelect()
        {
            Keyboard.RefreshModifiers();

            bool ctrl = Keyboard.Ctrl;
            bool shift = Keyboard.Shift;
            bool alt = Keyboard.Alt;

            if (_selectionStart.X > Mouse.Position.X)
            {
                _selectionEnd.X = _selectionStart.X;
                _selectionStart.X = Mouse.Position.X;
            }
            else
            {
                _selectionEnd.X = Mouse.Position.X;
            }

            if (_selectionStart.Y > Mouse.Position.Y)
            {
                _selectionEnd.Y = _selectionStart.Y;
                _selectionStart.Y = Mouse.Position.Y;
            }
            else
            {
                _selectionEnd.Y = Mouse.Position.Y;
            }

            if (_selectionEnd.X - _selectionStart.X < 5 || _selectionEnd.Y - _selectionStart.Y < 5)
            {
                return;
            }

            foreach (Entity e in World.Map.GetEntities())
            {
                if (e == null)
                {
                    continue;
                }

                if (!e.IsVisible || e.IsDestroyed)
                {
                    continue;
                }

                if (e is Item item)
                {
                    if (!item.IsMulti)
                    {
                        continue;
                    }

                    if (!item.Graphic.HasValue)
                    {
                        continue;
                    }

                    if (item.Graphic.Value != 0x4000)
                    {
                        continue;
                    }

                    if (item.Layer != 0)
                    {
                        continue;
                    }
                }
                else if (!(e is Mobile))
                {
                    continue;
                }

                if (e is Mobile mob && mob.IsDead)
                {
                    continue;
                }

                if (ProfileManager.CurrentProfile.DragSelect_Nameplates)
                {
                    if (ProfileManager.CurrentProfile.DragSelect_NameplateModifier != 0)
                    {
                        // 1 = ctrl
                        if (ProfileManager.CurrentProfile.DragSelect_NameplateModifier == 1 && !ctrl)
                        {
                            continue;
                        }

                        // 2 = shift
                        if (ProfileManager.CurrentProfile.DragSelect_NameplateModifier == 2 && !shift)
                        {
                            continue;
                        }

                        // 3 = alt
                        if (ProfileManager.CurrentProfile.DragSelect_NameplateModifier == 3 && !alt)
                        {
                            continue;
                        }
                    }

                    if (e is Mobile nameMobile)
                    {
                        if (!nameMobile.IsHuman)
                        {
                            continue;
                        }

                        if (nameMobile.NotorietyFlag == NotorietyFlag.Invulnerable)
                        {
                            continue;
                        }
                    }
                }

                if (ProfileManager.CurrentProfile.DragSelect_PlayersOnly && ProfileManager.CurrentProfile.DragSelect_PlayersModifier != 0)
                {
                    // 1 = ctrl
                    if (ProfileManager.CurrentProfile.DragSelect_PlayersModifier == 1 && !ctrl)
                    {
                        continue;
                    }

                    // 2 = shift
                    if (ProfileManager.CurrentProfile.DragSelect_PlayersModifier == 2 && !shift)
                    {
                        continue;
                    }

                    // 3 = alt
                    if (ProfileManager.CurrentProfile.DragSelect_PlayersModifier == 3 && !alt)
                    {
                        continue;
                    }
                }

                if (ProfileManager.CurrentProfile.DragSelect_MonstersOnly && ProfileManager.CurrentProfile.DragSelect_MonstersModifier != 0)
                {
                    // 1 = ctrl
                    if (ProfileManager.CurrentProfile.DragSelect_MonstersModifier == 1 && !ctrl)
                    {
                        continue;
                    }

                    // 2 = shift
                    if (ProfileManager.CurrentProfile.DragSelect_MonstersModifier == 2 && !shift)
                    {
                        continue;
                    }

                    // 3 = alt
                    if (ProfileManager.CurrentProfile.DragSelect_MonstersModifier == 3 && !alt)
                    {
                        continue;
                    }
                }

                if (e is Mobile mobile)
                {
                    if (ProfileManager.CurrentProfile.DragSelect_PlayersOnly && !mobile.IsHuman)
                    {
                        continue;
                    }

                    if (ProfileManager.CurrentProfile.DragSelect_MonstersOnly && mobile.IsHuman)
                    {
                        continue;
                    }
                }

                Rectangle rect = e.Bounds;

                if (rect.Intersects(new Rectangle(_selectionStart.X, _selectionStart.Y, _selectionEnd.X - _selectionStart.X, _selectionEnd.Y - _selectionStart.Y)))
                {
                    if (e is Mobile mobileEntity)
                    {
                        UIManager.Add(new HealthBarGump(mobileEntity));
                    }
                    else if (e is Item itemEntity && itemEntity.IsMulti)
                    {
                        UIManager.Add(new HealthBarGump(itemEntity));
                    }
                }
            }
        }
    }
}
