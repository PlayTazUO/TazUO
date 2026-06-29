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
    ///     A function that creates a new profile with a given name
    /// </summary>
    private readonly Func<string, TProfile> _createProfile;

    /// <summary>
    ///     An action to be invoked when a profile is deleted via the editor's "Delete" button."
    /// </summary>
    private readonly Action<TProfile> _onDeleteProfile;

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
    /// <param name="createProfile">The function to create a new profile.</param>
    /// <param name="onDeleteProfile">The action to perform when deleting a profile.</param>
    /// <param name="profiles">The initial list of profiles.</param>
    public ProfileEditor(
        Func<TProfile, Widget> getConfigUiForProfile,
        Func<string, TProfile> createProfile,
        Action<TProfile> onDeleteProfile,
        IEnumerable<TProfile> profiles = null
    )
    {
        ArgumentNullException.ThrowIfNull(getConfigUiForProfile);
        ArgumentNullException.ThrowIfNull(createProfile);
        ArgumentNullException.ThrowIfNull(onDeleteProfile);

        _configUiGetter = getConfigUiForProfile;
        _createProfile = createProfile;
        _onDeleteProfile = onDeleteProfile;

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
    private void OnAdd()
    {
        TProfile newProfile = _createProfile(GetNextProfileName());
        AddProfile(newProfile);
        ChangeOrUpdateProfile(newProfile);
    }

    /// <summary>
    ///     Handles the rename button click.
    /// </summary>
    private void OnRename()
    {
        _isRenaming = true;
        RebuildUi();
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
            TazLang.Get("profileeditor_deleteprofilex", new[] { _selectedProfile.Name }),
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
            return;

        _selectedProfile.Name = newName;

        _isRenaming = false;
        RebuildUi();
    }

    /// <summary>
    ///     Handles the rename cancel button click.
    /// </summary>
    private void OnRenameCancel()
    {
        _isRenaming = false;
        RebuildUi();
    }

    #endregion Button Handlers

    #region UI Building

    /// <summary>
    ///     Builds the UI for the profile editor.
    /// </summary>
    /// <returns>The constructed wrap panel.</returns>
    private WrapPanel Build()
    {
        Widget content = _currentConfigUi ?? new Panel();
        content.Enabled = !_isRenaming;

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateSpacer(),
            GetToolbar(),
            OptionsFactory.CreateSpacer(),
            OptionTabCommons.StyledHorizontalWrapPanel(
                content
            )
        );
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
        StackPanel panel = OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            GetProfilesCombo(),
            new MyraButton(TazLang.Get("profileeditor_add"), OnAdd),
            new MyraButton(TazLang.Get("profileeditor_rename"), OnRename) { Enabled = canEdit, Tooltip = TazLang.Get("profileeditor_cannotrenamebuiltinprofile") },
            new MyraButton(TazLang.Get("profileeditor_delete"), OnDelete) { Enabled = canEdit, Tooltip = TazLang.Get("profileeditor_cannotdeletebuiltinprofile") }
        );

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

        return OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            panel,
            OptionTabCommons.StyledHorizontalSeparator()
        );
    }

    /// <summary>
    ///     Gets the profiles combo box stack panel.
    /// </summary>
    /// <returns>The profiles combo box stack panel.</returns>
    private Widget GetProfilesCombo()
    {
        string selectedProfileName = _selectedProfile?.Name ?? Profiles.FirstOrDefault()?.Name ?? string.Empty;

        Widget combo = OptionTabCommons.CreateOptionsComboBox(TazLang.Get("profileeditor_profile"),
            selectedProfileName,
            Profiles?.Select(p => p.Name) ?? [],
            OnProfileSelected
        );

        combo.Width = PROFILE_BOX_WIDTH;
        combo.Margin = _profileBoxMargins;

        return combo;
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
        _renameInputBox.OnGotKeyboardFocus();
        _renameInputBox.CursorPosition = _renameInputBox?.Text?.Length ?? 0;

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
    private void AddProfile(TProfile profile)
    {
        profile.PropertyChanged += OnProfilePropertyChanged;
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
    ///     Handles the property changed event of a profile.
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
