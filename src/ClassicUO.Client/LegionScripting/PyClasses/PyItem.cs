using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

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

    /// <summary>
    /// If this item matches a grid highlight rule, this is the rule name it matched against
    /// </summary>
    public string MatchingHighlightName = string.Empty;

    /// <summary>
    /// True/False if this matches a grid highlight config
    /// </summary>
    public bool MatchesHighlight;

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
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "PyItem";

    protected Item item;
    protected Item GetItem()
    {
        if (item != null && item.Serial == Serial) return item;

        return MainThreadQueue.InvokeOnMainThread(() =>
        {
            return item = Client.Game.UO.World.Items.TryGetValue(Serial, out item) ? item : null;
        });
    }
}
