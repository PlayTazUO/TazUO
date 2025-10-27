using System;
using System.Collections.Concurrent;

namespace ClassicUO.Game.Managers
{
    public sealed class GlobalPriorityQueue
    {
        public static GlobalPriorityQueue Instance { get; private set; } = new();

        public bool IsEmpty => _isEmpty;

        private bool _isEmpty = true;
        private readonly ConcurrentQueue<Action> _actions = new();

        private GlobalPriorityQueue()
        {
        }

        public void Update()
        {
            if (_isEmpty) return;
            if (GlobalActionCooldown.IsOnCooldown) return;

            if (!_actions.TryDequeue(out Action action))
                return;

            action?.Invoke();
            GlobalActionCooldown.BeginCooldown();
            _isEmpty = _actions.IsEmpty;
        }

        public void Enqueue(Action action)
        {
            if (action == null) return;

            _actions.Enqueue(action);
            _isEmpty = false;
        }

        public void Clear()
        {
            while (_actions.TryDequeue(out Action _))
            {
            }
            _isEmpty = true;
        }
    }
}