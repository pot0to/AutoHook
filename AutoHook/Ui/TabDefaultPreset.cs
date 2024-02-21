using AutoHook.Resources.Localization;
using ImGuiNET;

namespace AutoHook.Ui;

internal class TabDefaultPreset : BaseTab
{
    public override bool Enabled => true;
    public override string TabName => UIStrings.TabName_Global_Preset;

    private SubTabBaitMooch _subTabBaitMooch = new();
    private SubTabAutoCast _subTabAutoCast = new();
    private SubTabFish _subTabFish = new();
    private SubTabExtra _subTabExtra = new();
    
    public override void DrawHeader()
    {
       DrawTabDescription(UIStrings.TabGlobalPreset_Description);
    }

    public override void Draw()
    {
        ImGui.PushID(@"TabBarsDefault");
        if (ImGui.BeginTabBar(@"TabBarsDefault", ImGuiTabBarFlags.NoTooltip))
        {
            var preset = Service.Configuration.HookPresets.DefaultPreset;
            
            if (ImGui.BeginTabItem(UIStrings.Hook))
            {
                if (ImGui.BeginTabBar(@"TabBarHooking", ImGuiTabBarFlags.NoTooltip))
                {
                    if (ImGui.BeginTabItem(UIStrings.Bait))
                    {
                        ImGui.PushID(@"TabDefaultCast");
                        _subTabBaitMooch.IsMooch = false;
                        _subTabBaitMooch.IsDefault = true;
                        _subTabBaitMooch.DrawHookTab(preset);
                        ImGui.PopID();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(@$"{UIStrings.Mooch}"))
                    {
                        ImGui.PushID(@"TabDefaultMooch");
                        _subTabBaitMooch.IsMooch = true;
                        _subTabBaitMooch.IsDefault = true;
                        _subTabBaitMooch.DrawHookTab(preset);
                        ImGui.PopID();
                        ImGui.EndTabItem();
                    }
                    
                    ImGui.EndTabBar();
                }

                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem(_subTabFish.TabName))
            {
                _subTabFish.DrawFishTab(preset);
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem(UIStrings.Extra))
            {
                _subTabExtra.IsDefaultPreset = true;
                _subTabExtra.DrawExtraTab(preset.ExtraCfg);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(@$"{UIStrings.Auto_Casts}"))
            {
                ImGui.PushID(@"TabDefaultAutoCast");
                _subTabAutoCast.IsDefaultPreset = true;
                _subTabAutoCast.DrawAutoCastTab(preset.AutoCastsCfg);
                ImGui.PopID();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopID();
    }
}