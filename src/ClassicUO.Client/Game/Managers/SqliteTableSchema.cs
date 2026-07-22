using System;
using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Declares a table's name and columns for use with <see cref="SqliteDatabase.EnsureTableAsync"/>.
    /// Passing the same schema on every startup keeps the table in sync as columns are added to or
    /// removed from the declaration over time - see <see cref="SqliteDatabase.EnsureTableAsync"/> for
    /// exactly what is and is not reconciled.
    /// </summary>
    /// <example>
    /// <code>
    /// private static readonly SqliteTableSchema ThingsTable = new("things",
    ///     SqliteColumn.Int("id", primaryKey: true),
    ///     SqliteColumn.Str("name", notNull: true, def: "''"));
    /// </code>
    /// </example>
    public readonly struct SqliteTableSchema
    {
        /// <summary>The table name.</summary>
        public readonly string Name;

        /// <summary>The columns that define the table.</summary>
        public readonly IReadOnlyList<SqliteColumn> Columns;

        public SqliteTableSchema(string name, params SqliteColumn[] columns)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Table name is required.", nameof(name));

            if (columns == null || columns.Length == 0)
                throw new ArgumentException("At least one column is required.", nameof(columns));

            Name = name;
            Columns = columns;
        }
    }
}
