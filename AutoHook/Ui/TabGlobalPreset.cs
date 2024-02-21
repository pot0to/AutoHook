using AutoHook.Resources.Localization;
using ImGuiNET;

namespace AutoHook.Ui;

internal class TabGlobalPreset : BaseTab
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
        ImGui.PushID(@"TabBarsGlobal");
        if (ImGui.BeginTabBar(@"TabBarsGlobal"))
        {
            var preset = Service.Configuration.HookPresets.DefaultPreset;
            
            if (ImGui.BeginTabItem(UIStrings.Hooking))
            {
                if (ImGui.BeginTabBar(@"TabBarHooking", ImGuiTabBarFlags.NoTooltip))
                {
                    if (ImGui.BeginTabItem(UIStrings.Bait))
                    {
                        ImGui.PushID(@"TabGlobalBait");
                        _subTabBaitMooch.IsMooch = false;
                        _subTabBaitMooch.IsGlobal = true;
                        _subTabBaitMooch.DrawHookTab(preset);
                        ImGui.PopID();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(@$"{UIStrings.Mooch}"))
                    {
                        ImGui.PushID(@"TabGlobalMooch");
                        _subTabBaitMooch.IsMooch = true;
                        _subTabBaitMooch.IsGlobal = true;
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
                _subTabExtra.IsGlobalPreset = true;
                _subTabExtra.DrawExtraTab(preset.ExtraCfg);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(@$"{UIStrings.Auto_Casts}"))
            {
                ImGui.PushID(@"TabGlobalAutoCast");
                _subTabAutoCast.IsGlobalPreset = true;
                _subTabAutoCast.DrawAutoCastTab(preset.AutoCastsCfg);
                ImGui.PopID();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopID();
    }
}