using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Discord.Sdk;

namespace ClassicUO.Game.UI.Controls;

public class DiscordChannelListItem : Control
{
    private readonly DiscordGump gump;
    private LobbyHandle lobby;
    private ChannelHandle dMessage;
    private ulong ID;
    private AlphaBlendControl selectedBackground;

    public override bool AcceptMouseInput => true;

    public DiscordChannelListItem(DiscordGump gump, LobbyHandle lobby, int width = 100, int height = 25)
    {
        Width = width;
        Height = height;
        CanMove = true;
        CanCloseWithRightClick = true;
        this.gump = gump;
        this.lobby = lobby;
        ID = lobby.Id();

        Build();
    }

    public DiscordChannelListItem(DiscordGump gump, ChannelHandle dMessage, int width = 100, int height = 25)
    {
        Width = width;
        Height = height;
        CanMove = true;
        CanCloseWithRightClick = true;
        this.gump = gump;
        this.dMessage = dMessage;
        ID = dMessage.Id();

        Build();
    }

    public void SetSelected()
    {
        if (gump.ActiveChannel == ID)
            selectedBackground.IsVisible = true;
        else
            selectedBackground.IsVisible = false;
    }

    protected override void OnMouseUp(int x, int y, MouseButtonType button)
    {
        base.OnMouseUp(x, y, button);

        if (button == MouseButtonType.Left)
        {
            gump?.SetActiveChatChannel(ID, dMessage != null);
        }
    }

    private void Build()
    {
        if (dMessage == null && lobby == null)
        {
            Dispose();

            return;
        }

        selectedBackground = new AlphaBlendControl(0.7f)
        {
            Width = Width,
            Height = Height,
        };

        selectedBackground.BaseColor = new(51, 51, 51);
        selectedBackground.IsVisible = false;
        Add(selectedBackground);

        string chanName = string.Empty;

        if (lobby != null)
            chanName = DiscordManager.Instance.GetLobbyName(lobby);

        if (dMessage != null)
            chanName = dMessage.Name();

        var name = TextBox.GetOne(chanName, TrueTypeLoader.EMBEDDED_FONT, 20, DiscordManager.GetUserhue(ID), TextBox.RTLOptions.Default());

        name.X = 5;
        Add(name);

        SetSelected();
    }
}