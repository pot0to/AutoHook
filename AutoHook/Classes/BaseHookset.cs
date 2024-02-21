using System;
using AutoHook.Enums;
using AutoHook.Resources.Localization;
using AutoHook.Utils;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using ImGuiNET;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace AutoHook.Classes;

public class BaseHookset
{
    // for future use, maybe we need a hooking condition under a different status?
    public uint RequiredStatus;

    private Guid _uniqueId;

    // Patience > Normal, Precision and Powerful
    public BaseBiteConfig PatienceWeak = new(HookType.Precision);
    public BaseBiteConfig PatienceStrong = new(HookType.Powerful);
    public BaseBiteConfig PatienceLegendary = new(HookType.Powerful);

    // Double Hook
    public bool UseDoubleHook;
    public bool LetFishEscapeDoubleHook;
    public BaseBiteConfig DoubleWeak = new(HookType.Double);
    public BaseBiteConfig DoubleStrong = new(HookType.Double);
    public BaseBiteConfig DoubleLegendary = new(HookType.Double);

    // Triple Hook
    public bool UseTripleHook;
    public bool LetFishEscapeTripleHook;
    public BaseBiteConfig TripleWeak = new(HookType.Triple);
    public BaseBiteConfig TripleStrong = new(HookType.Triple);
    public BaseBiteConfig TripleLegendary = new(HookType.Triple);


    // Timeout
    //public double TimeoutMin = 0;
    public double TimeoutMax;

    // Stop condition
    public bool StopAfterCaught;
    public bool StopAfterResetCount;
    public int StopAfterCaughtLimit = 1;

    public FishingSteps StopFishingStep = FishingSteps.None;

    public Guid GetUniqueId()
    {
        if (_uniqueId == Guid.Empty)
            _uniqueId = Guid.NewGuid();

        return _uniqueId;
    }

    public BaseHookset(uint requiredStatus)
    {
        this.RequiredStatus = requiredStatus;
    }


    public void DrawOptions()
    {
        ImGui.PushID(@"BaseHookset");

        ImGui.Spacing();
        DrawPatience();
        DrawUtil.SpacingSeparator();
        DrawDoubleHook();
        DrawTripleHook();
        DrawUtil.SpacingSeparator();
        DrawTimeout();
        DrawUtil.SpacingSeparator();
        DrawStopCondition();

        ImGui.PopID();
    }

    private void DrawPatience()
    {
        PatienceWeak.DrawOptions(UIStrings.HookWeakExclamation, true);
        PatienceStrong.DrawOptions(UIStrings.HookStrongExclamation, true);
        PatienceLegendary.DrawOptions(UIStrings.HookLegendaryExclamation, true);
    }

    private void DrawDoubleHook()
    {
        DrawUtil.DrawCheckboxTree(UIStrings.UseDoubleHook, ref UseDoubleHook,
            () =>
            {
                DrawUtil.Checkbox(UIStrings.LetTheFishEscape, ref LetFishEscapeDoubleHook);
                DoubleWeak.DrawOptions(UIStrings.HookWeakExclamation);
                DoubleStrong.DrawOptions(UIStrings.HookStrongExclamation);
                DoubleLegendary.DrawOptions(UIStrings.HookLegendaryExclamation);
            });
    }

    private void DrawTripleHook()
    {
        DrawUtil.DrawCheckboxTree(UIStrings.UseTripleHook, ref UseTripleHook,
            () =>
            {
                DrawUtil.Checkbox(UIStrings.LetTheFishEscape, ref LetFishEscapeTripleHook);
                TripleWeak.DrawOptions(UIStrings.HookWeakExclamation);
                TripleStrong.DrawOptions(UIStrings.HookStrongExclamation);
                TripleLegendary.DrawOptions(UIStrings.HookLegendaryExclamation);
            });
    }

    private void DrawTimeout()
    {
        if (ImGui.TreeNodeEx(UIStrings.Timeout, ImGuiTreeNodeFlags.FramePadding))
        {
            ImGui.TextWrapped(UIStrings.TimeoutHelpText);
            ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputDouble(UIStrings.MaxWait, ref TimeoutMax, .1, 1, @"%.1f%"))
            {
                switch (TimeoutMax)
                {
                    case 0.1:
                        TimeoutMax = 2;
                        break;
                    case <= 0:
                    case <= 1.9: //This makes the option turn off if delay = 2 seconds when clicking the minus.
                        TimeoutMax = 0;
                        break;
                    case > 99:
                        TimeoutMax = 99;
                        break;
                }

                Service.Save();
            }

            ImGui.SameLine();
            ImGuiComponents.HelpMarker(UIStrings.HelpMarkerMaxWaitTimer);
            ImGui.TreePop();
        }
    }

    private void DrawStopCondition()
    {
        DrawUtil.DrawCheckboxTree(UIStrings.StopAfterHooking, ref StopAfterCaught,
            () =>
            {
                ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
                if (ImGui.InputInt(UIStrings.TimeS, ref StopAfterCaughtLimit))
                {
                    if (StopAfterCaughtLimit < 1)
                        StopAfterCaughtLimit = 1;
                    Service.Save();
                }

                ImGui.Spacing();
                if (ImGui.RadioButton(UIStrings.Stop_Casting, StopFishingStep == FishingSteps.None))
                {
                    StopFishingStep = FishingSteps.None;
                    Service.Save();
                }

                ImGui.SameLine();
                ImGuiComponents.HelpMarker(UIStrings.Auto_Cast_Stopped);

                if (ImGui.RadioButton(UIStrings.Quit_Fishing, StopFishingStep == FishingSteps.Quitting))
                {
                    StopFishingStep = FishingSteps.Quitting;
                    Service.Save();
                }

                DrawUtil.Checkbox(UIStrings.Reset_the_counter, ref StopAfterResetCount);
            });
    }
}