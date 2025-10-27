using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    public class GridHighLightProfile
    {
        public static void MigrateGridHighlightToSetup(Profile profile)
        {
            profile.GridHighlightSetup.Clear();
            int count = profile.GridHighlight_Name.Count;

            for (int i = 0; i < count; i++)
            {
                var entry = new GridHighlightSetupEntry
                {
                    Name = profile.GridHighlight_Name[i],
                    Hue = profile.GridHighlight_Hue.ElementAtOrDefault(i),
                    AcceptExtraProperties = i < profile.GridHighlight_AcceptExtraProperties.Count
                        ? profile.GridHighlight_AcceptExtraProperties[i]
                        : true,
                    ExcludeNegatives = profile.GridHighlight_ExcludeNegatives.ElementAtOrDefault(i) ?? new(),
                    RequiredRarities = profile.GridHighlight_RequiredRarities.ElementAtOrDefault(i) ?? new(),
                    Properties = new List<GridHighlightProperty>()
                };

                List<string> names = profile.GridHighlight_PropNames.ElementAtOrDefault(i) ?? new();
                List<int> mins = profile.GridHighlight_PropMinVal.ElementAtOrDefault(i) ?? new();
                List<bool> opts = profile.GridHighlight_IsOptionalProperties.ElementAtOrDefault(i);

                for (int j = 0; j < names.Count; j++)
                {
                    entry.Properties.Add(new GridHighlightProperty
                    {
                        Name = names[j],
                        MinValue = j < mins.Count ? mins[j] : -1,
                        IsOptional = opts != null && j < opts.Count ? opts[j] : false
                    });
                }

                profile.GridHighlightSetup.Add(entry);
            }

            // Clear legacy lists
            profile.GridHighlight_Name.Clear();
            profile.GridHighlight_Hue.Clear();
            profile.GridHighlight_PropNames.Clear();
            profile.GridHighlight_PropMinVal.Clear();
            profile.GridHighlight_AcceptExtraProperties.Clear();
            profile.GridHighlight_IsOptionalProperties.Clear();
            profile.GridHighlight_ExcludeNegatives.Clear();
            profile.GridHighlight_RequiredRarities.Clear();
        }
    }
}