// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.Network;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers
{
    public sealed class ObjectPropertiesListManager
    {
        private readonly Dictionary<uint, ItemProperty> _itemsProperties = new Dictionary<uint, ItemProperty>();
        private World _world;

        public ObjectPropertiesListManager(World world)
        {
            _world = world;
        }

        public void Add(uint serial, uint revision, string name, string data, int namecliloc)
        {
            if (!_itemsProperties.TryGetValue(serial, out ItemProperty prop))
            {
                prop = new ItemProperty();
                _itemsProperties[serial] = prop;
            }

            prop.Serial = serial;
            prop.Revision = revision;
            prop.Name = name;
            prop.Data = data;
            prop.NameCliloc = namecliloc;

            EventSink.InvokeOPLOnReceive(null, new OPLEventArgs(serial, name, data));

            Item item = _world.Items.Get(serial);
            if(item != null)
                ItemDatabaseManager.Instance.AddOrUpdateItem(item, _world);

            GridHighlightData.ProcessItemOpl(_world, serial);
        }

        public bool Contains(uint serial)
        {
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ForceTooltipsOnOldClients)
                ForcedTooltipManager.RequestName(_world, serial);

            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
                return true; //p.Revision != 0;  <-- revision == 0 can contain the name.

            // if we don't have the OPL of this item, let's request it to the server.
            // Original client seems asking for OPL when character is not running.
            // We'll ask OPL when mouse is over an object.
            PacketHandlers.AddMegaClilocRequest(serial);

            return false;
        }

        public bool IsRevisionEquals(uint serial, uint revision)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty prop))
            {
                return (revision & ~0x40000000) == prop.Revision || // remove the mask
                       revision == prop.Revision;                   // if mask removing didn't work, try a simple compare.
            }

            return false;
        }

        public bool TryGetRevision(uint serial, out uint revision)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
            {
                revision = p.Revision;

                return true;
            }

            revision = 0;

            return false;
        }

        public bool TryGetNameAndData(uint serial, out string name, out string data)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
            {
                name = p.Name;
                data = p.Data;

                return true;
            }

            name = data = null;

            return false;
        }

        public int GetNameCliloc(uint serial)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
            {
                return p.NameCliloc;
            }

            return 0;
        }

        public ItemPropertiesData TryGetItemPropertiesData(World world, uint serial)
        {
            if (Contains(serial))
                if (world.Items.TryGetValue(serial, out Item item))
                    return new ItemPropertiesData(world, item);
            return null;
        }

        public void Remove(uint serial) => _itemsProperties.Remove(serial);

        public void Clear() => _itemsProperties.Clear();
    }

    public class ItemProperty
    {
        public bool IsEmpty => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Data);
        public string Data;
        public string Name;
        public uint Revision;
        public uint Serial;
        public int NameCliloc;

        public string CreateData(bool extended) => string.Empty;
    }

    public class ItemPropertiesData
    {
        public readonly bool HasData = false;
        public string Name = "";
        public readonly string RawData = "";
        public readonly uint serial;
        public string[] RawLines;
        public readonly Item item, itemComparedTo;
        public List<SinglePropertyData> singlePropertyData = new List<SinglePropertyData>();

        private World world;

        public ItemPropertiesData(World world, Item item, Item compareTo = null)
        {
            if (item == null)
                return;
            this.world = world;
            this.item = item;
            itemComparedTo = compareTo;

            serial = item.Serial;
            if (world.OPL.TryGetNameAndData(item.Serial, out Name, out RawData))
            {
                Name = Name.Trim();
                HasData = true;
                processData();
            }
        }

        public ItemPropertiesData(string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip))
                return;
            if (tooltip.Contains("\n"))
            {
                Name = tooltip.Substring(0, tooltip.IndexOf("\n"));
                RawData = tooltip.Substring(tooltip.IndexOf("\n") + 1);
            }
            else
            {
                Name = tooltip;
            }
            HasData = true;
            processData();
        }

        private void processData()
        {
            string formattedData = TextBox.ConvertHtmlToFontStashSharpCommand(RawData);

            RawLines = formattedData.Split(new string[] { "\n", "<br>" }, StringSplitOptions.None);

            foreach (string line in RawLines)
            {
                singlePropertyData.Add(new SinglePropertyData(line));
            }

            if (itemComparedTo != null)
            {
                GenComparisonData();
            }
        }

        private void GenComparisonData()
        {
            if (itemComparedTo == null) return;

            var itemPropertiesData = new ItemPropertiesData(world, itemComparedTo);
            if (itemPropertiesData.HasData)
            {
                foreach (SinglePropertyData thisItem in singlePropertyData)
                {
                    foreach (SinglePropertyData secondItem in itemPropertiesData.singlePropertyData)
                    {
                        if (String.Equals(thisItem.Name, secondItem.Name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (thisItem.FirstValue != double.MinValue && secondItem.FirstValue != double.MinValue)
                            {
                                thisItem.FirstDiff = thisItem.FirstValue - secondItem.FirstValue;
                            }

                            if (thisItem.SecondValue > double.MinValue && secondItem.SecondValue > double.MinValue)
                            {
                                thisItem.SecondDiff = thisItem.SecondValue - secondItem.SecondValue;
                            }
                            break;
                        }
                    }
                }
            }
        }

        public bool GenerateComparisonTooltip(ItemPropertiesData comparedTo, out string compiledToolTip)
        {
            if (!HasData)
            {
                compiledToolTip = null;
                return false;
            }

            string finalTooltip = Name + "\n";

            foreach (SinglePropertyData thisItem in singlePropertyData)
            {
                bool foundMatch = false;
                foreach (SinglePropertyData secondItem in comparedTo.singlePropertyData)
                {
                    if (string.Equals(thisItem.Name, secondItem.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        foundMatch = true;
                        finalTooltip += thisItem.Name;

                        if (thisItem.FirstValue != double.MinValue && secondItem.FirstValue != double.MinValue)
                        {
                            double diff = thisItem.FirstValue - secondItem.FirstValue;
                            finalTooltip += $" {thisItem.FirstValue}";
                            if (diff != 0)
                            {
                                finalTooltip += $"({(diff >= 0 ? "/c[green]+" : "/c[red]")} {diff}/cd)";
                            }
                        }

                        if (thisItem.SecondValue > double.MinValue && secondItem.SecondValue > double.MinValue)
                        {
                            double diff = thisItem.SecondValue - secondItem.SecondValue;
                            finalTooltip += $" {thisItem.SecondValue}";
                            if (diff != 0)
                            {
                                finalTooltip += $"({(diff >= 0 ? "/c[green]+" : "/c[red]")}{diff}/cd)";
                            }
                        }

                        finalTooltip += "\n";
                        break;
                    }
                }
                if (!foundMatch)
                    finalTooltip += thisItem.ToString() + "\n";
            }

            compiledToolTip = finalTooltip;
            return true;
        }

        public string CompileTooltip()
        {
            string result = "";

            result += Name + "\n";
            foreach (SinglePropertyData data in singlePropertyData)
                result += $"{data.Name} [{data.FirstValue}] [{data.SecondValue}]\n";

            return result;
        }

        public class SinglePropertyData
        {
            public string OriginalString;
            public string Name = "";
            public double FirstValue = double.MinValue;
            public double SecondValue = double.MinValue;
            public double FirstDiff = 0;
            public double SecondDiff = 0;

            public SinglePropertyData(string line)
            {
                OriginalString = line;

                // Remove any color tags like /c[#...]
                string cleaned = RegexHelper.GetRegex(@"/c\[[#a-zA-Z0-9]+\]", RegexOptions.IgnoreCase).Replace(line, "").Replace("/cd", "").Trim();

                // Extract numbers
                MatchCollection matches = RegexHelper.GetRegex(@"-?\d+(\.\d+)?").Matches(cleaned);

                if (matches.Count > 0)
                {
                    double.TryParse(matches[0].Value, out FirstValue);
                    if (matches.Count > 1)
                        double.TryParse(matches[1].Value, out SecondValue);
                }

                // Remove all numbers and symbols from the cleaned string to isolate the name
                Name = RegexHelper.GetRegex(@"[-+]?\d+(\.\d+)?[%]?([- ]*\d+)?", RegexOptions.IgnoreCase).Replace(cleaned, "").Trim();

                // Fallback if something went wrong
                if (string.IsNullOrWhiteSpace(Name))
                    Name = line;
            }

            public override string ToString()
            {
                string output = "";

                if (Name != null)
                    output += Name;

                if (FirstValue != double.MinValue)
                    output += $" {FirstValue}";

                if (SecondValue != double.MinValue)
                    output += $" {SecondValue}";

                return output;
            }
        }
    }
}
