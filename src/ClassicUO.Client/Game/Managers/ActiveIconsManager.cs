// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    public sealed class ActiveSpellIconsManager
    {
        private readonly HashSet<ushort> _activeIcons = new HashSet<ushort>();

        public void Add(ushort id)
        {
            if (!IsActive(id))
            {
                _activeIcons.Add(id);
            }
        }

        public void Remove(ushort id)
        {
            if (IsActive(id))
            {
                _activeIcons.Remove(id);
            }
        }

        public bool IsActive(ushort id) => _activeIcons.Count != 0 && _activeIcons.Contains(id);

        /// <summary>The spell ids currently toggled on (a snapshot copy).</summary>
        public ushort[] GetActive()
        {
            ushort[] result = new ushort[_activeIcons.Count];
            _activeIcons.CopyTo(result);
            return result;
        }

        public void Clear() => _activeIcons.Clear();
    }
}