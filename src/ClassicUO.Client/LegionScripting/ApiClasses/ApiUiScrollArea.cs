using ClassicUO.Game.UI.Controls;
using ClassicUO.LegionScripting.ApiClasses;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Inherits from ApiUiBaseControl
/// </summary>
/// <param name="scrollArea"></param>
public class ApiUiScrollArea(ScrollArea scrollArea) : ApiUiBaseControl(scrollArea) { }
