using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    /// <summary>
    /// A resizable paperdoll control that displays a character body with equipment layers
    /// without requiring actual game objects (Items, Mobiles, or serials).
    /// </summary>
    public class StaticPaperDollView : Control
    {
        private static readonly Layer[] _layerOrder =
        {
            Layer.Cloak,
            Layer.Shirt,
            Layer.Pants,
            Layer.Shoes,
            Layer.Legs,
            Layer.Arms,
            Layer.Torso,
            Layer.Tunic,
            Layer.Ring,
            Layer.Bracelet,
            Layer.Face,
            Layer.Gloves,
            Layer.Skirt,
            Layer.Robe,
            Layer.Waist,
            Layer.Neck,
            Layer.Hair,
            Layer.Beard,
            Layer.Earrings,
            Layer.Helmet,
            Layer.OneHanded,
            Layer.TwoHanded,
            Layer.Talisman
        };

        private ushort _bodyGraphic;
        private ushort _bodyHue;
        private bool _isFemale;
        private Dictionary<Layer, EquipmentEntry> _equipment;
        private Vector2 _targetSize;
        private readonly bool _background;
        private float _scaleFactor = 1f;
        private int _contentWidth;
        private int _contentHeight;
        private bool _needsRecalculate = true;

        /// <summary>
        /// Creates a new StaticPaperDollView control.
        /// </summary>
        /// <param name="bodyGraphic">The body graphic ID (e.g., 0x0190 for male human, 0x0191 for female human)</param>
        /// <param name="bodyHue">The hue for the body</param>
        /// <param name="isFemale">Whether the character is female (affects equipment gump selection)</param>
        /// <param name="equipment">Dictionary mapping Layer to equipment info (animID and hue)</param>
        /// <param name="targetSize">The desired size for the control</param>
        public StaticPaperDollView(
            ushort bodyGraphic,
            ushort bodyHue,
            bool isFemale,
            Dictionary<Layer, EquipmentEntry> equipment,
            Vector2 targetSize,
            bool background = true)
        {
            _bodyGraphic = bodyGraphic;
            _bodyHue = bodyHue;
            _isFemale = isFemale;
            _equipment = equipment ?? new Dictionary<Layer, EquipmentEntry>();
            _targetSize = targetSize;
            _background = background;

            AcceptMouseInput = false;
            CanMove = false;

            Width = (int)targetSize.X;
            Height = (int)targetSize.Y;

            CalculateScaleFactor();
        }

        /// <summary>
        /// Gets or sets the body graphic.
        /// </summary>
        public ushort BodyGraphic
        {
            get => _bodyGraphic;
            set
            {
                if (_bodyGraphic != value)
                {
                    _bodyGraphic = value;
                    _needsRecalculate = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the body hue.
        /// </summary>
        public ushort BodyHue
        {
            get => _bodyHue;
            set => _bodyHue = value;
        }

        /// <summary>
        /// Gets or sets whether the character is female.
        /// </summary>
        public bool IsFemale
        {
            get => _isFemale;
            set
            {
                if (_isFemale != value)
                {
                    _isFemale = value;
                    _needsRecalculate = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the target size for the control.
        /// </summary>
        public Vector2 TargetSize
        {
            get => _targetSize;
            set
            {
                if (_targetSize != value)
                {
                    _targetSize = value;
                    Width = (int)value.X;
                    Height = (int)value.Y;
                    _needsRecalculate = true;
                }
            }
        }

        /// <summary>
        /// Sets the equipment dictionary.
        /// </summary>
        public void SetEquipment(Dictionary<Layer, EquipmentEntry> equipment) => _equipment = equipment ?? new Dictionary<Layer, EquipmentEntry>();

        /// <summary>
        /// Sets or updates a single equipment layer.
        /// </summary>
        public void SetEquipmentLayer(Layer layer, ushort animID, ushort hue, bool isPartialHue = false) => _equipment[layer] = new EquipmentEntry(animID, hue, isPartialHue);

        /// <summary>
        /// Removes an equipment layer.
        /// </summary>
        public void RemoveEquipmentLayer(Layer layer) => _equipment.Remove(layer);

        /// <summary>
        /// Clears all equipment.
        /// </summary>
        public void ClearEquipment() => _equipment.Clear();

        private void CalculateScaleFactor()
        {
            ushort bodyGumpId = GetBodyGumpId(_bodyGraphic);
            ref readonly SpriteInfo bodyInfo = ref Client.Game.UO.Gumps.GetGump(bodyGumpId);

            if (bodyInfo.Texture != null)
            {
                _contentWidth = bodyInfo.UV.Width;
                _contentHeight = bodyInfo.UV.Height;

                float scaleX = _targetSize.X / _contentWidth;
                float scaleY = _targetSize.Y / _contentHeight;
                _scaleFactor = System.Math.Min(scaleX, scaleY);
            }
            else
            {
                _contentWidth = (int)_targetSize.X;
                _contentHeight = (int)_targetSize.Y;
                _scaleFactor = 1f;
            }

            _needsRecalculate = false;
        }

        private ushort GetBodyGumpId(ushort mobileGraphic)
        {
            if (mobileGraphic == 0x0191 || mobileGraphic == 0x0193)
            {
                return 0x000D; // Female human
            }
            else if (mobileGraphic == 0x025D)
            {
                return 0x000E; // Female elf
            }
            else if (mobileGraphic == 0x025E)
            {
                return 0x000F; // Male elf
            }
            else if (mobileGraphic == 0x029A || mobileGraphic == 0x02B6)
            {
                return 0x029A; // Female gargoyle
            }
            else if (mobileGraphic == 0x029B || mobileGraphic == 0x02B7)
            {
                return 0x0299; // Male gargoyle
            }
            else if (mobileGraphic == 0x04E5)
            {
                return 0xC835;
            }
            else if (mobileGraphic == 0x03DB)
            {
                return 0x000C; // Ghost
            }
            else if (_isFemale)
            {
                return 0x000D; // Default female
            }
            else
            {
                return 0x000C; // Default male
            }
        }

        private ushort GetEquipmentGumpId(ushort animID)
        {
            int offset = _isFemale ? Constants.FEMALE_GUMP_OFFSET : Constants.MALE_GUMP_OFFSET;

            Client.Game.UO.Animations.ConvertBodyIfNeeded(ref _bodyGraphic);

            if (Client.Game.UO.FileManager.Animations.EquipConversions.TryGetValue(
                _bodyGraphic,
                out Dictionary<ushort, EquipConvData> dict))
            {
                if (dict.TryGetValue(animID, out EquipConvData data))
                {
                    if (data.Gump > Constants.MALE_GUMP_OFFSET)
                    {
                        animID = (ushort)(
                            data.Gump >= Constants.FEMALE_GUMP_OFFSET
                                ? data.Gump - Constants.FEMALE_GUMP_OFFSET
                                : data.Gump - Constants.MALE_GUMP_OFFSET);
                    }
                    else
                    {
                        animID = data.Gump;
                    }
                }
            }

            // Check if the gump exists, otherwise try the other gender
            int requested = animID + offset;
            if (requested > GumpsLoader.MAX_GUMP_DATA_INDEX_COUNT ||
                Client.Game.UO.Gumps.GetGump((ushort)requested).Texture == null)
            {
                offset = _isFemale ? Constants.MALE_GUMP_OFFSET : Constants.FEMALE_GUMP_OFFSET;
            }

            return (ushort)(animID + offset);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            if (_needsRecalculate)
            {
                CalculateScaleFactor();
            }

            if (_background)
            {
                Vector3 hue_vec = ShaderHueTranslator.GetHueVector(1, false, 0.6f);

                batcher.Draw
                (
                    SolidColorTextureCache.GetTexture(Color.White),
                    new Rectangle
                    (
                        x - 4,
                        y - 2,
                        (int)(TargetSize.X * _scaleFactor),
                        (int)(TargetSize.Y * _scaleFactor)
                    ),
                    hue_vec
                );

                hue_vec = ShaderHueTranslator.GetHueVector(0, false, 0.6f);

                batcher.DrawRectangle
                (
                    SolidColorTextureCache.GetTexture(Color.Gray),
                    x - 4,
                    y - 2,
                    (int)(TargetSize.X * _scaleFactor),
                    (int)(TargetSize.Y * _scaleFactor),
                    hue_vec
                );
            }

            // Draw body
            ushort bodyGumpId = GetBodyGumpId(_bodyGraphic);
            ushort bodyHue = _bodyHue;

            // Special case for ghost
            if (_bodyGraphic == 0x03DB)
            {
                bodyHue = 0x03EA;
            }

            DrawGump(batcher, bodyGumpId, bodyHue, x, y, true);

            // Draw ghost overlay if applicable
            if (_bodyGraphic == 0x03DB)
            {
                DrawGump(batcher, 0xC72B, _bodyHue, x, y, true);
            }

            // Draw equipment in layer order
            foreach (Layer layer in _layerOrder)
            {
                if (_equipment.TryGetValue(layer, out EquipmentEntry entry))
                {
                    ushort gumpId = GetEquipmentGumpId(entry.AnimID);
                    DrawGump(batcher, gumpId, entry.Hue, x, y, entry.IsPartialHue);
                }
            }

            return base.Draw(batcher, x, y);
        }

        private void DrawGump(UltimaBatcher2D batcher, ushort gumpId, ushort hue, int x, int y, bool isPartialHue)
        {
            ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(gumpId);

            if (gumpInfo.Texture == null)
            {
                return;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(hue, isPartialHue, Alpha, true);

            int scaledWidth = (int)(gumpInfo.UV.Width * _scaleFactor);
            int scaledHeight = (int)(gumpInfo.UV.Height * _scaleFactor);

            batcher.Draw(
                gumpInfo.Texture,
                new Rectangle(x, y, scaledWidth, scaledHeight),
                gumpInfo.UV,
                hueVector);
        }

        /// <summary>
        /// Represents an equipment entry with animation ID and hue.
        /// </summary>
        public readonly struct EquipmentEntry
        {
            public readonly ushort AnimID;
            public readonly ushort Hue;
            public readonly bool IsPartialHue;

            public EquipmentEntry(ushort animID, ushort hue, bool isPartialHue = false)
            {
                AnimID = animID;
                Hue = hue;
                IsPartialHue = isPartialHue;
            }
        }
    }
}
