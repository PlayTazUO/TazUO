using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;

/// <summary>
///     A generic editor for profile-based configurations.
/// </summary>
/// <typeparam name="TProfile">The type of profile to manage</typeparam>
public class ProfileEditor<TProfile> : Widget where TProfile : IProfile
{
    #region Members

    /// <summary>
    ///     A caller-provided function that returns the UI widget for a given profile.
    /// </summary>
    private readonly Func<TProfile, Widget> _configUiGetter;

    /// <summary>
    ///     Input box used for renaming a profile.
    /// </summary>
    private readonly MyraInputBox _renameInputBox = new();

    /// <summary>
    ///     List of profile references.
    /// </summary>
    private readonly List<TProfile> _profileRefs = [];

    /// <summary>
    ///     Brings a new profile into existence under a given name and stores it: from nothing, or as
    ///     a duplicate of a source profile when one is supplied.
    ///     <para>
    ///         One callback rather than two, because creating and copying end the same way - the
    ///         host adds the profile to whatever pool it owns and persists it. Split apart, every
    ///         host writes that half twice, and a copy that stores before its contents are filled in
    ///         persists a blank.
    ///     </para>
    /// </summary>
    private readonly Func<string, TProfile, TProfile> _createProfile;

    /// <summary>
    ///     An action to be invoked when a profile is deleted via the editor's "Delete" button."
    /// </summary>
    private readonly Action<TProfile> _onDeleteProfile;

    /// <summary>
    ///     An optional action invoked after a profile is renamed via the editor's "Rename" flow.
    /// </summary>
    private readonly Action<TProfile> _onRenameProfile;

    /// <summary>
    ///     Margins for the profile combo box.
    /// </summary>
    private readonly Thickness _profileBoxMargins = new(0, 0, 20, 0);

    /// <summary>
    ///     Width of the profile combo box.
    /// </summary>
    private const int PROFILE_BOX_WIDTH = 225;

    /// <summary>
    ///     The currently selected profile.
    /// </summary>
    private TProfile _selectedProfile;

    /// <summary>
    ///     The current profile's configuration UI
    /// </summary>
    private Widget _currentConfigUi;

    /// <summary>
    ///     A modal used for confirmation dialogs.
    /// </summary>
    private ConfirmationModal _confirmationModal;

    /// <summary>
    ///     Whether the editor is currently renaming a profile.
    /// </summary>
    private bool _isRenaming;

    /// <summary>
    ///     Validation message shown under the rename input, or <see langword="null" /> when the
    ///     current input is valid.
    /// </summary>
    private string _renameError;

    /// <summary>
    ///     Whether a newly created profile goes to the top of the list rather than the bottom. Opt-in:
    ///     it suits a library the user often adds to, and reads as arbitrary reordering everywhere
    ///     else.
    /// </summary>
    private readonly bool _newestFirst;

    #endregion Members

    #region Accessores

    /// <summary>
    ///     Collection of profiles.
    /// </summary>
    public ObservableCollection<TProfile> Profiles { get; } = [];

    #endregion Accessores

    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProfileEditor{TProfile}" /> class.
    /// </summary>
    /// <param name="getConfigUiForProfile">The function to retrieve the UI for a profile.</param>
    /// <param name="createProfile">
    ///     Creates and stores a profile under the given name. The second argument is the profile to
    ///     duplicate, or <see langword="null" /> to build a fresh one.
    /// </param>
    /// <param name="onDeleteProfile">The action to perform when deleting a profile.</param>
    /// <param name="profiles">The initial list of profiles.</param>
    /// <param name="onRenameProfile">An optional action to perform after renaming a profile.</param>
    /// <param name="newestFirst">When true, newly created profiles are listed first.</param>
    public ProfileEditor(
        Func<TProfile, Widget> getConfigUiForProfile,
        Func<string, TProfile, TProfile> createProfile,
        Action<TProfile> onDeleteProfile,
        IEnumerable<TProfile> profiles = null,
        Action<TProfile> onRenameProfile = null,
        bool newestFirst = false
    )
    {
        ArgumentNullException.ThrowIfNull(getConfigUiForProfile);
        ArgumentNullException.ThrowIfNull(createProfile);
        ArgumentNullException.ThrowIfNull(onDeleteProfile);

        _configUiGetter = getConfigUiForProfile;
        _createProfile = createProfile;
        _onDeleteProfile = onDeleteProfile;
        _onRenameProfile = onRenameProfile;
        _newestFirst = newestFirst;

        foreach (TProfile profile in profiles ?? [])
            AddProfile(profile);

        ChildrenLayout = new WrapPanelLayout();

        if (Profiles.Count > 0)
            ChangeOrUpdateProfile(Profiles.First());
        else
            Children.Add(Build());

        Profiles.CollectionChanged += OnProfilesCollectionChanged;
    }

