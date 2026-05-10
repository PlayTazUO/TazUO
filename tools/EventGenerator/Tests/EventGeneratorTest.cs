// A ready-made generator test.
// Does not work with netstandard2.0/1 - for more info, see the csproj file.
/*
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace EventGenerator.Tests;

[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers")]
public class EventGeneratorTest
{
    [Fact]
    public void TestEventGenerator()
    {
        var generator = new EventSourceGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);

        var pathToApiEventsFromRoot = Path.Combine("src", "ClassicUO.Client", "Game", "Managers", "EventSink.cs");

        var eventsFilePath = Path.GetFullPath(
            Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "..", pathToApiEventsFromRoot)
        );
        var eventsCode = File.ReadAllText(eventsFilePath);
        var compilation = CSharpCompilation.Create(
            nameof(EventGeneratorTest),
            [CSharpSyntaxTree.ParseText(eventsCode)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]
        );

        var runResult = driver.RunGenerators(compilation).GetRunResult();
    }
}
*/