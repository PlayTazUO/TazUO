using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

namespace ClassicUO.Game.Managers
{
    [JsonSerializable(typeof(ToolTipOverrideData))]
    internal class ToolTipOverrideData
    {
        public ToolTipOverrideData() { }
        public ToolTipOverrideData(int index, string searchText, string formattedText, int min1, int max1, int min2, int max2, byte layer)
        {
            Index = index;
            SearchText = DecodeUnicodeEscapes(searchText).Trim();
            FormattedText = DecodeUnicodeEscapes(formattedText).Trim();
            Min1 = min1;
            Max1 = max1;
            Min2 = min2;
            Max2 = max2;
            ItemLayer = (TooltipLayers)layer;
        }

        public int Index { get; }
        public string SearchText { get; set; }
        public string FormattedText { get; set; }
        public int Min1 { get; set; }
        public int Max1 { get; set; }
        public int Min2 { get; set; }
        public int Max2 { get; set; }
        public TooltipLayers ItemLayer { get; set; }

        public bool IsNew { get; set; } = false;

        public static ToolTipOverrideData Get(int index)
        {
            bool isNew = false;
            if (ProfileManager.CurrentProfile != null)
            {
                string searchText = "Weapon Damage", formattedText = "DMG /c[orange]{1} /cd- /c[red]{2}";
                int min1 = -1, max1 = 99, min2 = -1, max2 = 99;
                byte layer = (byte)TooltipLayers.Any;

                if (ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count > index)
                    searchText = ProfileManager.CurrentProfile.ToolTipOverride_SearchText[index];
                else isNew = true;

                if (ProfileManager.CurrentProfile.ToolTipOverride_NewFormat.Count > index)
                    formattedText = ProfileManager.CurrentProfile.ToolTipOverride_NewFormat[index];
                else isNew = true;

                if (ProfileManager.CurrentProfile.ToolTipOverride_MinVal1.Count > index)
                    min1 = ProfileManager.CurrentProfile.ToolTipOverride_MinVal1[index];
                else isNew = true;

                if (ProfileManager.CurrentProfile.ToolTipOverride_MinVal2.Count > index)
                    min2 = ProfileManager.CurrentProfile.ToolTipOverride_MinVal2[index];
                else isNew = true;

                if (ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1.Count > index)
                    max1 = ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1[index];
                else isNew = true;

                if (ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2.Count > index)
                    max2 = ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2[index];
                else isNew = true;

                if (ProfileManager.CurrentProfile.ToolTipOverride_Layer.Count > index)
                    layer = ProfileManager.CurrentProfile.ToolTipOverride_Layer[index];
                else isNew = true;

                ToolTipOverrideData data = new ToolTipOverrideData(index, searchText, formattedText, min1, max1, min2, max2, layer);

                if (isNew)
                {
                    data.IsNew = true;
                    data.Save();
                }
                return data;
            }
            return null;
        }

