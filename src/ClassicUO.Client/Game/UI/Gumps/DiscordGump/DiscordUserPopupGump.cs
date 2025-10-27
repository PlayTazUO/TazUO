using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Discord.Sdk;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps;

public class DiscordUserPopupGump : Gump
{
    private ulong userId;
    private UserHandle userHandle;

    public DiscordUserPopupGump(World world, ulong user, int x, int y) : base(world, 0, 0)
    {
        Width = 200;
        Height = 300;
        CanMove = true;
        IsModal = true;

        ModalClickOutsideAreaClosesThisControl = true;
        
        X = x;
        Y = y;
        userId = user;

        userHandle = DiscordManager.Instance.GetUser(userId);

        Build();
    }

    private void Build()
    {
        if (userHandle == null)
        {
            Dispose();

            return;
        }

        var abc = new AlphaBlendControl(0.7f)
        {
            Width = Width,
            Height = Height
        };

        abc.BaseColor = new Color(21, 21, 21);
        Add(abc);

        int y = 0;

        var avatar = new ExternalUrlImage(userHandle.AvatarUrl(UserHandle.AvatarType.Png, UserHandle.AvatarType.Png));
        avatar.Y = y;
        avatar.X = (Width - avatar.Width) / 2;
        y += avatar.Height + 5;
        Add(avatar);

        var name = TextBox.GetOne(userHandle.DisplayName(), TrueTypeLoader.EMBEDDED_FONT, 20f, DiscordManager.GetUserhue(userId), TextBox.RTLOptions.DefaultCentered(Width));
        name.Y = y;
        Add(name);
        y += name.Height + 5;

        Activity presence = userHandle.GameActivity();

        if (presence != null)
        {
            var c = TextBox.GetOne(presence.Name(), TrueTypeLoader.EMBEDDED_FONT, 20f, Color.LightSlateGray, TextBox.RTLOptions.DefaultCentered(Width));
            c.Y = y;
            y += c.Height + 5;
            Add(c);

            ActivityParty activitys = presence.Party();

            if (activitys != null)
            {
                var data = ServerInfo.FromJson(activitys.Id());

                c = TextBox.GetOne(data.Name, TrueTypeLoader.EMBEDDED_FONT, 20f, Color.LightSlateGray, TextBox.RTLOptions.DefaultCentered(Width));
                c.Y = y;
                y += c.Height + 5;
                Add(c);
            }
        }
    }

    public override bool AcceptMouseInput => true;
}