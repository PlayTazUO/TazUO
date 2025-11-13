using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Represents a Python-accessible item in the game world.
/// Inherits entity and positional data from <see cref="PyEntity"/>.
/// </summary>
public class PyItem : PyEntity
{
    public int Amount => GetItem()?.Amount ?? 0;
    public bool IsCorpse;
    public bool Opened => GetItem()?.Opened ?? false;
    public uint Container => GetItem()?.Container ?? 0;
    public uint RootContainer => GetItem()?.RootContainer ?? 0;

    /// <summary>
    /// Check if this item is a container(Bag, chest, etc)
    /// </summary>
    public bool IsContainer;

    public PyEntity RootEntity => MainThreadQueue.InvokeOnMainThread<PyEntity>(() =>
    {
        if (item == null)
        {
            item = GetItemUnsafe();
            if (item == null) return null;
        }

        if (SerialHelper.IsMobile(item.RootContainer))
        {
            Client.Game.UO.World.Mobiles.TryGetValue(item.RootContainer, out Mobile m);

            return m != null ? new PyMobile(m) : null;
        }
        else
        {
            Client.Game.UO.World.Items.TryGetValue(item.RootContainer, out Item i);

            return i != null ? new PyItem(i) : null;
        }
    });

    /// <summary>
    /// If this item matches a grid highlight rule, this is the rule name it matched against
    /// </summary>
    public string MatchingHighlightName = string.Empty;

    /// <summary>
    /// True/False if this matches a grid highlight config
    /// </summary>
    public bool MatchesHighlight;

    /// <summary>
    /// If this item is a container ( item.IsContainer ) and is open, this will return the grid container or container gump for it.
    /// </summary>
    /// <returns></returns>
    public PyBaseControl GetContainerGump()
    {
        Item item = GetItem();
        if (item == null) return null;

        Gump result = MainThreadQueue.InvokeOnMainThread(() => UIManager.GetGump(item.Serial));

        if (result is GridContainer || result is ContainerGump)
            return new PyBaseControl(result);

        return null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PyItem"/> class from an <see cref="Item"/>.
    /// </summary>
    /// <param name="item">The item to wrap.</param>
    internal PyItem(Item item) : base(item)
    {
        if (item == null) return;

        IsCorpse =  item.IsCorpse;
        MatchingHighlightName = item.HighlightName;
        MatchesHighlight = item.MatchesHighlightData;
        IsContainer = item.ItemData.IsContainer;
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "PyItem";

    protected Item item;
    protected Item GetItemUnsafe() => Client.Game.UO.World.Items.TryGetValue(Serial, out item) ? item : null;
    protected Item GetItem()
    {
        if (item != null && item.Serial == Serial) return item;

        return MainThreadQueue.InvokeOnMainThread(() =>
        {
            return item = GetItemUnsafe();
        });
    }

    /// <summary>
    /// Gets the item name and properties (tooltip text).
    /// This returns the name and properties in a single string. You can split it by newline if you want to separate them.
    /// </summary>
    /// <param name="wait">True or false to wait for name and props</param>
    /// <param name="timeout">Timeout in seconds</param>
    /// <returns>Item name and properties, or empty string if we don't have them.</returns>
    public string NameAndProps(bool wait = false, int timeout = 10)
    {
        if (wait)
        {
            System.DateTime expire = System.DateTime.UtcNow.AddSeconds(timeout);

            while (!MainThreadQueue.InvokeOnMainThread(() => Client.Game.UO.World.OPL.Contains(Serial)) && System.DateTime.UtcNow < expire)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        return MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (Client.Game.UO.World.OPL.TryGetNameAndData(Serial, out string n, out string d))
            {
                return n + "\n" + d;
            }

            return string.Empty;
        });
    }
}
