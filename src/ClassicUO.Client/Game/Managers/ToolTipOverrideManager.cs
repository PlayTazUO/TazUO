using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ClassicUO.Utility.Logging;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.Utility;

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
        public ToolTipOverrideData(int index, string searchText, string formattedText, int min1, int max1, int min2, int max2, byte layer, int borderHue = -1)
        {
            Index = index;
            SearchText = DecodeUnicodeEscapes(searchText).Trim();
            FormattedText = DecodeUnicodeEscapes(formattedText).Trim();
            Min1 = min1;
            Max1 = max1;
            Min2 = min2;
            Max2 = max2;
            ItemLayer = (TooltipLayers)layer;
            BorderHue = borderHue;
        }

        /// <summary>
        /// Position of this override within its scope's list (<see cref="TooltipOverridesConfig.GetScope"/>).
        /// This is a runtime index derived from list position, not part of the persisted file.
        /// </summary>
        [JsonIgnore]
        public int Index { get; set; }

        public string SearchText { get; set; }
        public string FormattedText { get; set; }
        public int Min1 { get; set; }
        public int Max1 { get; set; }
        public int Min2 { get; set; }
        public int Max2 { get; set; }
        public TooltipLayers ItemLayer { get; set; }

        /// <summary>
        /// Optional UO hue used to draw a custom-colored border around the tooltip when this override
        /// matches. A value of -1 means "no override" and the default tooltip border is drawn instead.
        /// </summary>
        public int BorderHue { get; set; } = -1;

        /// <summary>Whether this override specifies a custom tooltip border hue.</summary>
        [JsonIgnore]
        public bool HasBorderHue => BorderHue >= 0;

        /// <summary>Pixel width of the custom border drawn when <see cref="HasBorderHue"/> is set.</summary>
        public const int BorderWidth = 5;

        [JsonIgnore]
        public bool IsNew { get; set; } = false;

        /// <summary>
        /// Returns the override at <paramref name="index"/> in the given <paramref name="scope"/>'s list.
        /// An out-of-range index creates a new default override, persists it into that scope and returns it.
        /// </summary>
        public static ToolTipOverrideData Get(int index, SettingsScope scope = SettingsScope.Char)
        {
            if (ProfileManager.CurrentProfile == null)
                return null;

            List<ToolTipOverrideData> overrides = TooltipOverridesConfig.Current.GetScope(scope).Overrides;

            if (index >= 0 && index < overrides.Count)
                return overrides[index];

            // Requesting an out-of-range index creates a new default override, persists it and returns it.
            var data = new ToolTipOverrideData(index, "Weapon Damage", "DMG /c[orange]{1} /cd- /c[red]{2}", -1, 99, -1, 99, (byte)TooltipLayers.Any)
            {
                IsNew = true
            };
            data.Save(scope);
            return data;
        }

        public void Save(SettingsScope scope = SettingsScope.Char)
        {
            if (ProfileManager.CurrentProfile == null)
                return;

            TooltipOverridesConfig.Current.GetScope(scope).Upsert(this);
        }

        public void Delete(SettingsScope scope = SettingsScope.Char)
        {
            if (Index < 0 || ProfileManager.CurrentProfile == null)
                return;

            TooltipOverridesConfig.Current.GetScope(scope).RemoveAt(Index);
        }

        /// <summary>All overrides from every scope, most-specific first, used for tooltip processing.</summary>
        public static ToolTipOverrideData[] GetAllToolTipOverrides()
        {
            return TooltipOverridesConfig.Current.GetAllOverrides();
        }

        /// <summary>Copies the given scope's tooltip overrides to the clipboard as JSON.</summary>
        public static void ExportOverrideSettings(World world, SettingsScope scope)
        {
            try
            {
                ToolTipOverrideData[] allData = TooltipOverridesConfig.Current.GetScope(scope).Overrides.ToArray();
                string json = JsonSerializer.Serialize(allData, ToolTipOverrideContext.Default.ToolTipOverrideDataArray);
                Clipboard.SetClipboardText(json);
                GameActions.Print(world, TazLang.Get("tooltipconfig_export_clipboard_success", "Copied tooltip overrides to clipboard"));
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                GameActions.Print(world, TazLang.Get("tooltipconfig_export_error", "Failed to export tooltip overrides"), Constants.HUE_ERROR);
            }
        }

        /// <summary>Imports tooltip overrides from the clipboard JSON, appending them to the given scope.</summary>
        public static void ImportOverrideSettings(SettingsScope scope)
        {
            try
            {
                string json = Clipboard.GetClipboardText();

                if (string.IsNullOrWhiteSpace(json))
                {
                    GameActions.Print(World.Instance, TazLang.Get("tooltipconfig_import_clipboard_empty", "Clipboard is empty, nothing to import"), Constants.HUE_ERROR);
                    return;
                }

                ToolTipOverrideData[] imported = JsonSerializer.Deserialize(json, ToolTipOverrideContext.Default.ToolTipOverrideDataArray);

                if (imported == null)
                {
                    GameActions.Print(World.Instance, TazLang.Get("tooltipconfig_import_error", "Failed to import tooltip overrides"), Constants.HUE_ERROR);
                    return;
                }

                List<ToolTipOverrideData> overrides = TooltipOverridesConfig.Current.GetScope(scope).Overrides;

                foreach (ToolTipOverrideData importedData in imported)
                    new ToolTipOverrideData(overrides.Count, importedData.SearchText, importedData.FormattedText, importedData.Min1, importedData.Max1, importedData.Min2, importedData.Max2, (byte)importedData.ItemLayer, importedData.BorderHue).Save(scope);

                GameActions.Print(World.Instance, string.Format(TazLang.Get("tooltipconfig_import_clipboard_success", "Imported {0} tooltip overrides!"), imported.Length));
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                GameActions.Print(World.Instance, TazLang.Get("tooltipconfig_import_error", "Failed to import tooltip overrides"), Constants.HUE_ERROR);
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
            if (all == null)
                yield break;

            foreach (ToolTipOverrideData data in all)
            {
                if (data == null)
                    continue;

                if (!CheckLayers(data.ItemLayer, itemLayer))
                    continue;

                yield return data;
            }
        }

        private static string BuildTooltip(ItemPropertiesData itemPropertiesData, out int borderHue, uint compareTo = uint.MinValue)
        {
            borderHue = -1;

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

                    // The first matching rule that sets a border hue wins for the whole tooltip.
                    if (borderHue < 0 && overrideData.HasBorderHue)
                        borderHue = overrideData.BorderHue;

                    headerHandled = true;
                    break;
                }
            }

            if (!headerHandled)
            {
                if(string.IsNullOrEmpty(itemPropertiesData.Name))
                    itemPropertiesData.Name = "";

                sb.AppendLine(
                    ProfileManager.CurrentProfile == null
                        ? $"/c[yellow]{itemPropertiesData.Name}"
                        : string.Format(ProfileManager.CurrentProfile.TooltipHeaderFormat, itemPropertiesData.Name)
                );
            }

            GridHighlightData bestGridHighlightData = ProfileManager.CurrentProfile is { GridHighlightProperties: true } ? GridHighlightData.GetBestMatch(itemPropertiesData) : null;

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

                        if ((!property.FirstValue.HasValue || (property.FirstValue.Value >= overrideData.Min1 && property.FirstValue.Value <= overrideData.Max1)) &&
                            (!property.SecondValue.HasValue || (property.SecondValue.Value >= overrideData.Min2 && property.SecondValue.Value <= overrideData.Max2)))
                        {
                            matchedOverride = overrideData;

                            // The first matching rule that sets a border hue wins for the whole tooltip.
                            if (borderHue < 0 && overrideData.HasBorderHue)
                                borderHue = overrideData.BorderHue;

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
                        GameActions.Print(World.Instance, $"Invalid format string in tooltip override: {matchedOverride.FormattedText}", Constants.HUE_ERROR);
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

            if (ProfileManager.CurrentProfile is { GridHighlightShowRuleName: true } && bestGridHighlightData != null && !string.IsNullOrEmpty(bestGridHighlightData.Name))
            {
                sb.AppendLine($"/c[gray]Matched Rule: {bestGridHighlightData.Name}/cd");
            }

            return sb.ToString();
        }

        public static string ProcessTooltipText(World world, uint serial, uint compareTo = uint.MinValue)
            => ProcessTooltipText(world, serial, out _, compareTo);

        /// <summary>
        /// As <see cref="ProcessTooltipText(World, uint, uint)"/>, additionally reporting the
        /// border hue requested by the matched override (-1 when none applies).
        /// </summary>
        public static string ProcessTooltipText(World world, uint serial, out int borderHue, uint compareTo = uint.MinValue)
        {
            ItemPropertiesData itemPropertiesData =
                compareTo != uint.MinValue
                ? new ItemPropertiesData(world, world.Items.Get(serial), world.Items.Get(compareTo))
                : new ItemPropertiesData(world, world.Items.Get(serial));

            return BuildTooltip(itemPropertiesData, out borderHue, compareTo);
        }

        public static string ProcessTooltipText(string text)
            => ProcessTooltipText(text, out _);

        /// <summary>
        /// As <see cref="ProcessTooltipText(string)"/>, additionally reporting the border hue
        /// requested by the matched override (-1 when none applies).
        /// </summary>
        public static string ProcessTooltipText(string text, out int borderHue)
        {
            var itemPropertiesData = new ItemPropertiesData(text);
            return BuildTooltip(itemPropertiesData, out borderHue);
        }

        /// <summary>
        /// Resolve the final tooltip text for a hovered object, applying any configured overrides.
        /// Items that exist in the world are resolved by serial. Items shown in server-sent gumps
        /// (and vendor search results) are referenced by serial but aren't real world items, so the
        /// serial lookup returns null - in that case the override is applied to the raw OPL text
        /// (<paramref name="rawHtml"/>) instead. Falls back to the raw text only when no override
        /// produced any output.
        /// </summary>
        public static string ResolveTooltipText(World world, uint serial, string rawHtml)
            => ResolveTooltipText(world, serial, rawHtml, out _);

        /// <summary>
        /// As <see cref="ResolveTooltipText(World, uint, string)"/>, additionally reporting the
        /// border hue requested by the matched override (-1 when none applies) so the renderer
        /// can draw a custom-colored tooltip border.
        /// </summary>
        public static string ResolveTooltipText(World world, uint serial, string rawHtml, out int borderHue)
        {
            borderHue = -1;
            string finalString = null;

            // Optionally skip overrides entirely for mobiles, returning their raw tooltip text.
            if (SerialHelper.IsMobile(serial) && ProfileManager.CurrentProfile is { ToolTipOverride_IgnoreMobiles: true })
                return rawHtml;

            if (SerialHelper.IsItem(serial))
                finalString = ProcessTooltipText(world, serial, out borderHue);

            //Fix for vendor search and items shown in server gumps that aren't real world items.
            if (string.IsNullOrEmpty(finalString) && !string.IsNullOrEmpty(rawHtml))
                finalString = ProcessTooltipText(rawHtml, out borderHue);

            if (string.IsNullOrEmpty(finalString))
                finalString = rawHtml;

            return finalString;
        }

        private static bool CheckLayers(TooltipLayers overrideLayer, byte itemLayer)
        {
            if (overrideLayer == TooltipLayers.Any)
                return true;

            if ((byte)overrideLayer == itemLayer)
                return true;

            if (overrideLayer == TooltipLayers.Body_Group)
            {
                if (itemLayer == (byte)Layer.Shoes || itemLayer == (byte)Layer.Pants || itemLayer == (byte)Layer.Shirt || itemLayer == (byte)Layer.Helmet || itemLayer == (byte)Layer.Neck || itemLayer == (byte)Layer.Arms || itemLayer == (byte)Layer.Gloves || itemLayer == (byte)Layer.Waist || itemLayer == (byte)Layer.Torso || itemLayer == (byte)Layer.Tunic || itemLayer == (byte)Layer.Legs || itemLayer == (byte)Layer.Skirt || itemLayer == (byte)Layer.Cloak || itemLayer == (byte)Layer.Robe)
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
                    return RegexHelper.GetRegex(match.Substring(1)).IsMatch(property);
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
