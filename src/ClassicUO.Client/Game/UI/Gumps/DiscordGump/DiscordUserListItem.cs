using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Discord.Sdk;
using DiscordSocialSDK.Wrapper;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls;

public class DiscordUserListItem : Control
{
    private readonly DiscordGump gump;
    private UserHandle user;
    private Color offline = new Color(1, 0, 0, 0.2f);
    private AlphaBlendControl selectedBackground;
    private readonly int sx, sy, sw, sh;
    private Vector3 shue;
    private long showPopupAt = long.MaxValue;
    private bool showPopupMouseOver;
    private World world;
    
    public override bool AcceptMouseInput => true;
    public DiscordUserListItem(World world, DiscordGump gump, UserHandle user, int width = 100, int height = 25)
    {
        this.world = world;
        Width = width;
        Height = height;
        CanMove = true;
        CanCloseWithRightClick = true;
        this.gump = gump;
        this.user = user;

        sx = Width - 10;
        sy = (Height - 5) / 2;
        sw = 5;
        sh = 5;

        Build();
    }

    public void SetSelected()
    {
        if (gump.ActiveChannel == user.Id())
            selectedBackground.IsVisible = true;
        else
            selectedBackground.IsVisible = false;
    }

    protected override void OnMouseEnter(int x, int y)
    {
        base.OnMouseEnter(x, y);
        showPopupAt = Time.Ticks + 1250;
        showPopupMouseOver = true;
    }

    protected override void OnMouseExit(int x, int y)
    {
        base.OnMouseExit(x, y);
        showPopupMouseOver = false;
    }

    public override void Update()
    {
        base.Update();

        if (showPopupMouseOver && Time.Ticks >= showPopupAt)
        {
            if (showPopupMouseOver)
            {
                UIManager.Add(new DiscordUserPopupGump(world, user.Id(), Mouse.Position.X + 20, Mouse.Position.Y + 20));
            }

            showPopupAt = long.MaxValue;
        }
    }

    protected override void OnMouseUp(int x, int y, MouseButtonType button)
    {
        base.OnMouseUp(x, y, button);

        if (button == MouseButtonType.Left)
        {
            gump?.SetActiveChatChannel(user.Id(), true);
        }
    }

    private void Build()
    {
        if (user == null)
        {
            Dispose();

            return;
        }
        
        selectedBackground = new AlphaBlendControl(0.7f)
        {
            Width = Width,
            Height = Height,
            AcceptMouseInput = true
        };

        selectedBackground.BaseColor = new(51,51,51);
        selectedBackground.IsVisible = false;
        Add(selectedBackground);

        var name = TextBox.GetOne(user.DisplayName(), TrueTypeLoader.EMBEDDED_FONT, 20, DiscordManager.GetUserhue(user.Id()), TextBox.RTLOptions.Default());
        
        name.X = 5;
        Add(name);

        if(user.GameActivity() != null) //In TUO
        {
            PNGLoader.Instance.TryGetEmbeddedTexture("TazUOSM.png", out Microsoft.Xna.Framework.Graphics.Texture2D tuoTexture);
            var tuo = new EmbeddedGumpPic(Width - 75, 2, tuoTexture);
            Add(tuo);
        }
        
        if (user.IsOnline())
            shue = ShaderHueTranslator.GetHueVector(72, false, 1f);
        else
            shue = ShaderHueTranslator.GetHueVector(32, false, 1f);
        
        
        SetSelected();
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        base.Draw(batcher, x, y);
        batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.White),x + sx, y + sy, sw, sh, shue);

        return true;
    }
}