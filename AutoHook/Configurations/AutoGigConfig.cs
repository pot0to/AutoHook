using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using AutoHook.Classes;
using AutoHook.Classes.AutoCasts;
using AutoHook.Interfaces;
using AutoHook.Resources.Localization;
using AutoHook.Utils;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace AutoHook.Configurations;

public class AutoGigConfig : IPresetConfig
{
    public bool AutoGigEnabled = false;
    public bool AutoGigHideOverlay = false;
    public bool AutoGigDrawFishHitbox = false;
    
    [DefaultValue(true)]
    public bool AutoGigDrawGigHitbox = true;

    //public AutoCordial Cordial = new(true);
    public AutoThaliaksFavor ThaliaksFavor = new(true);
    
    public List<AutoGigPreset> Presets = new();

    public string SelectedGuid { get; set; } = "";

    public AutoGigPreset? GetSelectedPreset() => Presets.Find(p => p.UniqueId.ToString() == SelectedGuid);

    public Guid UniqueId { get; } = Guid.NewGuid();

    public void AddPresetItem(string presetName)
    {
        var newPreset = new AutoGigPreset(presetName);
        Presets.Add(newPreset);
        SelectedGuid = newPreset.UniqueId.ToString();
        Service.Save();
    }

    public void RemovePresetItem(Guid value)
    {
        Presets.RemoveAll(p => p.UniqueId == value);
        Service.Save();
    }

    public void RenamePreset(Guid value, string newName)
    {
        var preset = Presets.Find(p => p.UniqueId == value);
        if (preset != null)
        {
            preset.Name = newName;
        }

        Service.Save();
    }

    public void SetSelectedPreset(Guid value)
    {
        SelectedGuid = value.ToString();
        Service.Save();
    }

    public IPresetItem? GetISelectedPreset()
    {
        return GetSelectedPreset();
    }

    public List<IPresetItem> GetIPresets()
    {
        return Presets.Cast<IPresetItem>().ToList();
    }
}

public class AutoGigPreset : IPresetItem
{
    public string Name { get; set; }

    public List<BaseGig> Gigs { get; set; } = new();
    
    public int HitboxSize = 25;

    public Guid UniqueId { get; set; } = Guid.NewGuid();

    public AutoGigPreset(string presetName)
    {
        Name = presetName;
    }

    public List<BaseGig> GetGigCurrentNode(int node) =>
        Gigs.Where(f => f.Fish != null && f.Fish.Nodes.Contains(node)).ToList();

    public void Rename(string newName)
    {
        Name = newName;
    }

    public void AddItem(IBaseOption item)
    {
        Gigs.Add((BaseGig)item);
    }

    public void RemoveItem(Guid value)
    {
        Gigs.RemoveAll(x => x.UniqueId == value);
    }

    public void DrawOptions()
    {
        if (Gigs == null || Gigs.Count == 0)
            return;

        foreach (var gig in Gigs)
        {
            ImGui.PushID(gig.UniqueId.ToString());
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button(@$"{FontAwesomeIcon.Trash.ToIconChar()}", new Vector2(ImGui.GetFrameHeight(), 0)) &&
                    ImGui.GetIO().KeyShift)
                {
                    RemoveItem(gig.UniqueId);
                    Service.Save();
                    return;
                }
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(UIStrings.HoldShiftToDelete);

            ImGui.SameLine();
            DrawUtil.DrawCheckboxTree($"{gig.Fish?.Name ?? UIStrings.None}", ref gig.Enabled,
                () => { gig.DrawOptions(); });

            ImGui.PopID();
        }
    }
}