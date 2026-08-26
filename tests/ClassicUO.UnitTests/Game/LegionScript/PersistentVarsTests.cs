using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClassicUO.LegionScripting;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.LegionScript;

/// <summary>
/// Exercises the <see cref="PersistentVars"/> save/read/delete API against a throwaway SQLite
/// database in a temp directory, so the tests never touch the game's real Data directory.
/// </summary>
public class PersistentVarsTests : IDisposable
{
    private readonly string _tempDir;

    public PersistentVarsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tazuo_pvars_test_" + Guid.NewGuid().ToString("N"));
        PersistentVars.ResetForTesting(_tempDir);
    }

    public void Dispose()
    {
        PersistentVars.ResetForTesting();

        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    [Fact]
    public async Task SaveVar_ThenGetVar_RoundTripsValue()
    {
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Char, "Kills", "1234");

        string value = await PersistentVars.GetVarAsync(LegionAPI.PersistentVar.Char, "Kills", "0");

        value.Should().Be("1234");
    }

    [Fact]
    public async Task GetVar_ReturnsDefault_WhenKeyMissing()
    {
        string value = await PersistentVars.GetVarAsync(LegionAPI.PersistentVar.Global, "missing", "fallback");

        value.Should().Be("fallback");
    }

    [Fact]
    public async Task SaveVar_Twice_OverwritesPreviousValue()
    {
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Global, "k", "first");
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Global, "k", "second");

        (await PersistentVars.GetVarAsync(LegionAPI.PersistentVar.Global, "k")).Should().Be("second");
        (await PersistentVars.GetAllVarsAsync(LegionAPI.PersistentVar.Global)).Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveVar_WithEmptyString_StoresItRatherThanDefault()
    {
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Global, "k", "");

        string value = await PersistentVars.GetVarAsync(LegionAPI.PersistentVar.Global, "k", "default");

        value.Should().Be("");
    }

    [Fact]
    public async Task DeleteVar_RemovesStoredValue()
    {
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Global, "k", "v");
        await PersistentVars.DeleteVarAsync(LegionAPI.PersistentVar.Global, "k");

        (await PersistentVars.GetVarAsync(LegionAPI.PersistentVar.Global, "k", "default")).Should().Be("default");
        (await PersistentVars.GetAllVarsAsync(LegionAPI.PersistentVar.Global)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllVars_ReturnsEverySavedVarForScope()
    {
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Global, "a", "1");
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Global, "b", "2");
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Char, "a", "char-a");

        Dictionary<string, string> global = await PersistentVars.GetAllVarsAsync(LegionAPI.PersistentVar.Global);
        global.Should().Equal(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" });

        Dictionary<string, string> charVars = await PersistentVars.GetAllVarsAsync(LegionAPI.PersistentVar.Char);
        charVars.Should().Equal(new Dictionary<string, string> { ["a"] = "char-a" });
    }

    [Fact]
    public async Task SameKey_DifferentScopes_DoNotCollide()
    {
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Global, "k", "global-value");
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Account, "k", "account-value");
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Server, "k", "server-value");
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Char, "k", "char-value");

        (await PersistentVars.GetVarAsync(LegionAPI.PersistentVar.Global, "k")).Should().Be("global-value");
        (await PersistentVars.GetVarAsync(LegionAPI.PersistentVar.Account, "k")).Should().Be("account-value");
        (await PersistentVars.GetVarAsync(LegionAPI.PersistentVar.Server, "k")).Should().Be("server-value");
        (await PersistentVars.GetVarAsync(LegionAPI.PersistentVar.Char, "k")).Should().Be("char-value");
    }

    [Fact]
    public async Task SavedVars_Persist_AcrossDatabaseReopen()
    {
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Global, "name", "legion");

        // Simulate a restart: drop the singleton so the next access opens the same file fresh.
        PersistentVars.ResetForTesting(_tempDir);

        string value = await PersistentVars.GetVarAsync(LegionAPI.PersistentVar.Global, "name", "default");

        value.Should().Be("legion");
    }

    [Fact]
    public async Task GetVar_SyncWrapper_ReadsBackSavedValue()
    {
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Global, "k", "v");

        string value = PersistentVars.GetVar(LegionAPI.PersistentVar.Global, "k", "default");

        value.Should().Be("v");
    }

    [Fact]
    public async Task WhenDatabaseCannotBeCreated_VarsDegradeToDefaults_WithoutThrowing()
    {
        string filePath = Path.Combine(_tempDir, "not_a_dir");
        Directory.CreateDirectory(_tempDir);
        await File.WriteAllTextAsync(filePath, "x");

        // Point the database at a path whose parent is a file, so creation must fail.
        PersistentVars.ResetForTesting(Path.Combine(filePath, "data"));

        (await PersistentVars.GetVarAsync(LegionAPI.PersistentVar.Global, "k", "default")).Should().Be("default");
        (await PersistentVars.GetAllVarsAsync(LegionAPI.PersistentVar.Global)).Should().BeEmpty();
        await PersistentVars.SaveVarAsync(LegionAPI.PersistentVar.Global, "k", "v");
        await PersistentVars.DeleteVarAsync(LegionAPI.PersistentVar.Global, "k");
    }
}
