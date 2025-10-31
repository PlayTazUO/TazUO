using System;
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using Discord.Sdk;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls;

public class DiscordMessageControl : Control
{
    public DiscordMessageControl(MessageHandle msg, int width)
    {
        Width = width;
        CanMove = true;


        DateTime time = DateTimeOffset.FromUnixTimeMilliseconds((long)msg.SentTimestamp()).UtcDateTime.ToLocalTime();

        string content = msg.Content();

        if (string.IsNullOrEmpty(content) && (msg.Metadata() == null || msg.Metadata().Count == 0) )
        {
            AdditionalContent adtl = msg.AdditionalContent();

            if (adtl == null)
            {
                Dispose();
                return;
            }

            if (adtl.Type() == AdditionalContentType.Attachment || adtl.Type() == AdditionalContentType.Embed)
            {
                content = "[- User sent an attachment, unable to view here. -]";
            }
        }

        var name = TextBox.GetOne($"[{time.ToShortTimeString()}] {msg.Author()?.DisplayName()}: ", TrueTypeLoader.EMBEDDED_FONT, 20f, DiscordManager.GetUserhue(msg.AuthorId()), TextBox.RTLOptions.Default());
        var message = TextBox.GetOne(content, TrueTypeLoader.EMBEDDED_FONT, 20f, Color.White, TextBox.RTLOptions.Default(width - name.Width));
        message.X = name.Width;

        int h = Math.Max(name.Height, message.Height);

        Add(name);
        Add(message);

        System.Collections.Generic.Dictionary<string, string> meta = msg.Metadata();
        if (meta != null && meta.Count > 0)
        {
            if (meta.TryGetValue("graphic", out string graphic) && ushort.TryParse(graphic, out ushort g))
            {
                if(meta.TryGetValue("hue", out string hue) && ushort.TryParse(hue, out ushort hue2))
                {
                    var item = new StaticPic(g, hue2);
                    item.X = name.Width;
                    item.Y = h;

                    var hitbox = new HitBox(item.X, item.Y, Math.Max(item.Width, 50), Math.Max(item.Height, 50));

                    string ttip = meta["name"];
                    if(meta.TryGetValue("data", out string data))
                        ttip += "\n" + data;
                    ttip = ToolTipOverrideData.ProcessTooltipText(ttip);

                    hitbox.SetTooltip(ttip);

                    Add(item);
                    Add(hitbox);
                    h = Math.Max(h, item.Height);
                }
            }
        }

        Height = h;
    }
}
