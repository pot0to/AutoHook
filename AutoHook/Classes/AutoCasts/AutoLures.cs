using AutoHook.Data;
using AutoHook.Resources.Localization;
using AutoHook.Utils;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using System;
using AutoHook.Enums;

namespace AutoHook.Classes.AutoCasts;

public class AutoLures: BaseActionCast
{
    public int LureStacks = 3;
    
    public override bool RequiresTimeWindow() => true;

    public override bool DoesCancelMooch() => true;
    
    public AutoLures() : base(UIStrings.UseLures, IDs.Actions.AmbitiousLure)
    {
        HelpText = UIStrings.CancelsCurrentMooch;
    }

    public override string GetName()
        => Name = UIStrings.UseLures;

    public override bool CastCondition()
    {
        uint status;

        status = Id == IDs.Actions.AmbitiousLure ? IDs.Status.AmbitiousLure : IDs.Status.ModestLure;
        
        if (PlayerRes.GetStatusStacks(status) >= LureStacks)
            return false;
        
        if (Service.EventFramework.FishingState != FishingState.Fishing)
            return false;

        return true;
    }

    protected override DrawOptionsDelegate DrawOptions => () =>
    {
        if (ImGui.RadioButton(UIStrings.AmbitiousLure, Id == IDs.Actions.AmbitiousLure))
        {
            Id = IDs.Actions.AmbitiousLure;
            Service.Save();
        }

        if (ImGui.RadioButton(UIStrings.ModestLure, Id == IDs.Actions.ModestLure))
        {
            Id = IDs.Actions.ModestLure;
            Service.Save();
        }
        
        var stack = LureStacks;
        
        /*if (ImGui.InputInt(UIStrings.ChumTimeLimit, ref stack, 1, 1))
        {
           
        }*/
        if (DrawUtil.EditNumberField(UIStrings.LureStacks, ref stack, "", 1))
        {
            // value has to be between 3 and 10
            LureStacks = Math.Max(1, Math.Min(stack, 3));
            Service.Save();
        }
    };

    public override int Priority { get; set; } = 18;
    public override bool IsExcludedPriority { get; set; } = true;
}