using System;
using AutoHook.Classes;
using AutoHook.Configurations;
using AutoHook.Resources.Localization;
using AutoHook.Utils;
using ImGuiNET;

namespace AutoHook.Ui;

internal class TabAutoGig : BaseTab
{
    public override string TabName => UIStrings.TabNameAutoGig;
    public override bool Enabled => true;

    private readonly AutoGigConfig _gigCfg = Service.Configuration.AutoGigConfig;

    public override void DrawHeader()
    {
        DrawTabDescription(UIStrings.TabAutoGigDescription);

        DrawUtil.DrawCheckboxTree(UIStrings.EnableAutoGig, ref _gigCfg.AutoGigEnabled, () =>
        {
            if (_gigCfg is { AutoGigEnabled: true, AutoGigHideOverlay: true })
                _gigCfg.AutoGigHideOverlay = false;

            if (DrawUtil.Checkbox(UIStrings.HideOverlayDuringSpearfishing, ref _gigCfg.AutoGigHideOverlay,
                    UIStrings.AutoGigHideOverlayHelpMarker))
                Service.Save();

            if (DrawUtil.Checkbox(UIStrings.DrawFishHitbox, ref _gigCfg.AutoGigDrawFishHitbox,
                    UIStrings.DrawFishHitboxHelpMarker))
                Service.Save();

            if (DrawUtil.Checkbox(UIStrings.DrawGigHitbox, ref _gigCfg.AutoGigDrawGigHitbox))
                Service.Save();

            //_gigCfg.Cordial.DrawConfig();
            _gigCfg.ThaliaksFavor.DrawConfig();
        });

        DrawPresetSelector();
    }

    public override void Draw()
    {
        ImGui.PushID("AutoGigTab");
        var selectedPreset = _gigCfg.GetSelectedPreset();

        if (selectedPreset == null)
            return;

        // add new gig button
        if (ImGui.Button(UIStrings.Add_new_fish))
        {
            selectedPreset.AddItem(new BaseGig(0));
            Service.Save();
        }

        ImGui.SameLine();

        ImGui.SetNextItemWidth(90);
        if (ImGui.InputInt(UIStrings.Hitbox + @" ", ref selectedPreset.HitboxSize))
        {
            selectedPreset.HitboxSize = Math.Max(0, Math.Min(selectedPreset.HitboxSize, 300));
            Service.Save();
        }

        DrawUtil.SpacingSeparator();

        selectedPreset.DrawOptions();
        ImGui.PopID();
    }

    public void DrawPresetSelector()
    {
        DrawUtil.DrawComboSelectorPreset(_gigCfg);
        ImGui.SameLine();
        DrawUtil.DrawAddNewPresetButton(_gigCfg);
        ImGui.SameLine();
        DrawUtil.DrawDeletePresetButton(_gigCfg);
    }
}