    #endregion Constructors

    #region Private Methods

    #region Button Handlers

    /// <summary>
    ///     Handles the add profile button click.
    /// </summary>
    private void OnAdd() => Introduce(_createProfile(GetNextProfileName(), default));

    /// <summary>Handles the copy button click.</summary>
    private void OnCopy()
    {
        if (_selectedProfile == null)
            return;

        Introduce(_createProfile(GetCopyName(_selectedProfile.Name), _selectedProfile));
    }

    /// <summary>
    ///     Lists a newly created profile and selects it. Selecting it is the point: creating or
    ///     copying is nearly always the first step of editing, and leaving the previous one on screen
    ///     means the change the user makes next lands on the wrong profile.
    /// </summary>
    /// <param name="profile">The profile the host created and stored, or null if it declined.</param>
    private void Introduce(TProfile profile)
    {
        if (profile == null)
            return;

        AddProfile(profile, _newestFirst);
        ChangeOrUpdateProfile(profile);
    }

    /// <summary>
    ///     Handles the rename button click.
    /// </summary>
    private void OnRename()
    {
        _isRenaming = true;
        _renameError = null;
        RebuildUi();

        // Only after the rebuild: SetKeyboardFocus goes through the Desktop, which the input box
        // doesn't have until it has been placed in the tree.
        FocusRenameInput();
    }

    /// <summary>
    ///     Puts the caret in the rename box so the user can type immediately.
    /// </summary>
    private void FocusRenameInput()
    {
        if (_renameInputBox.Desktop == null)
            return;

        _renameInputBox.SetKeyboardFocus();
        _renameInputBox.CursorPosition = _renameInputBox.Text?.Length ?? 0;
    }

    /// <summary>
    ///     Handles the delete button click.
    /// </summary>
    private void OnDelete()
    {
        if (_selectedProfile?.Deletable != true)
            return;

        IGui prevTopmost = UIManager.TopMostControl;

        _confirmationModal?.Dispose();
        _confirmationModal = new ConfirmationModal(
            TazLang.Get("profileeditor_deleteprofile"),
            TazLang.Get("profileeditor_deleteprofilex", [_selectedProfile.Name]),
            confirmed =>
            {
                if (!confirmed)
                    return;

                if (_selectedProfile?.Deletable != true)
                {
                    Log.Warn($"Profile {nameof(TProfile)} is not deletable. This is a logical bug, please report it via GitHub or Discord.");
                    return;
                }

                TProfile removedProfile = _selectedProfile;
                // RemoveProfile updates _selectedProfile so we need to track it first
                RemoveProfile(removedProfile);

                // Invoke the user callback
                _onDeleteProfile(removedProfile);

                // Restore focus back to the parent control
                UIManager.MakeTopMostGump(prevTopmost);
            }
        );

        UIManager.Add(_confirmationModal);
    }

