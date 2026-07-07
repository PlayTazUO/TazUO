using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.UI.Gumps
{
    public partial class GridContainer
    {
        public const int LIST_ICON_SIZE = 40;
        public const int LIST_NAME_MAX_CHARS = 20;
        public const int LIST_ROW_HEIGHT = 40;
        public const int LIST_COLUMN_WIDTH = 200;
        public const int LIST_COLUMN_GAP = 8;

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

        public bool IsListView => EffectiveViewMode == GridContainerViewMode.List;

        private void InitializeListView() => EventSink.OPLOnReceive += OnListViewOplReceived;

        private void DisposeListView() => EventSink.OPLOnReceive -= OnListViewOplReceived;

        private void OnListViewOplReceived(object sender, OPLEventArgs e)
        {
            if (!IsListView)
                return;

            SlotManager?.FindItem(e.Serial)?.RefreshListName();
        }

        private static GridContainerViewModeOverride ToViewModeOverride(int value) =>
            Enum.IsDefined((GridContainerViewModeOverride)value)
                ? (GridContainerViewModeOverride)value
                : GridContainerViewModeOverride.Default;

        private GridContainerViewModeOverride GetContainerViewModeOverride() =>
            ToViewModeOverride(_gridContainerEntry?.ViewModeOverride ?? (int)GridContainerViewModeOverride.Default);

        private void SetContainerViewModeOverride(GridContainerViewModeOverride mode)
        {
            _gridContainerEntry.ViewModeOverride = (int)mode;
            _openRegularGump.ContextMenu = GenContextMenu();
            _gridContainerEntry.UpdateSaveDataEntry(this);
            RequestUpdateContents();
        }

        private int GetContainerViewModeOverrideIndex() => (int)GetContainerViewModeOverride();

        private void SetContainerViewModeOverrideIndex(int index) => SetContainerViewModeOverride(ToViewModeOverride(index));
    }
}
