#nullable enable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Base for the script-visible value types. Gives each one a readable text form listing its
/// properties, and routes Python's <c>repr</c> to it. Override <see cref="ToString"/> for a tighter
/// form.
/// <para>
/// A class rather than an interface because IronPython resolves members off the concrete type, where
/// a default interface implementation is not found. Value types therefore cannot use this.
/// </para>
/// </summary>
public abstract class PyApiObject
{
    #region Private members

    private const int MAX_DEPTH = 4;

    private const int MAX_SEQUENCE_ITEMS = 8;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _printable = new();

    #endregion

    #region Public methods

    /// <summary>The object's property values. Nested API objects and sequences expand in place.</summary>
    /// <returns>The text form of this object.</returns>
    public override string ToString()
    {
        var text = new StringBuilder();

        Describe(this, text, 0);

        return text.ToString();
    }

    #region Python Emulation

    /// <summary>The object's string representation.</summary>
    /// <remarks>Serves Python usages like <c>repr(obj)</c>, including how a container formats this
    /// object as one of its elements.</remarks>
    /// <returns>The same text as <see cref="ToString"/>.</returns>
    public virtual string __repr__() => ToString();

    #endregion

    #endregion

    #region Private methods

    /// <summary>
    /// Appends one object's text form. Carries the depth rather than re-entering
    /// <see cref="ToString"/>, which would restart the budget.
    /// </summary>
    /// <param name="value">The object to describe.</param>
    /// <param name="text">The buffer to append to.</param>
    /// <param name="depth">How far into the tree this object sits.</param>
    private static void Describe(PyApiObject value, StringBuilder text, int depth)
    {
        Type type = value.GetType();

        text.Append(type.Name).Append('(');

        PropertyInfo[] properties = _printable.GetOrAdd(
            type,
            static forType => forType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .ToArray()
        );

        for (int index = 0; index < properties.Length; index++)
        {
            if (index > 0)
                text.Append(", ");

            text.Append(properties[index].Name).Append('=');

            // Appended here rather than as a value, so the marker is not quoted like a string.
            if (TryRead(properties[index], value, out object? read))
                AppendValue(read, text, depth);
            else
                text.Append("<unreadable>");
        }

        text.Append(')');
    }

    /// <summary>
    /// Reads a property for printing. A faulting getter must not take the whole text form with it -
    /// this exists to help debug a half-built object, which is when one is most likely to throw.
    /// </summary>
    /// <param name="property">The property to read.</param>
    /// <param name="owner">The object to read it from.</param>
    /// <param name="value">The value read, or null when the getter failed.</param>
    /// <returns>Whether the getter returned.</returns>
    private static bool TryRead(PropertyInfo property, object owner, out object? value)
    {
        try
        {
            value = property.GetValue(owner);

            return true;
        }
        catch (Exception)
        {
            value = null;

            return false;
        }
    }

    /// <summary>Appends one property value, expanding nested API objects and sequences.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="text">The buffer to append to.</param>
    /// <param name="depth">Nesting depth of the object this value belongs to.</param>
    private static void AppendValue(object? value, StringBuilder text, int depth)
    {
        switch (value)
        {
            case null:
                text.Append("None");

                return;

            case string plain:
                text.Append('\'').Append(plain).Append('\'');

                return;

            case bool flag:
                text.Append(flag ? "True" : "False");

                return;

            // These knobs are floats behind the double a script sets, so the exact form prints
            // digits nobody typed. Invariant, since a comma-decimal locale would put separators
            // inside what reads as Python output.
            case double number:
                text.Append(number.ToString("G6", CultureInfo.InvariantCulture));

                return;

            case float number:
                text.Append(number.ToString("G6", CultureInfo.InvariantCulture));

                return;

            case PyApiObject nested when depth + 1 < MAX_DEPTH:
                Describe(nested, text, depth + 1);

                return;

            case PyApiObject nested:
                text.Append(nested.GetType().Name).Append("(...)");

                return;

            case IEnumerable sequence when depth + 1 < MAX_DEPTH:
                AppendSequence(sequence, text, depth + 1);

                return;

            case IEnumerable:
                text.Append("[...]");

                return;

            default:
                text.Append(value);

                return;
        }
    }

    /// <summary>Appends a sequence, elided past <see cref="MAX_SEQUENCE_ITEMS"/> elements.</summary>
    /// <param name="sequence">The sequence to append.</param>
    /// <param name="text">The buffer to append to.</param>
    /// <param name="depth">Nesting depth of the sequence itself.</param>
    private static void AppendSequence(IEnumerable sequence, StringBuilder text, int depth)
    {
        text.Append('[');

        int listed = 0;

        foreach (object? element in sequence)
        {
            if (listed == MAX_SEQUENCE_ITEMS)
            {
                text.Append(", ...");

                break;
            }

            if (listed > 0)
                text.Append(", ");

            AppendValue(element, text, depth);
            listed++;
        }

        text.Append(']');
    }

    #endregion
}
