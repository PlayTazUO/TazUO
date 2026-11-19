using System;
using System.Collections.Generic;

namespace ClassicUO.Utility;

public class ConcurrentPriorityQueue<TElement, TPriority>
{
    public bool IsEmpty => _isEmpty;

    protected readonly PriorityQueue<TElement, (TPriority Priority, long Sequence)> _queue = new(new PrioritySequenceComparer<TPriority>());
    private readonly object _lock = new();
    private bool _isEmpty = true;
    private long _sequence;

    public void Enqueue(TElement element, TPriority priority, long? sequence = null)
    {
        if (element == null) return;

        lock (_lock)
        {
            // Enque using the lowest sequence between sequence and _sequence, or increment _sequence
            _queue.Enqueue(element, (priority, sequence < _sequence + 1 ? sequence.Value : _sequence++));
            _isEmpty = false;
        }
    }

    public bool TryDequeue(out TElement element, out TPriority priority, out long sequence)
    {
        lock (_lock)
        {
            bool res = _queue.TryDequeue(out element, out (TPriority Priority, long Sequence) compositePriority);
            priority = compositePriority.Priority;
            sequence = compositePriority.Sequence;

            _isEmpty = _queue.Count == 0;

            if(_isEmpty)
                _sequence = 0; //reset to avoid potential overrun if client is running for a long time

            return res;
        }
    }

    /// <summary>
    /// Clear all items of a specific priority
    /// </summary>
    /// <param name="priority"></param>
    public void ClearByPriority(TPriority priority) =>
        Retain((_, queuedPriority) => !EqualityComparer<TPriority>.Default.Equals(queuedPriority, priority));

    /// <summary>
    /// Remove all items matching <paramref name="predicate"/>, preserving the priority and
    /// insertion order of the remaining items.
    /// </summary>
    /// <param name="predicate">Return true to remove the item.</param>
    public void RemoveWhere(Predicate<TElement> predicate) => Retain((element, _) => !predicate(element));

    private void Retain(Func<TElement, TPriority, bool> keep)
    {
        lock (_lock)
        {
            var itemsToKeep = new List<(TElement Element, TPriority Priority, long Sequence)>();

            while (_queue.TryDequeue(out TElement element, out (TPriority Priority, long Sequence) compositePriority))
                if (keep(element, compositePriority.Priority))
                    itemsToKeep.Add((element, compositePriority.Priority, compositePriority.Sequence));

            foreach ((TElement Element, TPriority Priority, long Sequence) item in itemsToKeep) _queue.Enqueue(item.Element, (item.Priority, item.Sequence));

            _isEmpty = _queue.Count == 0;

            if (_isEmpty)
                _sequence = 0;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _queue.Clear();
            _isEmpty = true;
        }
    }

    private class PrioritySequenceComparer<T> : IComparer<(T Priority, long Sequence)>
    {
        public int Compare((T Priority, long Sequence) x, (T Priority, long Sequence) y)
        {
            int priorityComparison = Comparer<T>.Default.Compare(x.Priority, y.Priority);
            if (priorityComparison != 0)
                return priorityComparison;

            return x.Sequence.CompareTo(y.Sequence);
        }
    }
}
