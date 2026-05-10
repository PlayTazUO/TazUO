using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps;

internal class VersionHistory : NineSliceGump
{
    private ScrollArea _scrollArea;
    private VBoxContainer _vBoxContainer;

    public VersionHistory(World world) : base(world, 0, 0, 400, 300, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BorderSize, true, 200, 200)
    {
        CanCloseWithRightClick = true;
        CanMove = true;

        Build();

        CenterXInViewPort();
        CenterYInViewPort();
    }

    private void Build()
    {
        Clear();

        Positioner pos = new(13, 13);

        Add(pos.Position(TextBox.GetOne(Language.Instance.TazuoVersionHistory, TrueTypeLoader.EMBEDDED_FONT, 30, Color.White, TextBox.RTLOptions.DefaultCentered(Width))));

        Add(pos.Position(TextBox.GetOne(Language.Instance.CurrentVersion + CUOEnviroment.Version, TrueTypeLoader.EMBEDDED_FONT, 20, Color.Orange, TextBox.RTLOptions.DefaultCentered(Width))));

        _scrollArea = new ScrollArea(0, 0, Width - 26, Height - (pos.LastY + pos.LastHeight) - 32, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };
        _vBoxContainer = new VBoxContainer(_scrollArea.Width - _scrollArea.ScrollBarWidth());
        _scrollArea.Add(_vBoxContainer);

        _vBoxContainer.Add(TextBox.GetOne("Version history can now be found on our GitHub repo CHANGELOG.md file.", TrueTypeLoader.EMBEDDED_FONT, 15, Color.Orange, TextBox.RTLOptions.Default(_scrollArea.Width - _scrollArea.ScrollBarWidth())));
        _vBoxContainer.Add(pos.PositionExact(new HttpClickableLink("Main branch changelog", "https://github.com/PlayTazUO/TazUO/blob/main/CHANGELOG.md", Color.Orange, 15), 25, Height - 20));
        _vBoxContainer.Add(pos.PositionExact(new HttpClickableLink("Dev branch changelog", "https://github.com/PlayTazUO/TazUO/blob/dev/CHANGELOG.md", Color.Orange, 15), 25, Height - 20));

        Add(pos.Position(_scrollArea));

        Add(pos.PositionExact(new HttpClickableLink(Language.Instance.TazUOWiki, "https://github.com/PlayTazUO/TazUO/wiki", Color.Orange, 15), 25, Height - 20));
        Add(pos.PositionExact(new HttpClickableLink(Language.Instance.TazUODiscord, "https://discord.gg/QvqzkB95G4", Color.Orange, 15), Width - 110, Height - 20));
    }

    protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
    {
        base.OnResize(oldWidth, oldHeight, newWidth, newHeight);
        Build();
    }
}
