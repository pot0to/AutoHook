using AutoHook.Data;
using AutoHook.Resources.Localization;
using AutoHook.Utils;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AutoHook.Classes.AutoCasts;

public class AutoFishEyes : BaseActionCast
{
    public override int Priority { get; set; } = 6;
    public override bool IsExcludedPriority { get; set; } = false;
    public override bool RequiresTimeWindow() => true;

    public override bool DoesCancelMooch() => true;
    
    public AutoFishEyes() : base(UIStrings.Fish_Eyes, IDs.Actions.FishEyes, ActionType.Action)
    {
        HelpText = UIStrings.CancelsCurrentMooch;
    }

    public override string GetName()
        => Name = UIStrings.Fish_Eyes;

    public override bool CastCondition()
    {
        if (PlayerRes.HasStatus(IDs.Status.FishEyes))
            return false;
        
        return true;
    }

    /*protected override DrawOptionsDelegate DrawOptions => () =>
    {

    };*/
}