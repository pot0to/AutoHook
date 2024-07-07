using AutoHook.Data;
using AutoHook.Resources.Localization;
using AutoHook.Utils;
using ImGuiNET;
using System;
using System.Linq;
using AutoHook.Enums;
using ECommons.Throttlers;

namespace AutoHook.Classes.AutoCasts;

public class AutoLures : BaseActionCast
{
    public int LureStacks = 3;
    public bool CancelAttempt;

    public int TargetType;

    public AutoLures() : base(UIStrings.UseLures, IDs.Actions.AmbitiousLure)
    {
        HelpText = UIStrings.CancelsCurrentMooch;
    }

    public override string GetName()
        => Name = UIStrings.UseLures;

    private uint StatusId => Id == IDs.Actions.AmbitiousLure ? IDs.Status.AmbitiousLure : IDs.Status.ModestLure;
    public override bool CastCondition()
    {
        if (PlayerRes.GetStatusStacks(StatusId) >= LureStacks)
            return false;

        if (Service.EventFramework.FishingState != FishingState.Fishing)
            return false;

        return true;
    }

    protected override DrawOptionsDelegate DrawOptions => () =>
    {
        DrawUtil.TextV(UIStrings.LureType);
        ImGui.SameLine();
        if (ImGui.RadioButton(UIStrings.AmbitiousLure, Id == IDs.Actions.AmbitiousLure))
        {
            Id = IDs.Actions.AmbitiousLure;
            Service.Save();
        }

        ImGui.SameLine();
        if (ImGui.RadioButton(UIStrings.ModestLure, Id == IDs.Actions.ModestLure))
        {
            Id = IDs.Actions.ModestLure;
            Service.Save();
        }

        var stack = LureStacks;
        
        DrawUtil.TextV(UIStrings.AutoLures_Target_Fish);
        ImGui.SameLine();
        if (ImGui.RadioButton(UIStrings.AnyTarget, TargetType == 0))
        {
            TargetType = 0;
            Service.Save();
        }

        ImGui.SameLine();
        if (ImGui.RadioButton(UIStrings.OnlySpecial, TargetType == 1))
        {
            TargetType = 1;
            Service.Save();
        }
        
        ImGui.SameLine();
        DrawUtil.Info($"{UIStrings.SpecialFishExemple} {GameRes.LureFishes.FirstOrDefault()?.Name}");
        
        if (DrawUtil.EditNumberField(UIStrings.MaxAttempts, ref stack, "", 1))
        {
            // value has to be between 3 and 10
            LureStacks = Math.Max(1, Math.Min(stack, 3));
            Service.Save();
        }
        
        DrawUtil.Checkbox(UIStrings.CancelAttempt, ref CancelAttempt);
    };

    public void TryCasting(bool lureSuccess)
    {
        if (!EzThrottler.Check("CastingLure"))
            return;
        
        if (PlayerRes.GetStatusStacks(StatusId) >= LureStacks && CancelAttempt && !lureSuccess)
        {
            PlayerRes.CastActionDelayed(IDs.Actions.Rest);
            return;
        }
        
        if (!IsAvailableToCast())
            return;
        
        if (lureSuccess)
            return;
        
        PlayerRes.CastActionDelayed(Id);
        EzThrottler.Throttle("CastingLure", 2500);
    }

    public override int Priority { get; set; } = 0;
    public override bool IsExcludedPriority { get; set; } = true;
}