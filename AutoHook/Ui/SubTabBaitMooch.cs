using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AutoHook.Classes;
using AutoHook.Configurations;
using AutoHook.Resources.Localization;
using AutoHook.Utils;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;

namespace AutoHook.Ui;

public class SubTabBaitMooch
{
    public bool IsGlobal { get; set; }
    public bool IsMooch { get; set; }

    //private PresetConfig _selectedPreset;
    private List<HookConfig> _listOfHooks = new();

    public void DrawHookTab(PresetConfig presetCfg)
    {
        if (IsMooch)
            _listOfHooks = presetCfg.ListOfMooch;
        else
            _listOfHooks = presetCfg.ListOfBaits;

        if (!IsGlobal)
        {
            ImGui.Spacing();
            DrawDescription();
            ImGui.Separator();
        }

        ImGui.BeginGroup();

        for (int idx = 0; idx < _listOfHooks?.Count; idx++)
        {
            var hook = _listOfHooks[idx];
            ImGui.PushID(@$"id###{idx}");
            ImGui.Spacing();

            var count = HookingManager.FishingCounter.GetCount(hook.GetUniqueId());
            var hookCounter = count > 0 ? @$"({UIStrings.Hooked_Counter} {count})" : "";
            if (ImGui.CollapsingHeader(@$"{hook.BaitFish.Name} {hookCounter}###{idx}"))
            {
                ImGui.Spacing();
                DrawEnabledButtonCustomBait(hook);
                if (!IsGlobal)
                {
                    ImGui.SameLine();
                    DrawInputSearchBar(hook);
                    ImGui.SameLine();
                    DrawDeleteButton(hook);
                }

                ImGui.Spacing();
                if (ImGui.BeginTabBar(@"TabBarsBaitMooch", ImGuiTabBarFlags.NoTooltip))
                {
                    ImGui.Spacing();

                    if (ImGui.BeginTabItem($"Default###DefaultBait"))
                    {
                        
                        DrawNormalTab(hook);
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem($"Intuition"))
                    {
                        DrawIntuitionTab(hook);
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }

            ImGui.Spacing();
            ImGui.PopID();
        }

        ImGui.EndGroup();
    }

    private void DrawDescription()
    {
        if (ImGui.Button(UIStrings.Add))
        {
            if (_listOfHooks.All(x => x.BaitFish.Id != -1))
            {
                _listOfHooks.Add(new HookConfig(new BaitFishClass()));
                Service.Save();
            }
        }

        var bait = IsMooch ? UIStrings.Add_new_mooch : UIStrings.Add_new_bait;

        ImGui.SameLine();
        ImGui.Text($"{bait} ({_listOfHooks.Count})");
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(UIStrings.TabPresets_DrawHeader_CorrectlyEditTheBaitMoochName);
        ImGui.Spacing();
    }

    private void DrawDeleteButton(HookConfig hookConfig)
    {
        if (IsGlobal)
            return;

        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button(@$"{FontAwesomeIcon.Trash.ToIconChar()}", new Vector2(ImGui.GetFrameHeight(), 0)) &&
            ImGui.GetIO().KeyShift)
        {
            _listOfHooks.RemoveAll(x => x.BaitFish.Id == hookConfig.BaitFish.Id);
            Service.Save();
        }

        ImGui.PopFont();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(UIStrings.HoldShiftToDelete);
    }

    private void DrawInputSearchBar(HookConfig hookConfig)
    {
        var list = IsMooch ? PlayerResources.Fishes : PlayerResources.Baits;

        DrawUtil.DrawComboSelector(
            list,
            (BaitFishClass item) => item.Name,
            hookConfig.BaitFish.Name,
            (BaitFishClass item) => hookConfig.BaitFish = item);

        if (!IsMooch)
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(@$"{FontAwesomeIcon.ArrowLeft.ToIconChar()}", new Vector2(ImGui.GetFrameHeight(), 0)))
            {
                unsafe
                {
                    var p = PlayerState.Instance();
                    if (p != null && p->FishingBait > 0) // just make sure bait is bait
                    {
                        hookConfig.BaitFish = list.Single(x => x.Id == p->FishingBait);
                    }
                }
            }

            ImGui.PopFont();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(UIStrings.UIUseCurrentBait);
        }
    }

    private void DrawEnabledButtonCustomBait(HookConfig hookConfig)
    {
        if (DrawUtil.Checkbox(UIStrings.Enabled, ref hookConfig.Enabled, UIStrings.EnabledConfigArrowhelpMarker))
        {
            Service.Save();
        }
    }

    private void DrawNormalTab(HookConfig bait)
    {
        try
        {
            bait.NormalHook.DrawOptions();
        }
        catch (Exception e)
        {
            Service.PrintDebug(e.Message);
            throw;
        }
    }

    private void DrawIntuitionTab(HookConfig hook)
    {
        try
        {
           
            hook.IntuitionHook.DrawOptions();
            
        }
        catch (Exception e)
        {
            Service.PrintDebug(e.Message);
            throw;
        }
    }
}