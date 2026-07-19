using System.Collections;
using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// A lightweight, ordered set of column/value pairs used to add or update a database row without
    /// writing SQL by hand. Supports collection-initializer syntax as well as a fluent API:
    /// <code>
    /// // collection initializer
    /// var row = new SqliteRow { { "serial", 1 }, { "name", "foo" } };
    ///
    /// // fluent
    /// var row = new SqliteRow().Set("serial", 1).Set("name", "foo");
    /// </code>
    /// Pass the row to <see cref="SqliteDatabase.AddRowAsync"/> or
    /// <see cref="SqliteDatabase.AddOrUpdateAsync"/>. All values are bound as SQL parameters, so any
    /// value type supported by Microsoft.Data.Sqlite is safe to use.
    /// </summary>
    public struct SqliteRow : IEnumerable<KeyValuePair<string, object>>
    {
        private List<KeyValuePair<string, object>> _columns;

        /// <summary>The number of columns currently in this row.</summary>
        public readonly int Count => _columns?.Count ?? 0;

        /// <summary>The ordered column/value pairs that make up this row.</summary>
        public readonly IReadOnlyList<KeyValuePair<string, object>> Columns
            => (IReadOnlyList<KeyValuePair<string, object>>)_columns ?? System.Array.Empty<KeyValuePair<string, object>>();

        /// <summary>
        /// Adds a column/value pair. Enables collection-initializer syntax
        /// (<c>new SqliteRow { { "col", value } }</c>).
        /// </summary>
        public void Add(string column, object value)
        {
            _columns ??= new List<KeyValuePair<string, object>>();
            _columns.Add(new KeyValuePair<string, object>(column, value));
        }

        /// <summary>Adds a column/value pair and returns this row for fluent chaining.</summary>
        public SqliteRow Set(string column, object value)
        {
            Add(column, value);
            return this;
        }

        /// <summary>Gets or sets the value for a column. The setter appends a new pair.</summary>
        public object this[string column]
        {
            readonly get
            {
                if (_columns != null)
                {
                    foreach (KeyValuePair<string, object> pair in _columns)
                    {
                        if (pair.Key == column)
                            return pair.Value;
                    }
                }

                return null;
            }
            set => Add(column, value);
        }

        public readonly IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            => (_columns ?? new List<KeyValuePair<string, object>>()).GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
