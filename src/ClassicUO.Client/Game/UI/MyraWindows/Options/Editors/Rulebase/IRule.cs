#nullable enable

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

public interface IRule
{
    uint Order { get; set; }
    bool Enabled { get; set; }
    bool CanEdit { get; set; }
    bool CanDelete { get; set; }

    /// <summary>
    /// Invoked Serves as a 'OnRuleDeleted' handler for implementors
    /// </summary>
    /// <param name="rule"></param>
    static virtual void DeleteRule(IRule rule)
    {
    }
}
