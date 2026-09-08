#nullable enable

using System;

namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>
/// Raised when a config document could not be brought to its expected shape - the document parsed,
/// carried a version this build knows how to start from, and a migration over it still failed. The
/// content is therefore suspect, so a caller should treat it as damaged.
/// <para>
/// Two derived types mean something else entirely and a caller wants them apart from this:
/// <see cref="ConfigDocumentMalformedException" /> establishes nothing about the shape, and
/// <see cref="ConfigVersionAheadException" /> says the document is intact but too new to read.
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

/// <summary>
/// Raised when the document carries a version above the highest this build knows. Nothing is wrong
/// with it - it was written by a newer build and will be read again by one. Distinct from its base
/// because the two call for opposite handling: a document that defeated a migration is damaged and
/// should be replaced, while this one is the only copy of settings a newer build still reads and
/// must survive the older build that met it.
/// </summary>
public sealed class ConfigVersionAheadException : ConfigMigrationException
{
    /// <summary>The version the document is at.</summary>
    public int DocumentVersion { get; }

    /// <summary>The highest version this build can migrate to.</summary>
    public int LatestKnownVersion { get; }

    /// <param name="documentVersion">The version the document is at.</param>
    /// <param name="latestKnownVersion">The highest version this build can migrate to.</param>
    public ConfigVersionAheadException(int documentVersion, int latestKnownVersion)
        : base($"Document is at version {documentVersion}, ahead of this build's latest known version {latestKnownVersion}.")
    {
        DocumentVersion = documentVersion;
        LatestKnownVersion = latestKnownVersion;
    }
}
