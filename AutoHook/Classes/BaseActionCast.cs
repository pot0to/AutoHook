using AutoHook.Data;
using AutoHook.Resources.Localization;
using AutoHook.Utils;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AutoHook.Classes;

public abstract class BaseActionCast
{
    protected BaseActionCast(string name, uint id, ActionType actionType = ActionType.Action)
    {
        Name = name;
        Id = id;
        Enabled = false;

        ActionType = actionType;

        if (actionType == ActionType.Action && id != IDs.Actions.ThaliaksFavor)
            GpThreshold = (int)PlayerResources.CastActionCost(Id, ActionType);
    }

    [NonSerialized] public string Name;
    
    [NonSerialized] public string HelpText = @"";

    public bool Enabled;

    public uint Id { get; set; }

    public int GpThreshold;

    public bool GpThresholdAbove { get; set; } = true;

    public bool DoesCancelMooch { get; set; }

    public bool DontCancelMooch = true;

    public virtual bool RequiresAutoCastAvailable() => false;

    public virtual int Priority { get; set; }

    public ActionType ActionType { get; protected init; }

    public virtual void SetThreshold(int newCost)
    {
        var actionCost = Id == IDs.Actions.ThaliaksFavor ? 0 : (int)PlayerResources.CastActionCost(Id, ActionType);

        GpThreshold = (newCost < 0) ? 0 : Math.Max(newCost, actionCost);

        Service.Save();
    }

    public bool IsAvailableToCast()
    {
        if (!Enabled)
            return false;

        if (DoesCancelMooch && PlayerResources.IsMoochAvailable() && DontCancelMooch)
            return false;

        var condition = CastCondition();

        var currentGp = PlayerResources.GetCurrentGp();

        bool hasGp;

        if (GpThresholdAbove)
            hasGp = currentGp >= GpThreshold;
        else
            hasGp = currentGp <= GpThreshold;

        var actionAvailable = PlayerResources.ActionTypeAvailable(Id, ActionType);

        Service.PluginLog.Debug(@$"[BaseAction] {Name} - {hasGp} - {actionAvailable} - {condition}");
        return hasGp && actionAvailable && condition;
    }

    public abstract bool CastCondition();

    public virtual string GetName() => "";

    public virtual int GetPriority() => Priority;

    protected delegate void DrawOptionsDelegate();

    protected virtual DrawOptionsDelegate? DrawOptions => null;

    public abstract bool IsExcludedPriority { get; set; }
    public virtual void DrawConfig(List<BaseActionCast>? availableActs = null)
    {
        ImGui.PushID(@$"{GetName()}_cfg");

        ImGui.Columns(3, null, false);
        ImGui.SetColumnWidth(0, 200f);
        ImGui.SetColumnWidth(1, 40f);
        if (DrawOptions != null)
        {
            if (DrawUtil.Checkbox(@$"###{GetName()}", ref Enabled, HelpText, true))
            {
                Service.PrintDebug(@$"[BaseAction] {Name} - {(Enabled ? @"Enabled" : @"Disabled")}");
                Service.Save();
            }

            ImGui.SameLine();

            if (ImGui.TreeNodeEx(@$"{GetName()}", ImGuiTreeNodeFlags.FramePadding))
            {
                ImGui.SameLine();
                ImGui.NextColumn();
                DrawGpThreshold();
                DrawUpDownArrows(availableActs);
                ImGui.Columns(1);
                DrawOptions?.Invoke();
                ImGui.NextColumn();
                ImGui.NextColumn();
                ImGui.Separator();
                ImGui.TreePop();
            }
            else
            {
                ImGui.SameLine();
                ImGui.NextColumn();
                DrawGpThreshold();
                DrawUpDownArrows(availableActs);
            }
        }
        else
        {
            if (DrawUtil.Checkbox(@$"###{GetName()}", ref Enabled, HelpText, true))
                Service.Save();

            ImGui.SameLine();
            ImGui.Text(@$"{GetName()}");
            ImGui.NextColumn();
            DrawGpThreshold();
            DrawUpDownArrows(availableActs);
        }

        ImGui.Columns(1, null, false);
        ImGui.PopID();
    }

    private void DrawUpDownArrows(List<BaseActionCast>? availableActs)
    {
        if (availableActs is null || IsExcludedPriority) return;
        
        if (GetPriority() == 0) //failsafe I guess
        {
            Priority = availableActs.MaxBy(x => x.Priority)!.Priority + 1;
        }

        ImGui.NextColumn();

        ImGui.SameLine();

        if (!availableActs.Any(x => x.Priority < Priority && !x.IsExcludedPriority))
            ImGui.BeginDisabled();

        if (ImGui.ArrowButton(@"###UpArrow", ImGuiDir.Up))
        {
            if (availableActs.Any(x => x.Priority < Priority && !x.IsExcludedPriority))
            {
                var nextAct = availableActs.Where(x => x.Priority < Priority && !x.IsExcludedPriority).OrderByDescending(x => x.Priority).First();
                nextAct.Priority = this.Priority;
                this.Priority--;
            }
        }

        if (!availableActs.Any(x => x.Priority < Priority && !x.IsExcludedPriority))
            ImGui.EndDisabled();

        ImGui.SameLine();

        if (!availableActs.Any(x => x.Priority > Priority && !x.IsExcludedPriority))
            ImGui.BeginDisabled();

        if (ImGui.ArrowButton(@"###DownArrow", ImGuiDir.Down))
        {
            if (availableActs.Any(x => x.Priority > Priority && !x.IsExcludedPriority))
            {
                var lastAct = availableActs.Where(x => x.Priority > Priority && !x.IsExcludedPriority).OrderBy(x => x.Priority).First();
                lastAct.Priority = this.Priority;
                this.Priority++;
            }
        }

        if (!availableActs.Any(x => x.Priority > Priority && !x.IsExcludedPriority))
            ImGui.EndDisabled();
    }

    public virtual void DrawGpThreshold()
    {
        ImGui.PushID(@$"{GetName()}_gp");
        if (ImGui.Button(@"GP"))
        {
            ImGui.OpenPopup(str_id: @"gp_cfg");
        }

        if (ImGui.BeginPopup(@"gp_cfg"))
        {
            if (ImGui.BeginChild(@"gp_cfg2", new Vector2(175, 125), true))
            {
                if (ImGui.Button(@" X "))
                    ImGui.CloseCurrentPopup();
                ImGui.SameLine();
                ImGui.TextColored(ImGuiColors.DalamudYellow, @$"GP - {GetName()}");

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(
                        @$"{GetName()} {UIStrings.WillBeUsedWhenYourGPIsEqualOr} {(GpThresholdAbove ? UIStrings.Above : UIStrings.Below)} {GpThreshold}");

                ImGui.Separator();
                if (ImGui.RadioButton(UIStrings.Above, GpThresholdAbove))
                {
                    GpThresholdAbove = true;
                    Service.Save();
                }

                //ImGui.SameLine();

                if (ImGui.RadioButton(UIStrings.Below, GpThresholdAbove == false))
                {
                    GpThresholdAbove = false;
                    Service.Save();
                }

                //ImGui.SameLine();

                ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
                if (ImGui.InputInt(UIStrings.GP, ref GpThreshold, 1, 1))
                {
                    GpThreshold = Math.Max(GpThreshold, 0);
                    SetThreshold(GpThreshold);
                    Service.Save();
                }
                
                ImGui.EndChild();
            }

            ImGui.EndPopup();
        }

        ImGui.PopID();
    }
}