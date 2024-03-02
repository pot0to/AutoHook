using System;
using System.Diagnostics;
using AutoHook.Resources.Localization;
using AutoHook.Utils;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace AutoHook.Ui;

public class TabConfigGuides : BaseTab
{
    public override string TabName { get; } = UIStrings.Settings;
    public override bool Enabled { get; } = true;
    
    public override void DrawHeader()
    {
        DrawTabDescription(
            "Localization options were added, but currently only English is available. If you want to help with the translation, please visit the link below");

        ImGui.Spacing();
        
        if (ImGui.Button(UIStrings.TabGeneral_DrawHeader_Localization_Help))
        {
            Process.Start(new ProcessStartInfo
                { FileName = "https://crowdin.com/project/autohook", UseShellExecute = true });
        }

        ImGui.Spacing();

        if (ImGui.Button(UIStrings.TabAutoCasts_DrawHeader_Guide_Collectables))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/PunishXIV/AutoHook/blob/main/AcceptCollectable.md",
                UseShellExecute = true
            });
        }

        ImGui.Spacing();
    }

    public override void Draw()
    {
        DrawConfigs();
    }

    private void DrawConfigs()
    {
        DrawDelayHook();
        DrawUtil.SpacingSeparator();
        DrawDelayCasts();
        DrawUtil.SpacingSeparator();
        
        if (DrawUtil.Checkbox(UIStrings.AntiAfkOption, ref Service.Configuration.ResetAfkTimer))
        {
            Service.Save();
        }
        
        if (DrawUtil.Checkbox(UIStrings.DontHideExtraAutoCast, ref Service.Configuration.DontHideOptionsDisabled))
        {
            Service.Save();
        }
        
        if (DrawUtil.Checkbox(UIStrings.Hide_Tab_Description, ref Service.Configuration.HideTabDescription))
        {
            Service.Save();
        }
        
        if (DrawUtil.Checkbox(UIStrings.Show_Current_Status_Header, ref Service.Configuration.ShowStatusHeader))
        {
            Service.Save();
        }
        
        if (DrawUtil.Checkbox(UIStrings.Show_Chat_Logs, ref Service.Configuration.ShowChatLogs,
                UIStrings.Show_Chat_Logs_HelpText))
        {
            Service.Save();
        }
        
        if (DrawUtil.Checkbox(UIStrings.Show_Debug_Console, ref Service.Configuration.ShowDebugConsole))
        {
            Service.Save();
        }
        
        
        if (DrawUtil.Checkbox(UIStrings.Show_Presets_As_Sidebar, ref Service.Configuration.ShowPresetsAsSidebar))
        {
            Service.Save();
        }

        
        DrawUtil.DrawCheckboxTree(UIStrings.SwapTreeNodeButtons, ref Service.Configuration.SwapToButtons, () =>
        {
            if (ImGui.RadioButton(UIStrings.Type_1, Service.Configuration.SwapType == 0))
            {
                Service.Configuration.SwapType = 0;
                Service.Save();
            }
            
            if (ImGui.RadioButton(UIStrings.Type_2, Service.Configuration.SwapType == 1))
            {
                Service.Configuration.SwapType = 1;
                Service.Save();
            }
            
            ImGui.Text("Hello, you're cute!");
        });
    }

    private static void DrawDelayHook()
    {
        ImGui.PushID("DrawDelayHook");
        
        ImGui.TextWrapped(UIStrings.Delay_when_hooking);
        
        ImGui.SetNextItemWidth(45 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt(UIStrings.DrawConfigs_Min_, ref Service.Configuration.DelayBetweenHookMin, 0))
        {
            Service.Configuration.DelayBetweenHookMin = Math.Max(0, Math.Min(Service.Configuration.DelayBetweenHookMin, 9999));
            Service.Save();
        }

        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(45 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt(UIStrings.DrawConfigs_Max_, ref Service.Configuration.DelayBetweenHookMax, 0))
        {
           
            Service.Configuration.DelayBetweenHookMax = Math.Max(0, Math.Min(Service.Configuration.DelayBetweenHookMax, 9999));

            Service.Save();
        }
        
        ImGui.PopID();
    }

    private static void DrawDelayCasts()
    {
        ImGui.PushID("DrawDelayCasts");
        
        ImGui.TextWrapped(UIStrings.Delay_Between_Casts);
        
        ImGui.SetNextItemWidth(45 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt(UIStrings.DrawConfigs_Min_, ref Service.Configuration.DelayBetweenCastsMin, 0))
        {
            
            Service.Configuration.DelayBetweenCastsMin = Math.Max(0, Math.Min(Service.Configuration.DelayBetweenCastsMin, 9999));
            Service.Save();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(45 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt(UIStrings.DrawConfigs_Max_, ref Service.Configuration.DelayBetweenCastsMax, 0))
        {
            
            Service.Configuration.DelayBetweenCastsMax = Math.Max(0, Math.Min(Service.Configuration.DelayBetweenCastsMax, 9999));
            Service.Save();
        }
        
        ImGui.PopID();
    }
    
}