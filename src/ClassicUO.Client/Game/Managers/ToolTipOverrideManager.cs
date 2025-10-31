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
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using ClassicUO.Game.UI.Gumps.GridHighLight;

namespace ClassicUO.Game.Managers
{
    [JsonSerializable(typeof(ToolTipOverrideData))]
    [JsonSerializable(typeof(ToolTipOverrideData[]))]
    public partial class ToolTipOverrideContext : JsonSerializerContext
    {
    }

    public class ToolTipOverrideData
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

                var data = new ToolTipOverrideData(index, searchText, formattedText, min1, max1, min2, max2, layer);

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
            if (Index < 0) return;

            Profile profile = ProfileManager.CurrentProfile;

            if (Index < profile.ToolTipOverride_SearchText.Count)
                profile.ToolTipOverride_SearchText.RemoveAt(Index);

            if (Index < profile.ToolTipOverride_NewFormat.Count)
                profile.ToolTipOverride_NewFormat.RemoveAt(Index);

            if (Index < profile.ToolTipOverride_MinVal1.Count)
                profile.ToolTipOverride_MinVal1.RemoveAt(Index);

            if (Index < profile.ToolTipOverride_MinVal2.Count)
                profile.ToolTipOverride_MinVal2.RemoveAt(Index);

            if (Index < profile.ToolTipOverride_MaxVal1.Count)
                profile.ToolTipOverride_MaxVal1.RemoveAt(Index);

            if (Index < profile.ToolTipOverride_MaxVal2.Count)
                profile.ToolTipOverride_MaxVal2.RemoveAt(Index);

