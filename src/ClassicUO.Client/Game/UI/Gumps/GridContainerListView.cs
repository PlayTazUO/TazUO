using ClassicUO.Configuration;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.UI.Gumps
{
    public partial class GridContainer
    {
        private const int LIST_ROW_HEIGHT = 40;
        private const int LIST_ICON_SIZE = 40;
        private const int LIST_NAME_MAX_CHARS = 20;
        private const int LIST_COLUMN_WIDTH = 200;
        private const int LIST_COLUMN_GAP = 8;

        private enum GridContainerViewMode
        {
            Grid = 0,
            List = 1
        }

        private enum GridContainerViewModeOverride
        {
            Default = 0,
            Grid = 1,
            List = 2
        }

        private GridContainerViewMode EffectiveViewMode
        {
            get
            {
                GridContainerViewModeOverride viewOverride = GetContainerViewModeOverride();

                if (viewOverride == GridContainerViewModeOverride.Grid)
                    return GridContainerViewMode.Grid;

                if (viewOverride == GridContainerViewModeOverride.List)
                    return GridContainerViewMode.List;

                int mode = ProfileManager.CurrentProfile?.GridContainerViewMode ?? (int)GridContainerViewMode.Grid;
                return mode == (int)GridContainerViewMode.List ? GridContainerViewMode.List : GridContainerViewMode.Grid;
            }
        }

        private bool IsListView => EffectiveViewMode == GridContainerViewMode.List;

        private void InitializeListView() => EventSink.OPLOnReceive += OnListViewOplReceived;

        private void DisposeListView() => EventSink.OPLOnReceive -= OnListViewOplReceived;

        private void OnListViewOplReceived(object sender, OPLEventArgs e)
        {
            if (!IsListView)
                return;

            SlotManager?.FindItem(e.Serial)?.RefreshListName();
        }

        private GridContainerViewModeOverride GetContainerViewModeOverride()
        {
            int mode = _gridContainerEntry?.ViewModeOverride ?? (int)GridContainerViewModeOverride.Default;

            return mode switch
            {
                (int)GridContainerViewModeOverride.Grid => GridContainerViewModeOverride.Grid,
                (int)GridContainerViewModeOverride.List => GridContainerViewModeOverride.List,
                _ => GridContainerViewModeOverride.Default
            };
        }

        private void SetContainerViewModeOverride(GridContainerViewModeOverride mode)
        {
            _gridContainerEntry.ViewModeOverride = (int)mode;
            _openRegularGump.ContextMenu = GenContextMenu();
            _gridContainerEntry.UpdateSaveDataEntry(this);
            RequestUpdateContents();
        }

        private int GetContainerViewModeOverrideIndex() => (int)GetContainerViewModeOverride();

        private void SetContainerViewModeOverrideIndex(int index)
        {
            SetContainerViewModeOverride(index switch
            {
                (int)GridContainerViewModeOverride.Grid => GridContainerViewModeOverride.Grid,
                (int)GridContainerViewModeOverride.List => GridContainerViewModeOverride.List,
                _ => GridContainerViewModeOverride.Default
            });
        }
    }
}
