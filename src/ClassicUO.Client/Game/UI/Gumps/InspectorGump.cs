// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using SDL3;

namespace ClassicUO.Game.UI.Gumps
{
    public class InspectorGump : Gump
    {
        private const int WIDTH = 500;
        private const int HEIGHT = 400;
        private readonly GameObject _obj;

        public InspectorGump(World world, GameObject obj) : base(world, 0, 0)
        {
            X = 200;
            Y = 100;
            _obj = obj;
            CanMove = true;
            AcceptMouseInput = false;
            CanCloseWithRightClick = true;

            Add
            (
                new BorderControl
                (
                    0,
                    0,
                    WIDTH,
                    HEIGHT,
                    4
                )
            );

            Add
            (
                new GumpPicTiled
                (
                    4,
                    4,
                    WIDTH - 8,
                    HEIGHT - 8,
                    0x0A40
                )
                {
                    Alpha = 0.5f
                }
            );

            Add
            (
                new GumpPicTiled
                (
                    4,
                    4,
                    WIDTH - 8,
                    HEIGHT - 8,
                    0x0A40
                )
                {
                    Alpha = 0.5f
                }
            );

            Add(new Label(ResGumps.ObjectInformation, true, 1153, font: 3) { X = 20, Y = 10 });

            Add
            (
                new Line
                (
                    20,
                    30,
                    WIDTH - 50,
                    1,
                    0xFFFFFFFF
                )
            );

            Add
            (
                new NiceButton
                (
                    WIDTH - 115,
                    5,
                    100,
                    25,
                    ButtonAction.Activate,
                    TazLang.Get("inspector_clipboard", "To clipboard")
                )
                {
                    ButtonParameter = 0
                }
            );

            var scrollArea = new ScrollArea
            (
                20,
                35,
                WIDTH - 40,
                HEIGHT - 45,
                true
            )
            {
                AcceptMouseInput = true
            };

            Add(scrollArea);

            var databox = new DataBox(0, 0, 1, 1);
            databox.WantUpdateSize = true;
            scrollArea.Add(databox);

            Dictionary<string, string> dict = GetGameObjectProperties(obj);

            if (dict != null)
            {
                int startX = 5;
                int startY = 5;

                foreach (KeyValuePair<string, string> item in dict.OrderBy(s => s.Key))
                {
                    var label = new Label
                    (
                        item.Key + ":",
                        true,
                        33,
                        font: 1,
                        style: FontStyle.BlackBorder
                    )
                    {
                        X = startX,
                        Y = startY
                    };

                    databox.Add(label);

                    int height = label.Height;

                    label = new Label
                    (
                        item.Value,
                        true,
                        1153,
                        font: 1,
                        style: FontStyle.BlackBorder,
                        maxwidth: WIDTH - 65 - 200
                    )
                    {
                        X = startX + 200,
                        Y = startY,
                        AcceptMouseInput = true,
                        CanMove = true
                    };

                    label.MouseUp += OnLabelClick;

                    if (label.Height > 0)
                    {
                        height = label.Height;
                    }

                    databox.Add(label);

                    databox.Add
                    (
                        new Line
                        (
                            startX,
                            startY + height + 2,
                            WIDTH - 65,
                            1,
                            Color.Gray.PackedValue
                        )
                    );

                    startY += height + 4;
                }
            }

            //databox.ReArrangeChildren();
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0) // dump
            {
                Dictionary<string, string> dict = GetGameObjectProperties(_obj);

                if (dict != null)
                {
                    StringBuilder sb = new();
                    sb.AppendLine("###################################################");
                    sb.AppendLine($"CUO version: {CUOEnviroment.Version}");
                    sb.AppendLine($"OBJECT TYPE: {_obj.GetType()}");

                    foreach (KeyValuePair<string, string> item in dict.OrderBy(s => s.Key)) sb.AppendLine($"{item.Key} = {item.Value}");

                    sb.AppendLine("###################################################");
                    sb.AppendLine("");

                    sb.ToString().CopyToClipboard();
                    GameActions.Print(World, TazLang.Get("gump_inspector_copied", "Copied to clipboard!"), Constants.HUE_SUCCESS);
                }
            }
        }

