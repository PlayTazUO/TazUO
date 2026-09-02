using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>Guards that the migrations namespace stays frozen against
/// <c>Configuration.FeatureConfigs</c>: a migration naming a live model type breaks when that type is
/// renamed.</summary>
public class ScreenDecorationsMigrationsNamespaceTests
{
    private const string MigrationsNamespace = "ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations";
    private const string ModelNamespacePrefix = "ClassicUO.Configuration.FeatureConfigs";

    [Fact]
    public void Migrations_Namespace_Holds_No_Reference_Into_FeatureConfigs_Model_Types()
    {
        Assembly assembly = typeof(ScreenDecorationsMigrations).Assembly;

        Type[] migrationTypes = assembly.GetTypes()
            .Where(t => t.Namespace == MigrationsNamespace)
            .ToArray();

        Assert.NotEmpty(migrationTypes);

        var offenders = new List<string>();

        foreach (Type type in migrationTypes)
        {
            foreach (Type referenced in ReferencedTypes(type))
            {
                if (referenced.Namespace == null || referenced.Namespace == MigrationsNamespace)
                    continue;

                if (referenced.Namespace.StartsWith(ModelNamespacePrefix, StringComparison.Ordinal))
                    offenders.Add($"{type.FullName} -> {referenced.FullName}");
            }
        }

        Assert.True(offenders.Count == 0, "Migration types must not reference live model types:\n" + string.Join('\n', offenders));
    }

    private static IEnumerable<Type> ReferencedTypes(Type type)
    {
        if (type.BaseType != null)
            yield return type.BaseType;

        foreach (Type iface in type.GetInterfaces())
            yield return iface;

        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            yield return field.FieldType;

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            yield return property.PropertyType;

        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            yield return method.ReturnType;

            foreach (ParameterInfo parameter in method.GetParameters())
                yield return parameter.ParameterType;
        }
    }
}
