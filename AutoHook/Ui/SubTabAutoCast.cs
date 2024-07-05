using System;
using System.Collections.Generic;
using System.Linq;
using AutoHook.Classes;
using AutoHook.Classes.AutoCasts;
using AutoHook.Configurations;
using AutoHook.Resources.Localization;
using AutoHook.Utils;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;

namespace AutoHook.Ui;

public class SubTabAutoCast
{
    public bool IsGlobalPreset { get; set; }

    private List<BaseActionCast> actionsAvailable = new();

    public void DrawAutoCastTab(AutoCastsConfig acCfg)
    {
        actionsAvailable = new()
        {
            acCfg.CastLine,
            acCfg.CastMooch,
            acCfg.CastChum,
            acCfg.CastCollect,
            acCfg.CastCordial,
            acCfg.CastFishEyes,
            acCfg.CastMakeShiftBait,
            acCfg.CastPatience,
            acCfg.CastPrizeCatch,
            acCfg.CastThaliaksFavor,
            acCfg.CastBigGame
        };

        DrawHeader(acCfg);
        DrawBody(acCfg);
    }

    private void DrawHeader(AutoCastsConfig acCfg)
    {
        ImGui.Spacing();

        if (DrawUtil.Checkbox(UIStrings.EnableActions, ref acCfg.EnableAll, UIStrings.Acton_Alert_Manual_Hook))
        {
            if (acCfg.EnableAll)
            {
                if (IsGlobalPreset &&
                    (Service.Configuration.HookPresets.SelectedPreset?.AutoCastsCfg.EnableAll ?? false))
                {
                    Service.Configuration.HookPresets.SelectedPreset.AutoCastsCfg.EnableAll = false;
                }
                else if (!IsGlobalPreset)
                {
                    Service.Configuration.HookPresets.DefaultPreset.AutoCastsCfg.EnableAll = false;
                }
            }

            Service.Save();
        }

        ImGui.SameLine();

        if (DrawUtil.Checkbox(UIStrings.Dont_Cancel_Mooch, ref acCfg.DontCancelMooch,
                UIStrings.TabAutoCasts_DrawHeader_HelpText))
        {
            foreach (var action in actionsAvailable.Where(action => action != null))
            {
                action.DontCancelMooch = acCfg.DontCancelMooch;

                Service.PrintDebug($"{action.Name} DontCancelMooch: {action.DontCancelMooch}");
            }

            Service.Save();
        }

        if (!IsGlobalPreset)
        {
            if (Service.Configuration.HookPresets.DefaultPreset.AutoCastsCfg.EnableAll && !acCfg.EnableAll)
                ImGui.TextColored(ImGuiColors.DalamudViolet, UIStrings.GlobalActionsBeingUsed);
            else if (!acCfg.EnableAll)
                ImGui.TextColored(ImGuiColors.ParsedBlue, UIStrings.AllActionsDisabled);
        }
        else
        {
            if (Service.Configuration.HookPresets.SelectedPreset?.AutoCastsCfg.EnableAll ?? false)
                ImGui.TextColored(ImGuiColors.DalamudViolet,
                    string.Format(UIStrings.Custom_AutoCast_Being_Used,
                        Service.Configuration.HookPresets.SelectedPreset.PresetName));
            else if (!acCfg.EnableAll)
                ImGui.TextColored(ImGuiColors.ParsedBlue, UIStrings.SubAuto_Disabled);
        }

        DrawUtil.SpacingSeparator();
    }

    private void DrawBody(AutoCastsConfig acCfg)
    {
        if (!acCfg.EnableAll && !Service.Configuration.DontHideOptionsDisabled)
            return;

        DrawUtil.DrawCheckboxTree(UIStrings.AutoCastOnlyAtSpecificTimes, ref acCfg.OnlyCastDuringSpecificTime, () =>
        {
            var startTime = acCfg.StartTime.ToString(@"HH:mm");
            var endTime = acCfg.EndTime.ToString(@"HH:mm");

            ImGui.PushItemWidth(40 * ImGuiHelpers.GlobalScale);
            var startTimeGui = ImGui.InputText(@$"{UIStrings.AutoCastStartTime}", ref startTime, 5,
                ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.PopItemWidth();
            if (startTimeGui && TimeOnly.TryParse(startTime, out var newStartTime))
            {
                acCfg.StartTime = newStartTime;
                Service.Save();
            }

            ImGui.PushItemWidth(40 * ImGuiHelpers.GlobalScale);
            var endTimeGui = ImGui.InputText(@$"{UIStrings.AutoCastEndTime}", ref endTime, 5,
                ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.PopItemWidth();
            if (endTimeGui && TimeOnly.TryParse(endTime, out var newEndTime))
            {
                acCfg.EndTime = newEndTime;
                Service.Save();
            }
        }, UIStrings.SpecificTimeWindowHelpText);

        ImGui.TextColored(ImGuiColors.DalamudOrange, UIStrings.Auto_Cast_Sort_Notice);

        if (ImGui.BeginChild("AutoCastItems", new Vector2(0, 0), true))
        {
            foreach (var action in actionsAvailable.OrderBy(x => x.GetType() == typeof(AutoCastLine))
                         .ThenBy(x => x.GetType() == typeof(AutoMooch)).ThenBy(x => x.GetType() == typeof(AutoCollect))
                         .ThenBy(x => x.Priority))
            {
                try
                {
                    ImGui.PushID(action.GetType().ToString());
                    action.DrawConfig(actionsAvailable);
                    ImGui.PopID();
                }
                catch (Exception e)
                {
                    Service.PluginLog.Error(e.ToString());
                }
            }

            ImGui.EndChild();
        }
    }
}