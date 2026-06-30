using System.ComponentModel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;

/// <summary>
/// Represents a set of configurations that can be created, deleted, and edited by users
/// </summary>
public interface IProfile : INotifyPropertyChanged
{
    /// <summary>
    /// The profile's name
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Indicates whether the profile can be deleted
    /// </summary>
    bool Deletable { get; }
}
