// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.GameObjects
{
    public partial class Mobile
    {
        private const int SIT_OFFSET_Y = 4;
        private EquipConvData? _equipConvData;
        private int _characterFrameStartY;
        private int _startCharacterWaistY;
        private int _startCharacterKneesY;

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (IsDestroyed || !AllowedToDraw || !IsVisible)
            {
                return false;
            }

            // Check if player character should be hidden via Hide HUD system
            if (this is PlayerMobile player && !player.IsVisible)
            {
                return false;
            }

            Profile profile = _profile;
            Managers.AuraManager auraManager = World.AuraManager;
            int clientViewRange = World.ClientViewRange;

            bool charSitting = false;
            ushort overridenHue = 0;

            AnimationsLoader.SittingInfoData seatData = AnimationsLoader.SittingInfoData.Empty;
            _equipConvData = null;
            FrameInfo.X = 0;
            FrameInfo.Y = 0;
            FrameInfo.Width = 0;
            FrameInfo.Height = 0;

            posY -= 3;
            int drawX = posX + (int)Offset.X + 22;
            int drawY = posY + (int)(Offset.Y - Offset.Z) + 22;

            // Cache boolean values
            bool isDead = IsDead;
            bool isHidden = IsHidden;
            bool isPlayer = IsPlayer;
            bool hasShadow = !isDead && !isHidden && profile.ShadowsEnabled;
            bool inParty = InParty;
            bool isHuman = IsHuman;
            bool isGargoyle = IsGargoyle;
            bool isSelected = ReferenceEquals(SelectedObject.Object, this);

            if (auraManager.IsEnabled)
            {
                auraManager.Draw(
                    batcher,
                    drawX,
                    drawY,
                    profile.PartyAura && inParty
                        ? profile.PartyAuraHue
                        : Notoriety.GetHue(NotorietyFlag),
                    depth + 1f
                );
            }

            float finalAlpha = isPlayer && profile.PlayerConstantAlpha != 100 ? profile.PlayerConstantAlpha / 100f : AlphaHue / 255f;
            Vector3 hueVec = ShaderHueTranslator.GetHueVector(0, false, finalAlpha);

            // Simplified hue override logic
            if (isSelected && profile.HighlightGameObjects)
            {
                overridenHue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                hueVec.Y = 1;
            }
            else if (SelectedObject.HealthbarObject == this)
            {
                overridenHue = Notoriety.GetHue(NotorietyFlag);
            }
            else if (profile.NoColorObjectsOutOfRange && Distance > clientViewRange)
            {
                overridenHue = Constants.OUT_RANGE_COLOR;
                hueVec.Y = 1;
            }
            else if (World.Player.IsDead && profile.EnableBlackWhiteEffect)
            {
                overridenHue = Constants.DEAD_RANGE_COLOR;
                hueVec.Y = 1;
            }
            else if (isHidden)
            {
                overridenHue = profile.HiddenBodyHue;
                hueVec = ShaderHueTranslator.GetHueVector(0, false, (float)profile.HiddenBodyAlpha / 100);
            }
            else
            {
                overridenHue = 0;

                if (isDead && !isHuman)
                {
                    overridenHue = 0x0386;
                }
                else if (!isDead)
                {
                    // Use single if-else chain instead of nested conditions
                    if (profile.HighlightMobilesByInvul && NotorietyFlag != NotorietyFlag.Invulnerable && IsYellowHits)
                    {
                        overridenHue = profile.InvulnerableHue;
                    }
                    else if (profile.HighlightMobilesByParalize && IsParalyzed && NotorietyFlag != NotorietyFlag.Invulnerable)
                    {
                        overridenHue = profile.ParalyzedHue;
                    }
                    else if (profile.HighlightMobilesByPoisoned && IsPoisoned)
                    {
                        overridenHue = profile.PoisonHue;
                    }
                }
            }

            if (!isPlayer)
            {
                Managers.TargetManager targetManager = World.TargetManager;
                if ((targetManager.IsTargeting && isSelected) || (Serial == targetManager.LastAttack && !profile.DisableGrayEnemies))
                    overridenHue = Notoriety.GetHue(NotorietyFlag);
                else if (inParty && profile.OverridePartyAndGuildHue)
                    overridenHue = profile.FriendHue;
            }

            ProcessSteps(out byte dir);
            byte layerDir = dir;

            Client.Game.UO.Animations.GetAnimDirection(ref dir, ref IsFlipped);

            ushort graphic = GetGraphicForAnimation();
            byte animGroup = GetGroupForAnimation(this, graphic, true);
            byte animIndex = AnimIndex;

            Item mount = Mount;
            if (mount != null)
            {
                // Validate mount still exists - don't modify state during rendering
                if (World.Items.Get(mount.Serial) == null)
                {
                    mount = null;
                }
            }

            sbyte mountOffsetY = 0;

            if (isHuman && mount != null && mount.Graphic != 0x3E96)
            {
                ushort mountGraphic = mount.GetGraphicForAnimation();
                byte animGroupMount = 0;

                if (
                    mountGraphic != 0xFFFF
                    && mountGraphic < Client.Game.UO.Animations.MaxAnimationCount
                )
                {
                    if (Mounts.TryGet(mount.Graphic, out MountInfo mountInfo))
                    {
                        mountOffsetY = mountInfo.OffsetY;
                    }

                    // Calculate animation group once
                    animGroupMount = GetGroupForAnimation(this, mountGraphic);

                    if (hasShadow)
                    {
                        DrawInternal(
                            batcher,
                            this,
                            null,
                            drawX,
                            drawY + 10,
                            hueVec,
                            IsFlipped,
                            animIndex,
                            true,
                            graphic,
                            animGroup,
                            dir,
                            isHuman,
                            false,
                            false,
                            false,
                            depth,
                            mountOffsetY,
                            overridenHue,
                            charSitting
                        );

                        DrawInternal(
                            batcher,
                            this,
                            mount,
                            drawX,
                            drawY,
                            hueVec,
                            IsFlipped,
                            animIndex,
                            true,
                            mountGraphic,
                            animGroupMount,
                            dir,
                            isHuman,
                            false,
                            false,
                            false,
                            depth,
                            mountOffsetY,
                            overridenHue,
                            charSitting
                        );
                    }

                    DrawInternal(
                        batcher,
                        this,
                        mount,
                        drawX,
                        drawY,
                        hueVec,
                        IsFlipped,
                        animIndex,
                        false,
                        mountGraphic,
                        animGroupMount,
                        dir,
                        isHuman,
                        false,
                        true,
                        false,
                        depth,
                        mountOffsetY,
                        overridenHue,
                        charSitting
                    );

                    drawY += mountOffsetY;
                }
            }
            else
            {
                if (TryGetSittingInfo(out seatData))
                {
                    animGroup = (byte)PeopleAnimationGroup.Stand;
                    animIndex = 0;

                    ProcessSteps(out dir);

                    Client.Game.UO.FileManager.Animations.FixSittingDirection(
                        ref dir,
                        ref IsFlipped,
                        ref drawX,
                        ref drawY,
                        ref seatData
                    );

                    drawY += SIT_OFFSET_Y;

                    if (dir == 3)
                    {
                        if (isGargoyle)
                        {
                            drawY -= 30 - SIT_OFFSET_Y;
                            animGroup = 42;
                        }
                        else
                        {
                            animGroup = 25;
                        }
                    }
                    else if (isGargoyle)
                    {
                        animGroup = 42;
                    }
                    else
                    {
                        charSitting = true;
                    }
                }
                else if (hasShadow)
                {
                    DrawInternal(
                        batcher,
                        this,
                        null,
                        drawX,
                        drawY,
                        hueVec,
                        IsFlipped,
                        animIndex,
                        true,
                        graphic,
                        animGroup,
                        dir,
                        isHuman,
                        false,
                        false,
                        false,
                        depth,
                        mountOffsetY,
                        overridenHue,
                        charSitting
                    );
                }
            }

            DrawInternal(
                batcher,
                this,
                null,
                drawX,
                drawY,
                hueVec,
                IsFlipped,
                animIndex,
                false,
                graphic,
                animGroup,
                dir,
                isHuman,
                false,
                false,
                isGargoyle,
                depth,
                mountOffsetY,
                overridenHue,
                charSitting
            );

            Profiler.EnterContext("SECTION 5");
            if (!IsEmpty)
            {
                // Cache profile properties for hot loop
                bool hiddenLayersEnabled = profile.HiddenLayersEnabled;
                bool hideLayersForSelf = profile.HideLayersForSelf;

                for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
                {
                    Layer layer = LayerOrder.UsedLayers[layerDir, i];

                    Profiler.EnterContext("FIND LAYER");
                    Item item = FindItemByLayer(layer);
                    Profiler.ExitContext("FIND LAYER");

                    if (item == null)
                    {
                        continue;
                    }

                    if (IsDead && (layer == Layer.Hair || layer == Layer.Beard))
                    {
                        continue;
                    }

                    if (isHuman)
                    {
                        Profiler.EnterContext("HIDDEN LAYERS");
                        if (hiddenLayersEnabled && profile.HiddenLayers.Contains((int)layer) && ((hideLayersForSelf && IsPlayer) || !hideLayersForSelf))
                        {
                            continue;
                        }
                        Profiler.ExitContext("HIDDEN LAYERS");

                        if (IsCovered(this, layer))
                        {
                            continue;
                        }

                        if (item.ItemData.AnimID != 0)
                        {
                            graphic = item.ItemData.AnimID;

                            if (isGargoyle)
                            {
                                FixGargoyleEquipments(ref graphic);
                            }

                            Profiler.EnterContext("EQUIP_CONV");
                            if (
                                Client.Game.UO.FileManager.Animations.EquipConversions.TryGetValue(
                                    Graphic,
                                    out Dictionary<ushort, EquipConvData> map
                                )
                            )
                            {
                                if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                                {
                                    _equipConvData = data;
                                    graphic = data.Graphic;
                                }
                            }
                            Profiler.ExitContext("EQUIP_CONV");

                            Profiler.EnterContext("EQUIP_DRAW");

                            Profiler.EnterContext("GROUPFORANIM");
                            byte group = isGargoyle /*&& item.ItemData.IsWeapon*/
                                        && seatData.Graphic == 0
                                ? GetGroupForAnimation(this, graphic, true)
                                : animGroup;
                            Profiler.ExitContext("GROUPFORANIM");

                            DrawInternal(
                                batcher,
                                this,
                                item,
                                drawX,
                                drawY,
                                hueVec,
                                IsFlipped,
                                animIndex,
                                false,
                                graphic,
                                group,
                                dir,
                                isHuman,
                                true,
                                false,
                                isGargoyle,
                                depth,
                                mountOffsetY,
                                overridenHue,
                                charSitting
                            );
                            Profiler.ExitContext("EQUIP_DRAW");
                        }
                        else
                        {
                            if (item.ItemData.IsLight)
                            {
                                GameScene.Instance?.AddLight(this, item, drawX, drawY);
                            }
                        }

                        _equipConvData = null;
                    }
                    else
                    {
                        if (item.ItemData.IsLight)
                        {
                            GameScene.Instance?.AddLight(this, item, drawX, drawY);
                        }
                    }
                }
            }

            FrameInfo.X = Math.Abs(FrameInfo.X);
            FrameInfo.Y = Math.Abs(FrameInfo.Y);
            FrameInfo.Width = FrameInfo.X + FrameInfo.Width;
            FrameInfo.Height = FrameInfo.Y + FrameInfo.Height;
            Profiler.ExitContext("SECTION 5");
            return true;
        }

        private ushort GetAnimationInfo(Item item, bool isGargoyle)
        {
            if (item.ItemData.AnimID != 0)
            {
                ushort graphic = item.ItemData.AnimID;

                if (isGargoyle)
                {
                    FixGargoyleEquipments(ref graphic);
                }

                if (
                    Client.Game.UO.FileManager.Animations.EquipConversions.TryGetValue(
                        Graphic,
                        out Dictionary<ushort, EquipConvData> map
                    )
                )
                {
                    if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                    {
                        _equipConvData = data;
                        graphic = data.Graphic;
                    }
                }

                return graphic;
            }

            return 0xFFFF;
        }

        private static void FixGargoyleEquipments(ref ushort graphic)
        {
            switch (graphic)
            {
                // gargoyle robe
                case 0x01D5:
                    graphic = 0x0156;

                    break;

                // gargoyle dead shroud
                case 0x03CA:
                    graphic = 0x0223;

                    break;

                // gargoyle spellbook
                case 0x03D8:
                    graphic = 329;

                    break;

                // gargoyle necrobook
                case 0x0372:
                    graphic = 330;

                    break;

                // gargoyle chivalry book
                case 0x0374:
                    graphic = 328;

                    break;

                // gargoyle bushido book
                case 0x036F:
                    graphic = 327;

                    break;

                // gargoyle ninjitsu book
                case 0x036E:
                    graphic = 328;

                    break;

                // gargoyle masteries book
                case 0x0426:
                    graphic = 0x042B;

                    break;
                //NOTE: gargoyle mysticism book seems ok. Mha!


                /* into the mobtypes.txt file of 7.0.90+ client version we have:
                 *
                 *   1529 	EQUIPMENT	0		# EQUIP_Shield_Pirate_Male_H
                 *   1530 	EQUIPMENT	0		# EQUIP_Shield_Pirate_Female_H
                 *   1531 	EQUIPMENT	10000	# Equip_Shield_Pirate_Male_G
                 *   1532 	EQUIPMENT	10000	# Equip_Shield_Pirate_Female_G
                 *
                 *   This means that graphic 0xA649 [pirate shield] has 4 tiledata infos.
                 *   Standard client handles it automatically without any issue.
                 *   Maybe it's hardcoded into the client
                 */

                // EQUIP_Shield_Pirate_Male_H
                case 1529:
                    graphic = 1531;

                    break;

                // EQUIP_Shield_Pirate_Female_H
                case 1530:
                    graphic = 1532;

                    break;
            }
        }

        private static bool GetTexture(
            ushort graphic,
            byte animGroup,
            ref byte animIndex,
            byte direction,
            out SpriteInfo spriteInfo,
            out bool isUOP
        )
        {
            spriteInfo = default;

            Span<SpriteInfo> frames = Client.Game.UO.Animations.GetAnimationFrames(
                graphic,
                animGroup,
                direction,
                out _,
                out isUOP
            );

            if (frames.Length == 0)
            {
                return false;
            }

            if (animIndex < 0)
            {
                animIndex = 0;
            }

            animIndex = (byte)(animIndex % frames.Length);

            spriteInfo = frames[animIndex];

            if (spriteInfo.Texture == null)
            {
                return false;
            }

            return true;
        }

        private static void DrawInternal(
            UltimaBatcher2D batcher,
            Mobile owner,
            Item entity,
            int x,
            int y,
            Vector3 hueVec,
            bool mirror,
            byte frameIndex,
            bool hasShadow,
            ushort id,
            byte animGroup,
            byte dir,
            bool isHuman,
            bool isEquip,
            bool isMount,
            bool forceUOP,
            float depth,
            sbyte mountOffset,
            ushort overridedHue,
            bool charIsSitting
        )
        {
            if (id >= Client.Game.UO.Animations.MaxAnimationCount || owner == null)
            {
                return;
            }

            Profiler.EnterContext("Get Anim Frames");
            Span<SpriteInfo> frames = Client.Game.UO.Animations.GetAnimationFrames(
                id,
                animGroup,
                dir,
                out ushort hueFromFile,
                out _,
                isEquip,
                false
            );
            Profiler.ExitContext("Get Anim Frames");

            if (hueFromFile == 0)
            {
                hueFromFile = overridedHue;
            }

            if (frames.Length == 0)
            {
                return;
            }

            if (frameIndex >= frames.Length)
            {
                frameIndex = (byte)(frames.Length - 1);
            }
            else if (frameIndex < 0)
            {
                frameIndex = 0;
            }

            ref SpriteInfo spriteInfo = ref frames[frameIndex % frames.Length];

            if (spriteInfo.Texture == null)
            {
                // Only continue for sitting characters without entity and no shadow
                if (!(charIsSitting && entity == null && !hasShadow))
                {
                    return;
                }
                // Skip position calculations, go directly to shadow check
            }
            else
            {
                // Position calculations only when texture exists
                if (mirror)
                {
                    x -= spriteInfo.UV.Width - spriteInfo.Center.X;
                }
                else
                {
                    x -= spriteInfo.Center.X;
                }

                y -= spriteInfo.UV.Height + spriteInfo.Center.Y;
            }

            if (hasShadow)
            {
                batcher.DrawShadow(
                    spriteInfo.Texture,
                    new Vector2(x, y),
                    spriteInfo.UV,
                    mirror,
                    depth
                );
            }
            else
            {
                ushort hue = overridedHue;
                bool partialHue = false;

                if (hue == 0)
                {
                    hue = entity?.Hue ?? owner.Hue;
                    partialHue = !isMount && entity != null && entity.ItemData.IsPartialHue;

                    if ((hue & 0x8000) != 0)
                    {
                        partialHue = true;
                        hue &= 0x7FFF;
                    }

                    if (hue == 0)
                    {
                        hue = hueFromFile;

                        if (hue == 0 && owner._equipConvData is EquipConvData equipData)
                        {
                            hue = equipData.Color;
                        }

                        partialHue = false;
                    }
                }

                hueVec = ShaderHueTranslator.GetHueVector(hue, partialHue, hueVec.Z);

                if (spriteInfo.Texture != null)
                {
                    var pos = new Vector2(x, y);
                    Rectangle rect = spriteInfo.UV;

                    if (charIsSitting)
                    {
                        Vector3 mod = owner.CalculateSitAnimation(y, entity, isHuman, ref spriteInfo);

                        batcher.DrawCharacterSitted(
                            spriteInfo.Texture,
                            pos,
                            rect,
                            mod,
                            hueVec,
                            mirror,
                            depth + 1f
                        );
                    }
                    else
                    {
                        int diffY = (spriteInfo.UV.Height + spriteInfo.Center.Y) - mountOffset;

                        int value = Math.Max(1, diffY);
                        int count = Math.Max((spriteInfo.UV.Height / value) + 1, 2);

                        rect.Height = Math.Min(value, rect.Height);
                        int remains = spriteInfo.UV.Height - rect.Height;

                        const int tiles = 2;

                        for (int i = 0; i < count; ++i)
                        {
                            batcher.Draw(
                                spriteInfo.Texture,
                                pos,
                                rect,
                                hueVec,
                                0f,
                                Vector2.Zero,
                                1f,
                                mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                                depth + 1f + (i * tiles)
                            );

                            pos.Y += rect.Height;
                            rect.Y += rect.Height;
                            rect.Height = remains;
                            remains -= rect.Height;
                        }
                    }

                    int xx = -spriteInfo.Center.X;
                    int yy = -(spriteInfo.UV.Height + spriteInfo.Center.Y + 3);

                    if (mirror)
                    {
                        xx = -(spriteInfo.UV.Width - spriteInfo.Center.X);
                    }

                    if (xx < owner.FrameInfo.X)
                    {
                        owner.FrameInfo.X = xx;
                    }

                    if (yy < owner.FrameInfo.Y)
                    {
                        owner.FrameInfo.Y = yy;
                    }

                    if (owner.FrameInfo.Width < xx + spriteInfo.UV.Width)
                    {
                        owner.FrameInfo.Width = xx + spriteInfo.UV.Width;
                    }

                    if (owner.FrameInfo.Height < yy + spriteInfo.UV.Height)
                    {
                        owner.FrameInfo.Height = yy + spriteInfo.UV.Height;
                    }
                }

                if (entity != null && entity.ItemData.IsLight)
                {
                    GameScene.Instance?.AddLight(owner, entity, mirror ? x + spriteInfo.UV.Width : x, y);
                }
            }
        }

        private Vector3 CalculateSitAnimation(
            int y,
            Item entity,
            bool isHuman,
            ref SpriteInfo spriteInfo
        )
        {
            var mod = new Vector3();

            const float UPPER_BODY_RATIO = 0.35f;
            const float MID_BODY_RATIO = 0.60f;
            const float LOWER_BODY_RATIO = 0.94f;

            if (entity == null && isHuman)
            {
                int frameHeight = spriteInfo.UV.Height;
                if (frameHeight == 0)
                {
                    frameHeight = 61;
                }

                _characterFrameStartY = y - (spriteInfo.Texture != null ? 0 : frameHeight - SIT_OFFSET_Y);
                _startCharacterWaistY = (int)(frameHeight * UPPER_BODY_RATIO) + _characterFrameStartY;
                _startCharacterKneesY = (int)(frameHeight * MID_BODY_RATIO) + _characterFrameStartY;

                if (spriteInfo.Texture == null)
                {
                    return mod;
                }
            }

            mod.X = UPPER_BODY_RATIO;
            mod.Y = MID_BODY_RATIO;
            mod.Z = LOWER_BODY_RATIO;

            if (entity != null)
            {
                float itemsEndY = y + spriteInfo.UV.Height;

                if (y >= _startCharacterWaistY)
                {
                    mod.X = 0;
                }
                else if (itemsEndY <= _startCharacterWaistY)
                {
                    mod.X = 1.0f;
                }
                else
                {
                    float upperBodyDiff = _startCharacterWaistY - y;
                    mod.X = upperBodyDiff / spriteInfo.UV.Height;

                    if (mod.X < 0)
                    {
                        mod.X = 0;
                    }
                }

                if (_startCharacterWaistY >= itemsEndY || y >= _startCharacterKneesY)
                {
                    mod.Y = 0;
                }
                else if (_startCharacterWaistY <= y && itemsEndY <= _startCharacterKneesY)
                {
                    mod.Y = 1.0f;
                }
                else
                {
                    float midBodyDiff;

                    if (y >= _startCharacterWaistY)
                    {
                        midBodyDiff = _startCharacterKneesY - y;
                    }
                    else if (itemsEndY <= _startCharacterKneesY)
                    {
                        midBodyDiff = itemsEndY - _startCharacterWaistY;
                    }
                    else
                    {
                        midBodyDiff = _startCharacterKneesY - _startCharacterWaistY;
                    }

                    mod.Y = mod.X + midBodyDiff / spriteInfo.UV.Height;

                    if (mod.Y < 0)
                    {
                        mod.Y = 0;
                    }
                }

                if (itemsEndY <= _startCharacterKneesY)
                {
                    mod.Z = 0;
                }
                else if (y >= _startCharacterKneesY)
                {
                    mod.Z = 1.0f;
                }
                else
                {
                    float lowerBodyDiff = itemsEndY - _startCharacterKneesY;
                    mod.Z = mod.Y + lowerBodyDiff / spriteInfo.UV.Height;

                    if (mod.Z < 0)
                    {
                        mod.Z = 0;
                    }
                }
            }

            return mod;
        }

        public override bool CheckMouseSelection()
        {
            Point position = RealScreenPosition;
            position.Y -= 3;
            position.X += (int)Offset.X + 22;
            position.Y += (int)(Offset.Y - Offset.Z) + 22;

            Rectangle r = FrameInfo;
            r.X = position.X - r.X;
            r.Y = position.Y - r.Y;

            if (!r.Contains(SelectedObject.TranslatedMousePositionByViewport))
            {
                return false;
            }

            bool isHuman = IsHuman;
            bool isGargoyle =
                Client.Game.UO.Version >= ClientVersion.CV_7000
                && (Graphic == 666 || Graphic == 667 || Graphic == 0x02B7 || Graphic == 0x02B6);

            Renderer.Animations.Animations animations = Client.Game.UO.Animations;

            ProcessSteps(out byte dir);
            byte layerDir = dir;
            bool isFlipped = IsFlipped;
            animations.GetAnimDirection(ref dir, ref isFlipped);

            ushort graphic = GetGraphicForAnimation();
            byte animGroup = GetGroupForAnimation(this, graphic, true);
            byte animIndex = AnimIndex;

            byte animGroupBackup = animGroup;
            byte animIndexBackup = animIndex;

            SpriteInfo spriteInfo;
            bool isUop;

            if (isHuman)
            {
                Item mount = Mount;
                if (mount != null)
                {
                    ushort mountGraphic = mount.GetGraphicForAnimation();

                    if (mountGraphic != 0xFFFF)
                    {
                        byte animGroupMount = GetGroupForAnimation(this, mountGraphic);

                        if (
                            GetTexture(
                                mountGraphic,
                                animGroupMount,
                                ref animIndex,
                                dir,
                                out spriteInfo,
                                out isUop
                            )
                        )
                        {
                            int x =
                                position.X
                                - (
                                    isFlipped
                                        ? spriteInfo.UV.Width - spriteInfo.Center.X
                                        : spriteInfo.Center.X
                                );
                            int y = position.Y - (spriteInfo.UV.Height + spriteInfo.Center.Y);

                            if (
                                animations.PixelCheck(
                                    mountGraphic,
                                    animGroupMount,
                                    dir,
                                    isUop,
                                    animIndex,
                                    isFlipped
                                        ? x
                                            + spriteInfo.UV.Width
                                            - SelectedObject.TranslatedMousePositionByViewport.X
                                        : SelectedObject.TranslatedMousePositionByViewport.X - x,
                                    SelectedObject.TranslatedMousePositionByViewport.Y - y
                                )
                            )
                            {
                                return true;
                            }

                            if (Mounts.TryGet(mount.Graphic, out MountInfo moutInfo))
                            {
                                position.Y += moutInfo.OffsetY;
                            }
                        }
                    }
                }
            }

            if (GetTexture(graphic, animGroup, ref animIndex, dir, out spriteInfo, out isUop))
            {
                int x =
                    position.X
                    - (isFlipped ? spriteInfo.UV.Width - spriteInfo.Center.X : spriteInfo.Center.X);
                int y = position.Y - (spriteInfo.UV.Height + spriteInfo.Center.Y);

                if (
                    animations.PixelCheck(
                        graphic,
                        animGroup,
                        dir,
                        isUop,
                        animIndex,
                        isFlipped
                            ? x
                                + spriteInfo.UV.Width
                                - SelectedObject.TranslatedMousePositionByViewport.X
                            : SelectedObject.TranslatedMousePositionByViewport.X - x,
                        SelectedObject.TranslatedMousePositionByViewport.Y - y
                    )
                )
                {
                    return true;
                }
            }

            if (!IsEmpty && isHuman)
            {
                for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
                {
                    Layer layer = LayerOrder.UsedLayers[layerDir, i];
                    Item item = FindItemByLayer(layer);

                    if (
                        item == null
                        || (IsDead && (layer == Layer.Hair || layer == Layer.Beard))
                        || IsCovered(this, layer)
                    )
                    {
                        continue;
                    }

                    graphic = GetAnimationInfo(item, isGargoyle);

                    if (graphic != 0xFFFF)
                    {
                        animGroup = animGroupBackup;
                        animIndex = animIndexBackup;

                        if (
                            GetTexture(
                                graphic,
                                animGroup,
                                ref animIndex,
                                dir,
                                out spriteInfo,
                                out isUop
                            )
                        )
                        {
                            int x =
                                position.X
                                - (
                                    isFlipped
                                        ? spriteInfo.UV.Width - spriteInfo.Center.X
                                        : spriteInfo.Center.X
                                );
                            int y = position.Y - (spriteInfo.UV.Height + spriteInfo.Center.Y);

                            if (
                                animations.PixelCheck(
                                    graphic,
                                    animGroup,
                                    dir,
                                    isUop,
                                    animIndex,
                                    isFlipped
                                        ? x
                                            + spriteInfo.UV.Width
                                            - SelectedObject.TranslatedMousePositionByViewport.X
                                        : SelectedObject.TranslatedMousePositionByViewport.X - x,
                                    SelectedObject.TranslatedMousePositionByViewport.Y - y
                                )
                            )
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsCovered(Mobile mobile, Layer layer)
        {
            if (mobile.IsEmpty)
            {
                return false;
            }

            switch (layer)
            {
                case Layer.Shoes:
                    Item pants = mobile.FindItemByLayer(Layer.Pants);
                    Item robe;

                    if (
                        mobile.FindItemByLayer(Layer.Legs) != null
                        || pants != null
                            && (
                                pants.Graphic == 0x1411 /*|| pants.Graphic == 0x141A*/
                            )
                    )
                    {
                        return true;
                    }
                    else
                    {
                        robe = mobile.FindItemByLayer(Layer.Robe);

                        if (
                            pants != null && (pants.Graphic == 0x0513 || pants.Graphic == 0x0514)
                            || robe != null && robe.Graphic == 0x0504
                        )
                        {
                            return true;
                        }
                    }

                    break;

                case Layer.Pants:

                    robe = mobile.FindItemByLayer(Layer.Robe);
                    pants = mobile.FindItemByLayer(Layer.Pants);

                    if (
                        mobile.FindItemByLayer(Layer.Legs) != null
                        || robe != null && robe.Graphic == 0x0504
                    )
                    {
                        return true;
                    }

                    if (
                        pants != null
                        && (
                            pants.Graphic == 0x01EB
                            || pants.Graphic == 0x03E5
                            || pants.Graphic == 0x03eB
                        )
                    )
                    {
                        Item skirt = mobile.FindItemByLayer(Layer.Skirt);

                        if (skirt != null && skirt.Graphic != 0x01C7 && skirt.Graphic != 0x01E4)
                        {
                            return true;
                        }

                        if (
                            robe != null
                            && robe.Graphic != 0x0229
                            && (robe.Graphic <= 0x04E7 || robe.Graphic > 0x04EB)
                        )
                        {
                            return true;
                        }
                    }

                    break;

                case Layer.Tunic:
                    robe = mobile.FindItemByLayer(Layer.Robe);
                    Item tunic = mobile.FindItemByLayer(Layer.Tunic);

                    if (tunic != null && tunic.Graphic == 0x0238)
                    {
                        return robe != null
                            && robe.Graphic != 0x9985
                            && robe.Graphic != 0x9986
                            && robe.Graphic != 0xA412
                            && robe.Graphic != 0xA2CB
                            && robe.Graphic != 0xA2CA;
                    }

                    break;

                case Layer.Torso:
                    robe = mobile.FindItemByLayer(Layer.Robe);

                    if (
                        robe != null
                        && robe.Graphic != 0
                        && robe.Graphic != 0x9985
                        && robe.Graphic != 0x9986
                        && robe.Graphic != 0xA412
                        && robe.Graphic != 0xA2CB
                        && robe.Graphic != 0xA2CA
                    )
                    {
                        return true;
                    }
                    else
                    {
                        tunic = mobile.FindItemByLayer(Layer.Tunic);

                        if (tunic != null && tunic.Graphic != 0x1541 && tunic.Graphic != 0x1542)
                        {
                            Item torso = mobile.FindItemByLayer(Layer.Torso);

                            if (
                                torso != null
                                && (torso.Graphic == 0x782A || torso.Graphic == 0x782B)
                            )
                            {
                                return true;
                            }
                        }
                    }

                    break;

                case Layer.Arms:
                    robe = mobile.FindItemByLayer(Layer.Robe);

                    return robe != null
                        && robe.Graphic != 0
                        && robe.Graphic != 0x9985
                        && robe.Graphic != 0x9986
                        && robe.Graphic != 0xA412
                        && robe.Graphic != 0xA2CB
                        && robe.Graphic != 0xA2CA;

                case Layer.Helmet:
                case Layer.Hair:
                    robe = mobile.FindItemByLayer(Layer.Robe);

                    if (robe != null)
                    {
                        if (robe.Graphic > 0x3173)
                        {
                            if (robe.Graphic == 0x4B9D || robe.Graphic == 0x7816)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if (robe.Graphic <= 0x2687)
                            {
                                if (robe.Graphic < 0x2683)
                                {
                                    return robe.Graphic >= 0x204E && robe.Graphic <= 0x204F;
                                }

                                return true;
                            }

                            if (robe.Graphic == 0x2FB9 || robe.Graphic == 0x3173)
                            {
                                return true;
                            }
                        }
                    }

                    break;
            }

            return false;
        }
    }
}