        private void OnLabelClick(object sender, MouseEventArgs e)
        {
            var l = (Label) sender;

            if (e.Button == MouseButtonType.Left && l != null)
            {
                SDL.SDL_SetClipboardText(l.Text);
                GameActions.Print(World, TazLang.Get("gump_inspector_copiedwithtext_fmt", "Copied to clipboard: {0}", new[] { l.Text }));
            }
        }

        private Dictionary<string, string> GetGameObjectProperties(GameObject obj)
        {
            var dict = new Dictionary<string, string>();

            dict[TazLang.Get("gump_inspector_graphics", "Graphics")] = $"0x{obj.Graphic:X4}";
            dict[TazLang.Get("gump_inspector_hue", "Hue")] = $"{obj.Hue}";
            dict[TazLang.Get("gump_inspector_position", "Position")] = $"X={obj.X}, Y={obj.Y}, Z={obj.Z}";
            dict[TazLang.Get("gump_inspector_priorityz", "PriorityZ")] = obj.PriorityZ.ToString();
            dict[TazLang.Get("gump_inspector_distance", "Distance")] = obj.Distance.ToString();
            dict[TazLang.Get("gump_inspector_allowedtodraw", "AllowedToDraw")] = obj.AllowedToDraw.ToString();
            dict[TazLang.Get("gump_inspector_alphahue", "AlphaHue")] = obj.AlphaHue.ToString();
            dict[TazLang.Get("gump_inspector_haslineofsightfromplayer", "HasLineOfSightFromPlayer")] = obj.HasLineOfSightFrom().ToString();

            switch (obj)
            {
                case Mobile mob:

                    dict[TazLang.Get("gump_inspector_type", "Type")] = "Mobile";
                    dict[TazLang.Get("gump_inspector_serial", "Serial")] = $"0x{mob.Serial:X8}";
                    dict[TazLang.Get("gump_inspector_flags", "Flags")] = mob.Flags.ToString();
                    dict[TazLang.Get("gump_inspector_notoriety", "Notoriety")] = mob.NotorietyFlag.ToString();
                    dict[TazLang.Get("gump_inspector_title", "Title")] = mob.Title ?? string.Empty;
                    dict[TazLang.Get("gump_inspector_name", "Name")] = mob.Name ?? string.Empty;
                    dict[TazLang.Get("gump_inspector_hp", "HP")] = $"{mob.Hits}/{mob.HitsMax}";
                    dict[TazLang.Get("gump_inspector_mana", "Mana")] = $"{mob.Mana}/{mob.ManaMax}";
                    dict[TazLang.Get("gump_inspector_stamina", "Stamina")] = $"{mob.Stamina}/{mob.StaminaMax}";
                    dict[TazLang.Get("gump_inspector_speedmode", "SpeedMode")] = mob.SpeedMode.ToString();
                    dict[TazLang.Get("gump_inspector_race", "Race")] = mob.Race.ToString();
                    dict[TazLang.Get("gump_inspector_isrenamable", "IsRenamable")] = mob.IsRenamable.ToString();
                    dict[TazLang.Get("gump_inspector_direction", "Direction")] = mob.Direction.ToString();
                    dict[TazLang.Get("gump_inspector_isdead", "IsDead")] = mob.IsDead.ToString();
                    dict[TazLang.Get("gump_inspector_isdrivingaboat", "IsDrivingABoat")] = mob.IsDrivingBoat.ToString();
                    dict[TazLang.Get("gump_inspector_ismounted", "IsMounted")] = mob.IsMounted.ToString();

                    break;

                case Item it:

                    dict[TazLang.Get("gump_inspector_type", "Type")] = "Item";
                    dict[TazLang.Get("gump_inspector_serial", "Serial")] = $"0x{it.Serial:X8}";
                    dict[TazLang.Get("gump_inspector_flags", "Flags")] = it.Flags.ToString();
                    dict[TazLang.Get("gump_inspector_hp", "HP")] = $"{it.Hits}/{it.HitsMax}";
                    dict[TazLang.Get("gump_inspector_iscoins", "IsCoins")] = it.IsCoin.ToString();
                    dict[TazLang.Get("gump_inspector_amount", "Amount")] = it.Amount.ToString();
                    dict[TazLang.Get("gump_inspector_container", "Container")] = $"0x{it.Container:X8}";
                    dict[TazLang.Get("gump_inspector_layer", "Layer")] = it.Layer.ToString();
                    dict[TazLang.Get("gump_inspector_price", "Price")] = it.Price.ToString();
                    dict[TazLang.Get("gump_inspector_direction", "Direction")] = it.Direction.ToString();
                    dict[TazLang.Get("gump_inspector_ismulti", "IsMulti")] = it.IsMulti.ToString();
                    dict[TazLang.Get("gump_inspector_multigraphic", "MultiGraphic")] = $"0x{it.MultiGraphic:X4}";
                    dict[TazLang.Get("gump_inspector_isimpassable", "IsImpassable")] = it.ItemData.IsImpassable.ToString();
                    dict[TazLang.Get("gump_inspector_customname", "CustomName")] = it.CustomName;

                    break;

                case Static st:
                    ref StaticTiles staticData = ref Client.Game.UO.FileManager.TileData.StaticData[st.OriginalGraphic];
                    dict[TazLang.Get("gump_inspector_type", "Type")] = "Static";
                    dict[TazLang.Get("gump_inspector_isvegetation", "IsVegetation")] = st.IsVegetation.ToString();
                    dict[TazLang.Get("gump_inspector_iswall", "IsWall")] = staticData.IsWall.ToString();
                    dict[TazLang.Get("gump_inspector_isimpassable", "IsImpassable")] = staticData.IsImpassable.ToString();

                    break;

                case Multi multi:

                    dict[TazLang.Get("gump_inspector_type", "Type")] = "Multi";
                    dict[TazLang.Get("gump_inspector_state", "State")] = multi.State.ToString();
                    dict[TazLang.Get("gump_inspector_ismovable", "IsMovable")] = multi.IsMovable.ToString();
                    dict[TazLang.Get("gump_inspector_isimpassable", "IsImpassable")] = multi.ItemData.IsImpassable.ToString();
                    dict[TazLang.Get("gump_inspector_iswall", "IsWall")] = multi.ItemData.IsWall.ToString();

                    break;

                case Land land:

                    dict[TazLang.Get("gump_inspector_type", "Type")] = "Land";
                    dict[TazLang.Get("gump_inspector_isflat", "IsFlat")] = (!land.IsStretched).ToString();
                    dict[TazLang.Get("gump_inspector_normalleft", "NormalLeft")] = land.NormalLeft.ToString();
                    dict[TazLang.Get("gump_inspector_normalright", "NormalRight")] = land.NormalRight.ToString();
                    dict[TazLang.Get("gump_inspector_normaltop", "NormalTop")] = land.NormalTop.ToString();
                    dict[TazLang.Get("gump_inspector_normalbottom", "NormalBottom")] = land.NormalBottom.ToString();
                    dict[TazLang.Get("gump_inspector_minz", "MinZ")] = land.MinZ.ToString();
                    dict[TazLang.Get("gump_inspector_avgz", "AvgZ")] = land.AverageZ.ToString();
                    dict[TazLang.Get("gump_inspector_yoffsets", "YOffsets")] = land.YOffsets.ToString();
                    dict[TazLang.Get("gump_inspector_isimpassable", "IsImpassable")] = land.TileData.IsImpassable.ToString();

                    break;
            }

            return dict;
        }
    }
}
