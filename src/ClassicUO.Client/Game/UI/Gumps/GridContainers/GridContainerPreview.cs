using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps;

public class GridContainerPreview : Gump
{
    private readonly AlphaBlendControl _background;
    private readonly Item _container;

    private const int WIDTH = 170;
    private const int HEIGHT = 150;
    private const int GRIDSIZE = 50;

    public GridContainerPreview(World world, uint serial, int x, int y) : base(world, serial, 0)
    {
        _container = World.Items.Get(serial);
        if (_container == null)
        {
            Dispose();
            return;
        }

        X = x - WIDTH - 20;
        Y = y - HEIGHT - 20;
        _background = new AlphaBlendControl
        {
            Width = WIDTH,
            Height = HEIGHT
        };

        CanCloseWithRightClick = true;
        Add(_background);
        InvalidateContents = true;
    }

    protected override void UpdateContents()
    {
        base.UpdateContents();
        if (InvalidateContents && !IsDisposed && IsVisible)
        {
            if (_container != null && _container.Items != null)
            {
                int currentCount = 0, lastX = 0, lastY = 0;
                for (LinkedObject i = _container.Items; i != null; i = i.Next)
                {

                    var item = (Item)i;
                    if (item == null)
                        continue;

                    if (currentCount > 8)
                        break;

                    var gridItem = new StaticPic(item.DisplayedGraphic, item.Hue)
                    {
                        X = lastX
                    };
                    
                    if (gridItem.X + GRIDSIZE > WIDTH)
                    {
                        gridItem.X = 0;
                        lastX = 0;
                        lastY += GRIDSIZE;

                    }
                    lastX += GRIDSIZE;
                    gridItem.Y = lastY;
                    //gridItem.Width = GRIDSIZE;
                    //gridItem.Height = GRIDSIZE;
                    Add(gridItem);

                    currentCount++;


                }
            }
        }
    }

    public override void Update()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_container == null || _container.IsDestroyed || _container.OnGround && _container.Distance > 3)
        {
            Dispose();

            return;
        }

        base.Update();
    }
}