        public void Save()
        {
            if (ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_SearchText[Index] = SearchText;
            else ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Add(SearchText);

            if (ProfileManager.CurrentProfile.ToolTipOverride_NewFormat.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_NewFormat[Index] = FormattedText;
            else ProfileManager.CurrentProfile.ToolTipOverride_NewFormat.Add(FormattedText);

            if (ProfileManager.CurrentProfile.ToolTipOverride_MinVal1.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_MinVal1[Index] = Min1;
            else ProfileManager.CurrentProfile.ToolTipOverride_MinVal1.Add(Min1);

            if (ProfileManager.CurrentProfile.ToolTipOverride_MinVal2.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_MinVal2[Index] = Min2;
            else ProfileManager.CurrentProfile.ToolTipOverride_MinVal2.Add(Min2);

            if (ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1[Index] = Max1;
            else ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1.Add(Max1);

            if (ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2[Index] = Max2;
            else ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2.Add(Max2);

            if (ProfileManager.CurrentProfile.ToolTipOverride_Layer.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_Layer[Index] = (byte)ItemLayer;
            else ProfileManager.CurrentProfile.ToolTipOverride_Layer.Add((byte)ItemLayer);
        }

        public void Delete()
        {
            ProfileManager.CurrentProfile.ToolTipOverride_SearchText.RemoveAt(Index);
            ProfileManager.CurrentProfile.ToolTipOverride_NewFormat.RemoveAt(Index);
            ProfileManager.CurrentProfile.ToolTipOverride_MinVal1.RemoveAt(Index);
            ProfileManager.CurrentProfile.ToolTipOverride_MinVal2.RemoveAt(Index);
            ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1.RemoveAt(Index);
            ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2.RemoveAt(Index);
            ProfileManager.CurrentProfile.ToolTipOverride_Layer.RemoveAt(Index);
        }

        public static ToolTipOverrideData[] GetAllToolTipOverrides()
        {
            if (ProfileManager.CurrentProfile == null)
                return null;

            ToolTipOverrideData[] result = new ToolTipOverrideData[ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count];

            for (int i = 0; i < ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count; i++)
            {
                result[i] = Get(i);
            }

            return result;
        }

        public static void ExportOverrideSettings()
        {
            ToolTipOverrideData[] allData = GetAllToolTipOverrides();

            if (!CUOEnviroment.IsUnix)
            {
                Thread t = new Thread(() =>
                {
                    System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
                    saveFileDialog1.Filter = "Json|*.json";
                    saveFileDialog1.Title = "Save tooltip override settings";
                    saveFileDialog1.ShowDialog();

                    string result = JsonSerializer.Serialize(allData);

                    // If the file name is not an empty string open it for saving.
                    if (saveFileDialog1.FileName != "")
                    {
                        System.IO.FileStream fs =
                            (System.IO.FileStream)saveFileDialog1.OpenFile();
                        // NOTE that the FilterIndex property is one-based.
                        switch (saveFileDialog1.FilterIndex)
                        {
                            default:
                                byte[] data = Encoding.UTF8.GetBytes(result);
                                fs.Write(data, 0, data.Length);
                                break;
                        }

                        fs.Close();
                    }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
        }

        public static void ImportOverrideSettings()
        {
            if (!CUOEnviroment.IsUnix)
            {
                Thread t = new Thread(() =>
                {
                    System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                    openFileDialog.Filter = "Json|*.json";
                    openFileDialog.Title = "Import tooltip override settings";
                    openFileDialog.ShowDialog();

                    // If the file name is not an empty string open it for saving.
                    if (openFileDialog.FileName != "")
                    {
                        // NOTE that the FilterIndex property is one-based.
                        switch (openFileDialog.FilterIndex)
                        {
                            default:
                                try
                                {
                                    string result = File.ReadAllText(openFileDialog.FileName);

                                    ToolTipOverrideData[] imported = JsonSerializer.Deserialize<ToolTipOverrideData[]>(result);

                                    foreach (ToolTipOverrideData importedData in imported)
                                        //GameActions.Print(importedData.searchText);
                                        new ToolTipOverrideData(ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count, importedData.SearchText, importedData.FormattedText, importedData.Min1, importedData.Max1, importedData.Min2, importedData.Max2, (byte)importedData.ItemLayer).Save();

                                }
                                catch (System.Exception e)
                                {
                                    GameActions.Print(e.Message);
                                    GameActions.Print("It looks like there was an error trying to import your override settings.", 32);
                                }
                                break;
                        }
                    }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
            else
            {
                GameActions.Print("This feature is not currently supported on Unix.", 32);
            }
        }

        private static string DecodeUnicodeEscapes(string input)
        {
            int index = 0;
            while ((index = input.IndexOf(@"\u", index)) != -1)
            {
                string hex = input.Substring(index + 2, 4);  // Extract the 4 hex digits after "\u"
                int unicodeValue = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);  // Parse the hex value
                string unicodeChar = char.ConvertFromUtf32(unicodeValue);  // Convert to character
                input = input.Remove(index, 6);  // Remove the "\u" and the 4 hex digits
                input = input.Insert(index, unicodeChar);  // Insert the decoded character
                index += unicodeChar.Length;  // Move the index forward
            }
            return input;
        }

        private static IEnumerable<ToolTipOverrideData> FilteredOverrides(
            ToolTipOverrideData[] all, byte itemLayer)
        {
            foreach (var data in all)
            {
                if (data == null)
                    continue;

                if (!CheckLayers(data.ItemLayer, itemLayer))
                    continue;

                yield return data;
            }
        }

        public static string ProcessTooltipText(uint serial, uint compareTo = uint.MinValue)
        {
            string tooltip = "";
            ItemPropertiesData itemPropertiesData;

            if (compareTo != uint.MinValue)
                itemPropertiesData = new ItemPropertiesData(World.Items.Get(serial), World.Items.Get(compareTo));
            else
                itemPropertiesData = new ItemPropertiesData(World.Items.Get(serial));

            if (!itemPropertiesData.HasData)
                return null;

            ToolTipOverrideData[] result = GetAllToolTipOverrides();

            // Event hook before processing
            EventSink.PreProcessTooltip?.Invoke(ref itemPropertiesData);

            // --------------------------------
            // Apply header override (item name)
            // --------------------------------
            bool headerHandled = false;

            foreach (var overrideData in FilteredOverrides(result, itemPropertiesData.item.ItemData.Layer))
            {
                if (MatchItemName(itemPropertiesData.Name, overrideData.SearchText))
                {
                    tooltip += string.Format(
                        overrideData.FormattedText,
                        itemPropertiesData.Name, "", "", "", "", ""
                    ) + "\n";

                    headerHandled = true;
                    break;
                }
            }

            if (!headerHandled)
            {
                tooltip += ProfileManager.CurrentProfile == null
                    ? $"/c[yellow]{itemPropertiesData.Name}\n"
                    : string.Format(ProfileManager.CurrentProfile.TooltipHeaderFormat + "\n", itemPropertiesData.Name);
            }

            // --------------------------
            // Apply property line overrides
            // --------------------------
            foreach (var property in itemPropertiesData.singlePropertyData)
            {
                bool handled = false;

                foreach (var overrideData in FilteredOverrides(result, itemPropertiesData.item.ItemData.Layer))
                {
                    // Skip name-based overrides — they should never apply to properties
                    if (!MatchPropertyName(property.OriginalString, overrideData.SearchText))
                        continue;

                    // Check value bounds
                    if ((property.FirstValue == double.MinValue || (property.FirstValue >= overrideData.Min1 && property.FirstValue <= overrideData.Max1)) &&
                        (property.SecondValue == double.MinValue || (property.SecondValue >= overrideData.Min2 && property.SecondValue <= overrideData.Max2)))
                    {
                        try
                        {
                            tooltip += (compareTo != uint.MinValue)
                                ? string.Format(
                                    overrideData.FormattedText,
                                    property.Name,
                                    property.FirstValue.ToString(),
                                    property.SecondValue.ToString(),
                                    property.OriginalString,
                                    property.FirstDiff != 0 ? $"({property.FirstDiff})" : "",
                                    property.SecondDiff != 0 ? $"({property.SecondDiff})" : ""
                                )
                                : string.Format(
                                    overrideData.FormattedText,
                                    property.Name,
                                    property.FirstValue.ToString(),
                                    property.SecondValue.ToString(),
                                    property.OriginalString, "", ""
                                );

                            tooltip += "\n";
                            handled = true;
                            break;
                        }
                        catch (FormatException e)
                        {
                            GameActions.Print($"Invalid format string in tooltip override: {overrideData.FormattedText}", 32);
                        }
                    }
                }

                if (!handled)
                {
                    tooltip += property.OriginalString + "\n";
                }
            }

            // Final hook
            EventSink.PostProcessTooltip?.Invoke(ref tooltip);

            return tooltip;
        }

        public static string ProcessTooltipText(string text)
        {
            string tooltip = "";

            ItemPropertiesData itemPropertiesData = new ItemPropertiesData(text);

            ToolTipOverrideData[] result = GetAllToolTipOverrides();

            if (itemPropertiesData.HasData && result != null && result.Length > 0)
            {
                tooltip += ProfileManager.CurrentProfile == null ? $"/c[yellow]{itemPropertiesData.Name}\n" : string.Format(ProfileManager.CurrentProfile.TooltipHeaderFormat + "\n", itemPropertiesData.Name);

                //Loop through each property
                foreach (ItemPropertiesData.SinglePropertyData property in itemPropertiesData.singlePropertyData)
                {
                    bool handled = false;
                    //Loop though each override setting player created
                    foreach (ToolTipOverrideData overrideData in result)
                    {
                        if (overrideData != null)
                            if (overrideData.ItemLayer == TooltipLayers.Any)
                            {
                                if (property.OriginalString.ToLower().Contains(overrideData.SearchText.ToLower()))
                                    if (property.FirstValue == double.MinValue || (property.FirstValue >= overrideData.Min1 && property.FirstValue <= overrideData.Max1))
                                        if (property.SecondValue == double.MinValue || (property.SecondValue >= overrideData.Min2 && property.SecondValue <= overrideData.Max2))
                                        {
                                            try
                                            {
                                                tooltip += string.Format(overrideData.FormattedText, property.Name, property.FirstValue.ToString(), property.SecondValue.ToString()) + "\n";
                                                handled = true;
                                                break;
                                            }
                                            catch { }
                                        }
                            }
                    }
                    if (!handled) //Did not find a matching override, need to add the plain tooltip line still
                        tooltip += $"{property.OriginalString}\n";

                }

                return tooltip;
            }
            return null;
        }

        private static bool CheckLayers(TooltipLayers overrideLayer, byte itemLayer)
        {
            if (overrideLayer == TooltipLayers.Any)
                return true;

            if ((byte)overrideLayer == itemLayer)
                return true;

            if (overrideLayer == TooltipLayers.Body_Group)
            {
                if (itemLayer == (byte)Layer.Shoes || itemLayer == (byte)Layer.Pants || itemLayer == (byte)Layer.Shirt || itemLayer == (byte)Layer.Helmet || itemLayer == (byte)Layer.Necklace || itemLayer == (byte)Layer.Arms || itemLayer == (byte)Layer.Gloves || itemLayer == (byte)Layer.Waist || itemLayer == (byte)Layer.Torso || itemLayer == (byte)Layer.Tunic || itemLayer == (byte)Layer.Legs || itemLayer == (byte)Layer.Skirt || itemLayer == (byte)Layer.Cloak || itemLayer == (byte)Layer.Robe)
                    return true;
            }
            else if (overrideLayer == TooltipLayers.Jewelry_Group)
            {
                if (itemLayer == (byte)Layer.Talisman || itemLayer == (byte)Layer.Bracelet || itemLayer == (byte)Layer.Ring || itemLayer == (byte)Layer.Earrings)
                    return true;
            }
            else if (overrideLayer == TooltipLayers.Weapon_Group)
            {
                if (itemLayer == (byte)Layer.OneHanded || itemLayer == (byte)Layer.TwoHanded)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the item name matches the search text
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="match">If prepended with $, regex will be applied</param>
        /// <returns></returns>
        private static bool MatchItemName(string itemName, string match)
        {
            if (string.IsNullOrEmpty(match))
                return false;

            if (match.StartsWith("$") && match.Length > 1)
            {
                try
                {
                    return Regex.IsMatch(itemName, match.Substring(1));
                }
                catch
                {
                    GameActions.Print($"Invalid regex pattern: {match.Substring(1)}");
                    return false;
                }
            }

            return itemName.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Check if the property name matches the search text
        /// </summary>
        /// <param name="property"></param>
        /// <param name="match">If prepended with $, regex will be applied</param>
        /// <returns></returns>
        private static bool MatchPropertyName(string property, string match)
        {
            if (string.IsNullOrEmpty(match))
                return false;

            if (match.StartsWith("$") && match.Length > 1)
            {
                try
                {
                    return Regex.IsMatch(property, match.Substring(1));
                }
                catch
                {
                    GameActions.Print($"Invalid regex pattern: {match.Substring(1)}");
                    return false;
                }
            }

            return property.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
