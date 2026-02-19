using System;

namespace ClassicUO.Common;

/// <summary>
/// An attribute that marks an Event as exposable via the Legion API.
/// <para>
/// Exposed events will be added to the <see cref="LegionScripting.LegionAPI.Events"/> field as well as included in API documentation.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Event)]
public class ApiEventAttribute : Attribute
{
}
