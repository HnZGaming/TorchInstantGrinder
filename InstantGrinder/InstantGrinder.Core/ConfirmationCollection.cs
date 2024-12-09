using System;
using System.Collections.Generic;

namespace InstantGrinder.Core
{
    public sealed class ConfirmationCollection
    {
        readonly Dictionary<ulong, (string Query, DateTime Timestamp)> _queries;

        public ConfirmationCollection()
        {
            _queries = new Dictionary<ulong, (string Query, DateTime Timestamp)>();
        }

        public bool IsConfirmation(ulong pid, string gridName)
        {
            return _queries.TryGetValue(pid, out var q)
                   && q.Query == gridName
                   && (DateTime.UtcNow - q.Timestamp).TotalSeconds < 30;
        }

        public void PendConfirmation(string gridName, ulong pid)
        {
            _queries[pid] = (gridName, DateTime.UtcNow);
        }

        public void Clear(ulong pid)
        {
            _queries.Remove(pid);
        }

        public void ClearAll()
        {
            _queries.Clear();
        }
    }
}