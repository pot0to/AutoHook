using System.ComponentModel;
using AutoHook.Resources.Localization;
using AutoHook.Utils;


namespace AutoHook.Classes.AutoCasts;

public class AutoCastLine : BaseActionCast
{
    public bool OnlyCastWithFishEyes = false;

    [DefaultValue(true)] public bool IgnoreMooch = true;

    public override bool DoesCancelMooch() => !IgnoreMooch;

    public override bool RequiresTimeWindow() => true;


    public AutoCastLine() : base(UIStrings.AutoCastLine_Auto_Cast_Line, Data.IDs.Actions.Cast)
    {
        Priority = 1;
    }

    public override int Priority { get; set; } = 0;

    public override bool IsExcludedPriority { get; set; } = true;

    public override bool CastCondition()
    {
        if (OnlyCastWithFishEyes && !PlayerRes.HasStatus(Data.IDs.Status.FishEyes))
            return false;

        return true;
    }

    public override string GetName()
        => Name = UIStrings.AutoCastLine_Auto_Cast_Line;

    protected override DrawOptionsDelegate DrawOptions => () =>
    {
        DrawUtil.Checkbox(UIStrings.AutoCastOnlyUnderFishEyes, ref OnlyCastWithFishEyes,
            UIStrings.AutoCastOnlyUnderFishEyesHelpText);

        DrawUtil.Checkbox(UIStrings.IgnoreMooch, ref IgnoreMooch,
            UIStrings.IgnoreMoochHelpText);
    };
}