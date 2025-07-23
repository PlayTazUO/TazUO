namespace ClassicUO.Game.UI.Controls;

public class TableContainer : Control
{
    private Positioner pos;
    private bool repositionRequested;
    private int columns, columnWidth, leftPad;

    public TableContainer(int width, int columns, int columnWidth, int leftpad = 1, int toppad = 1)
    {
        CanMove = true;
        Width = width;
        this.columns = columns;
        this.columnWidth = columnWidth;
        this.leftPad = leftpad;
        pos = new(leftpad, toppad);
        pos.StartTable(columns, columnWidth, leftpad);
    }

    public override bool AcceptMouseInput { get; set; } = true;

    public void Add(Control c, bool rePosition, int page = 0)
    {
        repositionRequested = rePosition;
        Add(c, page);
    }

    public override void Add(Control c, int page = 0)
    {
        base.Add(c, page);

        pos.Position(c);

        c.UpdateOffset(0, Offset.Y);

        if (repositionRequested)
            Reposition();
        else
            UpdateSize(c); //Reposition is not requested, so we update the size of the container
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
        pos.StartTable(columns, columnWidth, leftPad);

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
