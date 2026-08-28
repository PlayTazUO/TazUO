#nullable enable

using System;

namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>Raised when a config document could not be brought to its expected shape.</summary>
public sealed class ConfigMigrationException : Exception
{
    /// <summary>The version whose migration threw, when the failure happened mid-sequence.</summary>
    public int? FailedAtVersion { get; }

    public ConfigMigrationException(string message) : base(message)
    {
    }

    public ConfigMigrationException(string message, Exception inner) : base(message, inner)
    {
    }

    public ConfigMigrationException(string message, int failedAtVersion, Exception inner) : base(message, inner)
    {
        FailedAtVersion = failedAtVersion;
    }
}
