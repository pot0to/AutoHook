using AutoHook.Resources.Localization;

namespace AutoHook.Classes.AutoCasts;

public class AutoCastLine : BaseActionCast
{
    public AutoCastLine() : base(UIStrings.AutoCastLine_Auto_Cast_Line, Data.IDs.Actions.Cast)
    {
        Priority = 1;
    }

    public override int Priority { get; set; } = 0;
    public override bool IsExcludedPriority { get; set; } = true;

    public override bool CastCondition()
    {
        return true;
    }

    public override string GetName()
        => Name = UIStrings.AutoCastLine_Auto_Cast_Line;
}