            if (Index < profile.ToolTipOverride_Layer.Count)
                profile.ToolTipOverride_Layer.RemoveAt(Index);
        }

        public static ToolTipOverrideData[] GetAllToolTipOverrides()
        {
            if (ProfileManager.CurrentProfile == null)
                return null;

            var result = new ToolTipOverrideData[ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count];

            for (int i = 0; i < ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count; i++)
            {
                result[i] = Get(i);
            }

            return result;
        }

        public static void ExportOverrideSettings(World world)
        {
            ToolTipOverrideData[] allData = GetAllToolTipOverrides();

            UIManager.Add(new FileSelector(World.Instance, FileSelectorType.Directory, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), ["*.json"], (p) =>
            {
                if (!Directory.Exists(p))
                {
                    GameActions.Print(World.Instance, "Directory doesn't exist!", 32);
                    return;
                }

                try
                {
                    string result = JsonSerializer.Serialize(allData);
                    string path = Path.Combine(p, "tooltip_overrides.json");
                    File.WriteAllText(path, result);
                    GameActions.Print(World.Instance, $"The override file has been saved to [{path}]");
                }
                catch (Exception e)
                {
                    GameActions.Print(World.Instance, "Failed to save the override file!", 32);
                    Log.Error(e.ToString());
                }
            }));
        }

        public static void ImportOverrideSettings() => UIManager.Add(new FileSelector(World.Instance, FileSelectorType.File, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), ["*.json"], (p) =>
                                                                {
                                                                    if (!File.Exists(p))
                                                                    {
                                                                        GameActions.Print(World.Instance, "File doesn't exist!", 32);
                                                                        return;
                                                                    }

                                                                    try
                                                                    {
                                                                        string result = File.ReadAllText(p);

                                                                        ToolTipOverrideData[] imported = JsonSerializer.Deserialize<ToolTipOverrideData[]>(result);

                                                                        foreach (ToolTipOverrideData importedData in imported)
                                                                            new ToolTipOverrideData(ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count, importedData.SearchText, importedData.FormattedText, importedData.Min1, importedData.Max1, importedData.Min2, importedData.Max2, (byte)importedData.ItemLayer).Save();

                                                                        GameActions.Print(World.Instance, $"Imported {imported.Length} tooltip overrides!");
                                                                    }
                                                                    catch (System.Exception e)
                                                                    {
                                                                        Log.Error(e.ToString());
                                                                        GameActions.Print(World.Instance, "It looks like there was an error trying to import your override settings.", 32);
                                                                    }
                                                                }));

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
            foreach (ToolTipOverrideData data in all)
            {
                if (data == null)
                    continue;

                if (!CheckLayers(data.ItemLayer, itemLayer))
                    continue;

                yield return data;
            }
        }

        private static string BuildTooltip(ItemPropertiesData itemPropertiesData, uint compareTo = uint.MinValue)
        {
            if (!itemPropertiesData.HasData)
                return null;

            var sb = new StringBuilder();
            ToolTipOverrideData[] toolTipOverrides = GetAllToolTipOverrides();

            bool headerHandled = false;
            foreach (ToolTipOverrideData overrideData in FilteredOverrides(toolTipOverrides, itemPropertiesData.item?.ItemData.Layer ?? 0))
            {
                if (MatchItemName(itemPropertiesData.Name, overrideData.SearchText))
                {
                    sb.AppendLine(string.Format(
                        overrideData.FormattedText,
                        itemPropertiesData.Name, "", "", "", "", ""
                    ));
                    headerHandled = true;
                    break;
                }
            }

            if (!headerHandled)
            {
                sb.AppendLine(
                    ProfileManager.CurrentProfile == null
                        ? $"/c[yellow]{itemPropertiesData.Name}"
                        : string.Format(ProfileManager.CurrentProfile.TooltipHeaderFormat, itemPropertiesData.Name)
                );
            }

            GridHighlightData bestGridHighlightData = ProfileManager.CurrentProfile.GridHighlightProperties ? GridHighlightData.GetBestMatch(itemPropertiesData) : null;

            foreach (ItemPropertiesData.SinglePropertyData property in itemPropertiesData.singlePropertyData)
            {
                // Find if this property is highlighted
                bool isHighlighted = bestGridHighlightData != null && bestGridHighlightData.DoesPropertyMatch(property);

                // Try to find an override
                ToolTipOverrideData matchedOverride = null;
                if (toolTipOverrides != null)
                {
                    foreach (ToolTipOverrideData overrideData in FilteredOverrides(toolTipOverrides, itemPropertiesData.item?.ItemData.Layer ?? 0))
                    {
                        if (!MatchPropertyName(World.Instance, property.OriginalString, overrideData.SearchText))
                            continue;

                        if ((property.FirstValue == double.MinValue || (property.FirstValue >= overrideData.Min1 && property.FirstValue <= overrideData.Max1)) &&
                            (property.SecondValue == double.MinValue || (property.SecondValue >= overrideData.Min2 && property.SecondValue <= overrideData.Max2)))
                        {
                            matchedOverride = overrideData;
                            break;
                        }
                    }
                }

                string finalLine;

                // 1. If override exists, format it
                if (matchedOverride != null)
                {
                    try
                    {
                        if (compareTo != uint.MinValue)
                        {
                            finalLine = string.Format(
                                matchedOverride.FormattedText,
                                property.Name,
                                property.FirstValue.ToString(),
                                property.SecondValue.ToString(),
                                property.OriginalString,
                                property.FirstDiff != 0 ? $"({property.FirstDiff})" : "",
                                property.SecondDiff != 0 ? $"({property.SecondDiff})" : ""
                            );
                        }
                        else
                        {
                            finalLine = string.Format(
                                matchedOverride.FormattedText,
                                property.Name,
                                property.FirstValue.ToString(),
                                property.SecondValue.ToString(),
                                property.OriginalString, "", ""
                            );
                        }
                    }
                    catch
                    {
                        GameActions.Print(World.Instance, $"Invalid format string in tooltip override: {matchedOverride.FormattedText}", 32);
                        finalLine = property.OriginalString;
                    }
                }
                else
                {
                    // 2. No override → fallback to original text
                    finalLine = property.OriginalString;
                }

                if (isHighlighted)
                {
                    finalLine = $"[o] {finalLine}/cd";
                }

                sb.AppendLine(finalLine);
            }

            if (ProfileManager.CurrentProfile.GridHighlightShowRuleName && bestGridHighlightData != null && !string.IsNullOrEmpty(bestGridHighlightData.Name))
            {
                sb.AppendLine($"/c[gray]Matched Rule: {bestGridHighlightData.Name}/cd");
            }

            return sb.ToString();
        }

        public static string ProcessTooltipText(World world, uint serial, uint compareTo = uint.MinValue)
        {
            ItemPropertiesData itemPropertiesData =
                compareTo != uint.MinValue
                ? new ItemPropertiesData(world, world.Items.Get(serial), world.Items.Get(compareTo))
                : new ItemPropertiesData(world, world.Items.Get(serial));

            return BuildTooltip(itemPropertiesData, compareTo);
        }

        public static string ProcessTooltipText(string text)
        {
            var itemPropertiesData = new ItemPropertiesData(text);
            return BuildTooltip(itemPropertiesData);
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
                    GameActions.Print(World.Instance, $"Invalid regex pattern: {match.Substring(1)}");
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
        private static bool MatchPropertyName(World world, string property, string match)
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
                    GameActions.Print(world, $"Invalid regex pattern: {match[1..]}");
                    return false;
                }
            }

            return property.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
