using ClassicUO.Game.UI;

namespace ClassicUO.Game.UI.Controls;

public class VBoxContainer : Control
{
    private Positioner pos;
    private bool repositionRequested;

    public VBoxContainer(int width, int leftpad = 1, int toppad = 1)
    {
        CanMove = true;
        Width = width;
        pos = new(leftpad, toppad);
    }

    public override bool AcceptMouseInput { get; set; } = true;

    public void Add(Control c, bool rePosition, int page = 0)
    {
        repositionRequested = rePosition;
        Add(c, page);
    }

    public void BlankLine() => pos.BlankLine();

    public override T Add<T>(T c, int page = 0)
    {
        base.Add(c, page);

        pos.Position(c);

        c.UpdateOffset(0, Offset.Y);

        if (repositionRequested)
            Reposition();
        else
            UpdateSize(c); //Reposition is not requested, so we update the size of the container

        return c;
    }

    public override void Clear()
    {
        base.Clear();
        Reposition();
    }

    protected override void OnChildRemoved()
    {
        base.OnChildRemoved();
        Reposition(); //Need to reposition, we don't know where the child was removed
    }

    private void Reposition()
    {
        repositionRequested = false;

        pos.Reset();

        foreach (Control child in Children)
        {
            if (!child.IsVisible || child.IsDisposed)
                continue;

            pos.Position(child);
        }

        UpdateSize();
    }

    private void UpdateSize()
    {
        int h = 0;
        foreach (Control child in Children)
        {
            if(!child.IsVisible || child.IsDisposed) continue;

            if (child.Height + child.Y > h)
                h = child.Height + child.Y;
        }

        Height = h;
    }

    private void UpdateSize(Control c)
    {
        if(!c.IsVisible || c.IsDisposed) return;

        Height = c.Height + c.Y;
    }
}