    /// <summary>
    ///     Handles the rename save button click.
    /// </summary>
    private void OnRenameSave()
    {
        if (_selectedProfile == null)
            return;

        string newName = _renameInputBox.Text;
        if (string.IsNullOrWhiteSpace(newName))
        {
            _renameError = TazLang.Get("profileeditor_emptyname", "Name cannot be empty.");
            RebuildUi();
            return;
        }

        newName = newName.Trim();

        bool collides = Profiles.Any(profile =>
            !ReferenceEquals(profile, _selectedProfile)
            && string.Equals(profile.Name, newName, StringComparison.OrdinalIgnoreCase)
        );

        if (collides)
        {
            _renameError = TazLang.Get("profileeditor_duplicatename", "A profile with this name already exists.");
            RebuildUi();
            return;
        }

        _selectedProfile.Name = newName;
        _onRenameProfile?.Invoke(_selectedProfile);

        _isRenaming = false;
        _renameError = null;
        RebuildUi();
    }

    /// <summary>
    ///     Handles the rename cancel button click.
    /// </summary>
    private void OnRenameCancel()
    {
        _isRenaming = false;
        _renameError = null;
        RebuildUi();
    }

    #endregion Button Handlers

    #region UI Building

    /// <summary>
    ///     Builds the UI for the profile editor.
    /// </summary>
    /// <returns>The constructed panel.</returns>
    private StackPanel Build()
    {
        Widget content = _currentConfigUi ?? new Panel();
        content.Enabled = !_isRenaming;

        // Stacked, not wrapped. These four are a vertical sequence, and a vertical WrapPanel
        // answers a config UI taller than the window by starting a second column beside the
        // toolbar rather than by overflowing into the window's own scroller. The content itself
        // still wraps horizontally.
        StackPanel panel = OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            OptionsFactory.CreateSpacer(),
            GetToolbar(),
            OptionsFactory.CreateSpacer(),
            OptionTabCommons.StyledHorizontalWrapPanel(
                content
            )
        );

        panel.VerticalAlignment = VerticalAlignment.Top;

