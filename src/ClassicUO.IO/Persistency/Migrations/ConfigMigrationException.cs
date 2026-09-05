#nullable enable

using System;

namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>
/// Raised when a config document could not be brought to its expected shape.
/// <para>
/// A caller with somewhere else to look - an older copy, a backup - wants
/// <see cref="ConfigDocumentMalformedException" /> apart from this: only the text being unreadable
/// makes another copy of the same file worth trying.
/// </para>
/// </summary>
public class ConfigMigrationException : Exception
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

/// <summary>
/// Raised when the persisted text is not a document of its format at all - truncated, hand-mangled,
/// or written by something else entirely. Distinct from its base because nothing about the shape was
/// established: a backup of the same file may still be readable, where a document this build cannot
/// migrate is one no older copy can answer either.
/// </summary>
public sealed class ConfigDocumentMalformedException : ConfigMigrationException
{
    /// <summary>Creates the exception with no underlying failure to carry.</summary>
    /// <param name="message">What about the text made it unparseable.</param>
    public ConfigDocumentMalformedException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception over the parse failure that produced it.</summary>
    /// <param name="message">What about the text made it unparseable.</param>
    /// <param name="inner">The parse failure this restates.</param>
    public ConfigDocumentMalformedException(string message, Exception inner) : base(message, inner)
    {
    }
}
