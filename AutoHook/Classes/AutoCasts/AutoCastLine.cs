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
    public bool OnlyCastDuringSpecificTime = false;
    public TimeOnly StartTime = new(0);
    public TimeOnly EndTime = new(0);

    public AutoCastLine() : base(UIStrings.AutoCastLine_Auto_Cast_Line, Data.IDs.Actions.Cast)
    {
        Priority = 1;
    }

    public override int Priority { get; set; } = 0;
    public override bool IsExcludedPriority { get; set; } = true;

    public override bool CastCondition()
    {
        if (OnlyCastWithFishEyes)
        {
            return OnlyCastDuringSpecificTime && InsideCastWindow() || PlayerResources.HasStatus(Data.IDs.Status.FishEyes);
        }
        else if (OnlyCastDuringSpecificTime)
        {
            return InsideCastWindow();
        }
        else
        {
            return true;
        }
    }

    public override string GetName()
        => Name = UIStrings.AutoCastLine_Auto_Cast_Line;

    protected override DrawOptionsDelegate DrawOptions => () =>
    {
        DrawUtil.Checkbox(UIStrings.AutoCastOnlyUnderFishEyes, ref OnlyCastWithFishEyes);
        DrawUtil.Checkbox(UIStrings.AutoCastOnlyAtSpecificTimes, ref OnlyCastDuringSpecificTime);

        if (OnlyCastDuringSpecificTime)
        {
            var startTime = StartTime.ToString("HH:mm");
            var endTime = EndTime.ToString("HH:mm");

            ImGui.PushItemWidth(40 * ImGuiHelpers.GlobalScale);
            var startTimeGui = ImGui.InputText($"{UIStrings.AutoCastStartTime}", ref startTime, 5, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.PopItemWidth();
            if (startTimeGui && TimeOnly.TryParse(startTime, out var newStartTime))
            {
                StartTime = newStartTime;
                Service.Save();
            }

            ImGui.PushItemWidth(40 * ImGuiHelpers.GlobalScale);
            var endTimeGui = ImGui.InputText($"{UIStrings.AutoCastEndTime}", ref endTime, 5, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.PopItemWidth();
            if (endTimeGui && TimeOnly.TryParse(startTime, out var newEndTime))
            {
                EndTime = newEndTime;
                Service.Save();
            }
        }
    };

    private unsafe bool InsideCastWindow()
    {
        var clientTime = Framework.Instance()->ClientTime.EorzeaTime;
        var eorzeaTime = TimeOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(clientTime).DateTime);
        return eorzeaTime.IsBetween(StartTime, EndTime);
    }
}