        return panel;
    }

    /// <summary>
    ///     Gets the toolbar based on the current renaming state.
    /// </summary>
    /// <returns>The stack panel representing the toolbar.</returns>
    private StackPanel GetToolbar() => _isRenaming ? GetRenamingToolbar() : GetNormalToolbar();

    /// <summary>
    ///     Gets the normal toolbar when not in renaming mode.
    /// </summary>
    /// <returns>The normal toolbar stack panel.</returns>
    private StackPanel GetNormalToolbar()
    {
        bool canEdit = _selectedProfile is { Deletable: true };

        var buttons = new List<Widget>
        {
            GetProfilesCombo(),
            new MyraButton(TazLang.Get("profileeditor_add"), OnAdd),
            // Offered for a read-only profile too - that is precisely when it is wanted, since copying
            // is the only way to get an editable version of one.
            new MyraButton(TazLang.Get("profileeditor_copy", "Copy"), OnCopy)
            {
                Enabled = _selectedProfile != null,
                Tooltip = TazLang.Get("profileeditor_copy_tooltip", "Duplicate this profile, and edit the copy.")
            },
            new MyraButton(TazLang.Get("profileeditor_rename"), OnRename) { Enabled = canEdit, Tooltip = TazLang.Get("profileeditor_cannotrenamebuiltinprofile") },
            new MyraButton(TazLang.Get("profileeditor_delete"), OnDelete) { Enabled = canEdit, Tooltip = TazLang.Get("profileeditor_cannotdeletebuiltinprofile") }
        };

        StackPanel panel = OptionTabCommons.StyledStackPanel(Orientation.Horizontal, [.. buttons]);

        panel.Margin = new Thickness(0, 0, 0, 10);

        return OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            panel,
            OptionTabCommons.StyledHorizontalSeparator()
        );
    }

    /// <summary>
    ///     Gets the renaming toolbar when in renaming mode.
    /// </summary>
    /// <returns>The renaming toolbar stack panel.</returns>
    private StackPanel GetRenamingToolbar()
    {
        StackPanel panel = OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            GetRenameProfileInput(),
            new MyraButton(TazLang.Get("profileeditor_save"), OnRenameSave),
            new MyraButton(TazLang.Get("profileeditor_cancel"), OnRenameCancel)
        );

        panel.Margin = new Thickness(0, 0, 0, 10);

        var children = new List<Widget> { panel };

        if (_renameError != null)
            children.Add(new MyraLabel(_renameError, MyraLabel.TextStyle.P) { TextColor = Color.OrangeRed });

        children.Add(OptionTabCommons.StyledHorizontalSeparator());

        return OptionTabCommons.StyledStackPanel(Orientation.Vertical, [.. children]);
    }

    /// <summary>
    ///     Gets the profiles combo box stack panel. Searchable, because a library grows and scrolling
    ///     a long alphabetical list to find one look is the slowest way to do it.
    /// </summary>
    /// <returns>The profiles combo box stack panel.</returns>
    private Widget GetProfilesCombo()
    {
        string selectedProfileName = _selectedProfile?.Name ?? Profiles.FirstOrDefault()?.Name ?? string.Empty;

        // addSelectedItemIfMissing is off: every name shown comes from Profiles, so a missing one
        // would be a bug rather than a stale setting worth preserving.
        var combo = new ContainsLevenshteinComboBox(
            selectedProfileName,
            Profiles.Select(profile => profile.Name),
            name =>
            {
                if (name != null)
                    OnProfileSelected(name);
            },
            addSelectedItemIfMissing: false
        )
        {
            VerticalAlignment = VerticalAlignment.Center,
            TooltipSelector = name => name
        };

        MyraStyle.ApplySearchComboBoxPopupBorder(combo);

        combo.Width = PROFILE_BOX_WIDTH;
        combo.Margin = _profileBoxMargins;

        return new MyraLabel(TazLang.Get("profileeditor_profile"), MyraLabel.TextStyle.P).PlaceBefore(combo);
    }

    /// <summary>
    ///     Gets the rename profile input stack panel.
    /// </summary>
    /// <returns>The rename profile input stack panel.</returns>
    private StackPanel GetRenameProfileInput()
    {
        var panelLabel = new MyraLabel(TazLang.Get("profileeditor_profile"), MyraLabel.TextStyle.P);
        _renameInputBox.Text = _selectedProfile?.Name;

        // Ultimately this should always yield 200, but we keep this for the dynamic calculation
        _renameInputBox.Width = PROFILE_BOX_WIDTH - (panelLabel.Measure(new Point(PROFILE_BOX_WIDTH, 60)).X + MyraStyle.STANDARD_SPACING);

        StackPanel panel = OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            panelLabel,
            _renameInputBox
        );

        panel.Width = PROFILE_BOX_WIDTH;
        panel.Margin = _profileBoxMargins;

        return panel;
    }

    /// <summary>
    ///     Rebuilds the UI of the editor.
    /// </summary>
    private void RebuildUi()
    {
        Children.Clear();
        Children.Add(Build());
    }

    #endregion UI Building

    #region Profile Management Logic

    /// <summary>
    ///     Adds a profile to the editor.
    /// </summary>
    /// <param name="profile">The profile to add.</param>
    /// <param name="atTop">When true, the profile is listed first rather than last.</param>
    private void AddProfile(TProfile profile, bool atTop = false)
    {
        profile.PropertyChanged += OnProfilePropertyChanged;

        if (atTop)
            Profiles.Insert(0, profile);
        else
            Profiles.Add(profile);

        _profileRefs.Add(profile);
    }

    /// <summary>
    ///     Removes a profile from the editor.
    /// </summary>
    /// <param name="profile">The profile to remove.</param>
    private void RemoveProfile(TProfile profile)
    {
        if (profile == null)
            return;

        profile.PropertyChanged -= OnProfilePropertyChanged;
        Profiles.Remove(profile);
        _profileRefs.Remove(profile);

        // Since we've deleted a profile, we may need to display an empty state
        if (Profiles.Count > 0)
            ChangeOrUpdateProfile(Profiles.First());
        else
            Children.Add(Build());
    }

    /// <summary>
    ///     Handles the property-changed event of a profile.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The property changed event arguments.</param>
    private void OnProfilePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (!sender.Equals(_selectedProfile))
            return;

        // Re-render with the updated content
        ChangeOrUpdateProfile(_selectedProfile);
    }

    /// <summary>
    ///     Handles the profile selection event.
    /// </summary>
    /// <param name="selectedName">The name of the selected profile.</param>
    private void OnProfileSelected(string selectedName)
    {
        TProfile newValue = Profiles.FirstOrDefault(p => p.Name == selectedName);
        ChangeOrUpdateProfile(newValue);
    }

    /// <summary>
    ///     Changes or updates the current profile.
    /// </summary>
    /// <param name="profile">The profile to set as current.</param>
    private void ChangeOrUpdateProfile(TProfile profile)
    {
        _selectedProfile = profile;
        _currentConfigUi = _configUiGetter(profile);
        RebuildUi();
    }

    /// <summary>
    ///     Handles the profiles collection changed event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The collection changed event arguments.</param>
    private void OnProfilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                OnProfilesAddedToCollection(e);
                break;

            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Replace:
                OnProfilesRemovedFromCollection(e);
                break;
            case NotifyCollectionChangedAction.Reset:
                OnProfilesCollectionCleared();
                break;
        }

        RebuildUi();
    }

    /// <summary>
    ///     Handles profiles added to the collection.
    /// </summary>
    /// <param name="e">The collection changed event arguments.</param>
    private void OnProfilesAddedToCollection(NotifyCollectionChangedEventArgs e)
    {
        if (!(e.NewItems?.Count > 0))
            return;

        foreach (TProfile newProfile in e.NewItems ?? Array.Empty<TProfile>())
        {
            newProfile.PropertyChanged += OnProfilePropertyChanged;
            _profileRefs.Add(newProfile);
        }

        if (!(e.OldItems?.Count > 0))
            OnProfileSelected(Profiles.First().Name);
    }

    /// <summary>
    ///     Handles profiles removed from the collection.
    /// </summary>
    /// <param name="e">The collection changed event arguments.</param>
    private void OnProfilesRemovedFromCollection(NotifyCollectionChangedEventArgs e)
    {
        foreach (TProfile removedProfile in e.OldItems ?? Array.Empty<TProfile>())
        {
            removedProfile.PropertyChanged -= OnProfilePropertyChanged;
            _profileRefs.Remove(removedProfile);
        }
    }

    /// <summary>
    ///     Handles the collection cleared event.
    /// </summary>
    private void OnProfilesCollectionCleared()
    {
        foreach (TProfile profile in _profileRefs)
            profile.PropertyChanged -= OnProfilePropertyChanged;
        _profileRefs.Clear();
    }

    /// <summary>
    ///     A free name for a copy of <paramref name="original" />, numbered only as far as it has to
    ///     be so that repeated copying does not produce "X (copy) (copy) (copy)".
    /// </summary>
    /// <param name="original">The name being copied from.</param>
    /// <returns>A name no existing profile holds.</returns>
    private string GetCopyName(string original)
    {
        string candidate = TazLang.Get("profileeditor_copyofx", [original]);

        if (Profiles.All(profile => profile.Name != candidate))
            return candidate;

        int index = 2;

        while (Profiles.Any(profile => profile.Name == $"{candidate} {index}"))
            index++;

        return $"{candidate} {index}";
    }

    /// <summary>
    ///     Gets the next profile name.
    /// </summary>
    /// <returns>The next profile name.</returns>
    private string GetNextProfileName()
    {
        string profileWord = TazLang.Get("profileeditor_profile");

        int index = 1;
        while (Profiles.Any(p => p.Name == $"{profileWord} {index}"))
            index++;
        return $"{profileWord} {index}";
    }

    #endregion Profile Management Logic

    #endregion Private Methods
}
