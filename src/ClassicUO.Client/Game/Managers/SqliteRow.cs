using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// A lightweight, schema-agnostic representation of a single database row: an ordered set of
    /// column name / value pairs. It is the exchange type used by the generic CRUD helpers on
    /// <see cref="SqliteDatabase"/> (<see cref="SqliteDatabase.AddOrUpdateAsync"/>,
    /// <see cref="SqliteDatabase.DeleteAsync"/>, <see cref="SqliteDatabase.GetAsync"/>), so a subclass
    /// only maps its domain object to and from a <see cref="SqliteRow"/> and never writes SQL itself.
    /// <para>
    /// Build one with a collection or dictionary initializer, or with the fluent <see cref="Set"/>:
    /// </para>
    /// <example>
    /// <code>
    /// var row = new SqliteRow { ["id"] = item.Serial, ["name"] = item.Name };
    /// // or
    /// var row = new SqliteRow().Set("id", item.Serial).Set("name", item.Name);
    /// </code>
    /// </example>
    /// Column names are compared case-insensitively to match SQLite's default identifier handling.
    /// </summary>
    public readonly struct SqliteRow : IEnumerable<KeyValuePair<string, object>>
    {
        private readonly Dictionary<string, object> _values;

        /// <summary>Creates an empty row ready to have columns added to it.</summary>
        public SqliteRow()
        {
            _values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        private SqliteRow(Dictionary<string, object> values)
        {
            _values = values;
        }

        /// <summary>The number of columns held by this row.</summary>
        public int Count => _values?.Count ?? 0;

        /// <summary>The column names present in this row, in insertion order.</summary>
        public IEnumerable<string> Columns => _values?.Keys ?? Enumerable.Empty<string>();

        /// <summary>
        /// Gets or sets the raw value for a column. Reading an absent column returns <c>null</c>.
        /// The setter supports the dictionary initializer syntax <c>new SqliteRow { ["c"] = v }</c>.
        /// </summary>
        public object this[string column]
        {
            get => _values != null && _values.TryGetValue(column, out object value) ? value : null;
            set => Values()[column] = value;
        }

        /// <summary>Returns true if the given column is present in this row.</summary>
        public bool Contains(string column) => _values != null && _values.ContainsKey(column);

        /// <summary>
        /// Adds a column/value pair. Supports the collection initializer syntax
        /// <c>new SqliteRow { { "c", v } }</c>. Throws if the column already exists.
        /// </summary>
        public void Add(string column, object value) => Values().Add(column, value);

        /// <summary>Sets a column to a value (adding it if absent) and returns this row for chaining.</summary>
        public SqliteRow Set(string column, object value)
        {
            Values()[column] = value;
            return this;
        }

        /// <summary>
        /// Gets the value for a column converted to <typeparamref name="T"/>. Handles the widening SQLite
        /// performs (INTEGER -&gt; <see cref="long"/>, REAL -&gt; <see cref="double"/>, ...) as well as
        /// enums, <see cref="Guid"/>, and <see cref="Nullable{T}"/> targets. An absent or NULL value
        /// yields <c>default(T)</c>.
        /// </summary>
        public T Get<T>(string column) => (T)ConvertValue(this[column], typeof(T));

        /// <summary>
        /// Tries to get the value for a column converted to <typeparamref name="T"/>. Returns false if the
        /// column is absent or holds NULL.
        /// </summary>
        public bool TryGet<T>(string column, out T value)
        {
            object raw = this[column];
            if (raw is null or DBNull)
            {
                value = default;
                return false;
            }

            value = (T)ConvertValue(raw, typeof(T));
            return true;
        }

        /// <summary>Builds a <see cref="SqliteRow"/> from an existing dictionary (e.g. a Dapper result row).</summary>
        internal static SqliteRow FromValues(IDictionary<string, object> values)
        {
            Dictionary<string, object> copy = new(values.Count, StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object> pair in values)
                copy[pair.Key] = pair.Value is DBNull ? null : pair.Value;

            return new SqliteRow(copy);
        }

        // Lazily materializes the backing store so a default(SqliteRow) can still be mutated. A default
        // instance is used as the "no filter" sentinel for reads, where the store stays null.
        private Dictionary<string, object> Values()
        {
            // The field is readonly, so a genuine default(SqliteRow) cannot be written to. Callers that
            // need to build a row must start from `new SqliteRow()`; this makes that requirement explicit.
            if (_values == null)
                throw new InvalidOperationException(
                    "This SqliteRow was default-initialized and is read-only. Create rows with 'new SqliteRow()'.");

            return _values;
        }

        private static object ConvertValue(object value, Type targetType)
        {
            Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value is null or DBNull)
            {
                // A non-nullable value type must return its default (0, false, ...) rather than null.
                return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                    ? Activator.CreateInstance(targetType)
                    : null;
            }

            if (underlying.IsInstanceOfType(value))
                return value;

            if (underlying.IsEnum)
                return Enum.ToObject(underlying,
                    Convert.ChangeType(value, Enum.GetUnderlyingType(underlying), CultureInfo.InvariantCulture));

            if (underlying == typeof(Guid))
                return value is byte[] bytes ? new Guid(bytes) : Guid.Parse(value.ToString());

            return Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() =>
            (_values ?? (IEnumerable<KeyValuePair<string, object>>)Array.Empty<KeyValuePair<string, object>>())
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
