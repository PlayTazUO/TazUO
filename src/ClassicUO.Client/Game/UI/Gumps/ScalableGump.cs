using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Base class for gumps that support automatic scaling of all child controls.
    /// When GumpScale is set, all subsequently added children will be automatically scaled.
    /// </summary>
    public abstract class ScalableGump : Gump
    {
        private double _gumpScale = 1.0;
        private bool _autoScaleChildren = true;

        protected ScalableGump(World world, uint local, uint server) : base(world, local, server)
        {
        }

        /// <summary>
        /// The scale factor for this gump. When set, all subsequently added children will be automatically scaled.
        /// This only affects children - the gump container itself is not scaled.
        /// </summary>
        protected double GumpScale
        {
            get => _gumpScale;
            set
            {
                _gumpScale = value;
                // Don't set Scale/InternalScale on the gump itself - only children should be scaled
            }
        }

        /// <summary>
        /// Whether to automatically scale children when they are added. Default is true.
        /// Set to false if you need to manually control scaling for specific controls.
        /// </summary>
        protected bool AutoScaleChildren
        {
            get => _autoScaleChildren;
            set => _autoScaleChildren = value;
        }

        /// <summary>
        /// Override Add to automatically scale children when AutoScaleChildren is true
        /// </summary>
        public override T Add<T>(T control, int page = 0)
        {
            if (_autoScaleChildren && _gumpScale != 1.0)
            {
                control.ApplyScale(_gumpScale);
            }
            return base.Add(control, page);
        }

        /// <summary>
        /// Apply scale to all existing children. Useful when changing scale after controls have been added.
        /// WARNING: Only call this on freshly constructed gumps or with force=true.
        /// If children were already scaled via Add(), calling this without force may still scale nested children.
        /// </summary>
        protected void ApplyScaleToAllChildren(bool scalePosition = true, bool scaleSize = true, bool force = false)
        {
            foreach (Control child in Children)
            {
                child.ApplyScale(_gumpScale, scalePosition, scaleSize, force);
            }
        }

        /// <summary>
        /// Recursively apply <see cref="GumpScale"/> to a control and every one of its descendants.
        /// Needed for composite controls whose visuals live in nested children (the shallow scale done
        /// by <see cref="Add{T}"/> only touches the top-level control). The per-control InternalScale
        /// guard makes this safe to call again on subtrees that already contain some scaled controls.
        /// </summary>
        /// <param name="control">Root of the subtree to scale.</param>
        /// <param name="scaleRootPosition">Whether to scale the root's own X/Y (descendants are always positioned).</param>
        protected void ApplyScaleRecursive(Control control, bool scaleRootPosition = true)
        {
            if (_gumpScale == 1.0 || control == null)
                return;

            ScaleTree(control, scaleRootPosition, _gumpScale);
        }

        private static void ScaleTree(IGui control, bool scalePosition, double scale)
        {
            control.ApplyScale(scale, scalePosition: scalePosition);

            for (int i = 0; i < control.Children.Count; i++)
            {
                ScaleTree(control.Children[i], true, scale);
            }
        }
    }
}
