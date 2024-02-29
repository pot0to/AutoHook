using AutoHook.Resources.Localization;
using AutoHook.Utils;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ImGuiNET;
using System;

namespace AutoHook.Classes.AutoCasts;

public class AutoCastLine : BaseActionCast
{
    public bool OnlyCastWithFishEyes = false;

    public override bool RequiresTimeWindow() => true;
    
    public AutoCastLine() : base(UIStrings.AutoCastLine_Auto_Cast_Line, Data.IDs.Actions.Cast)
    {
        Priority = 1;
        Enabled = true;
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
    };
}