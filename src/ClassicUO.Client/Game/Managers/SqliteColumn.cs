namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// The SQLite storage class / affinity used when declaring a column.
    /// </summary>
    public enum SqliteColumnType
    {
        Integer,
        Text,
        Real,
        Blob
    }

    /// <summary>
    /// Describes a single column for use with <see cref="SqliteTableSchema"/>. Use the static factory
    /// helpers (<see cref="Int"/>, <see cref="Str"/>, <see cref="Number"/>, <see cref="Binary"/>) for
    /// terse declarations, e.g. <c>SqliteColumn.Int("id", primaryKey: true)</c>.
    /// </summary>
    public readonly struct SqliteColumn
    {
        /// <summary>The column name.</summary>
        public readonly string Name;

        /// <summary>The column storage type.</summary>
        public readonly SqliteColumnType Type;

        /// <summary>Whether this column is (part of) the table's PRIMARY KEY.</summary>
        public readonly bool PrimaryKey;

        /// <summary>Whether this column has a NOT NULL constraint.</summary>
        public readonly bool NotNull;

        /// <summary>
        /// Optional DEFAULT clause value, written verbatim into the DDL. For string defaults include
        /// the quotes yourself, e.g. <c>"''"</c>. Null/empty means no DEFAULT clause.
        /// </summary>
        public readonly string DefaultValue;

        public SqliteColumn(string name, SqliteColumnType type, bool primaryKey = false, bool notNull = false, string defaultValue = null)
        {
            Name = name;
            Type = type;
            PrimaryKey = primaryKey;
            NotNull = notNull;
            DefaultValue = defaultValue;
        }

        /// <summary>Creates an INTEGER column.</summary>
        public static SqliteColumn Int(string name, bool primaryKey = false, bool notNull = false, string def = null)
            => new(name, SqliteColumnType.Integer, primaryKey, notNull, def);

        /// <summary>Creates a TEXT column.</summary>
        public static SqliteColumn Str(string name, bool primaryKey = false, bool notNull = false, string def = null)
            => new(name, SqliteColumnType.Text, primaryKey, notNull, def);

        /// <summary>Creates a REAL (floating point) column.</summary>
        public static SqliteColumn Number(string name, bool primaryKey = false, bool notNull = false, string def = null)
            => new(name, SqliteColumnType.Real, primaryKey, notNull, def);

        /// <summary>Creates a BLOB column.</summary>
        public static SqliteColumn Binary(string name, bool primaryKey = false, bool notNull = false, string def = null)
            => new(name, SqliteColumnType.Blob, primaryKey, notNull, def);

        private string SqlType => Type switch
        {
            SqliteColumnType.Integer => "INTEGER",
            SqliteColumnType.Text => "TEXT",
            SqliteColumnType.Real => "REAL",
            SqliteColumnType.Blob => "BLOB",
            _ => "TEXT"
        };

        /// <summary>
        /// Builds the DDL fragment for this column, e.g. <c>"name" TEXT NOT NULL DEFAULT ''</c>.
        /// </summary>
        /// <param name="includePrimaryKey">
        /// When true, an inline <c>PRIMARY KEY</c> is appended for single-column primary keys.
        /// Set false when the primary key is declared separately as a table-level constraint, or when
        /// building an <c>ALTER TABLE ... ADD COLUMN</c> fragment (SQLite forbids a PRIMARY KEY there).
        /// </param>
        public string ToDefinition(bool includePrimaryKey = true)
        {
            System.Text.StringBuilder sb = new();
            // Quote the identifier so names that are keywords or contain unusual characters cannot
            // break out of the DDL (identifiers cannot be passed as SQL parameters).
            sb.Append('"');
            sb.Append(Name.Replace("\"", "\"\""));
            sb.Append('"');
            sb.Append(' ');
            sb.Append(SqlType);

            if (includePrimaryKey && PrimaryKey)
                sb.Append(" PRIMARY KEY");

            if (NotNull)
                sb.Append(" NOT NULL");

            if (!string.IsNullOrEmpty(DefaultValue))
            {
                sb.Append(" DEFAULT ");
                sb.Append(DefaultValue);
            }

            return sb.ToString();
        }
    }
}
