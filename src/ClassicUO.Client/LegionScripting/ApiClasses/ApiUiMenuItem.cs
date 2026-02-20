namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Represents a Python-accessible item in a menu.
/// </summary>
public class ApiUiMenuItem
{
    public int Index { get;}
    public string Name { get; }
    public ushort Graphic { get; }
    public ushort Hue { get; }

    internal ApiUiMenuItem(int index, string name, ushort graphic, ushort hue)
    {
        Index   = index;
        Name    = name;
        Graphic = graphic;
        Hue     = hue;
    }

    /// <summary>
    /// Returns a readable string representation of the menu item.
    /// Used when printing or converting the object to a string in Python scripts.
    /// </summary>
    public override string ToString() => $"<{__class__} Index={Index:D} Graphic=0x{Graphic:X4} Hue=0x{Hue:X4} Name=\"{Name}\">";

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public virtual string __class__ => nameof(ApiUiMenuItem);

    /// <summary>
    /// Returns a detailed string representation of the object.
    /// This string is used by Python’s built-in <c>repr()</c> function.
    /// </summary>
    /// <returns>A string suitable for debugging and inspection in Python.</returns>
    public virtual string __repr__() => ToString();
}
