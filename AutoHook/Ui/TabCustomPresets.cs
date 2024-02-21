using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using AutoHook.Configurations;
using AutoHook.Resources.Localization;
using AutoHook.Utils;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace AutoHook.Ui;

public class TabCustomPresets : BaseTab
{
    public override bool Enabled => true;
    public override string TabName => UIStrings.TabNameCustomPresets;

    private PresetConfig? _tempImport;

    private HookPresets _hookPresets = Service.Configuration.HookPresets;

    private SubTabBaitMooch _subTabBaitMooch = new();
    private SubTabAutoCast _subTabAutoCast = new();
    private SubTabFish _subTabFish = new();
    private SubTabExtra _subTabExtra = new();

    public override void DrawHeader()
    {
        DrawTabDescription(UIStrings.TabPresets_DrawHeader_NewTabDescription);

        if (Service.Configuration.ShowPresetsAsSidebar)
            return;
        
        DrawPresetSelectionDropdown();

        ImGui.SameLine();

        DrawAddPresetButton();

        ImGui.SameLine();

        DrawImportExport();

        ImGui.SameLine();

        DrawDeletePreset();

        ImGui.Spacing();
    }

    public override void Draw()
    {
        if (Service.Configuration.ShowPresetsAsSidebar)
        {
            DrawListboxPresets();
        }
        else 
            DrawStandardTabs();
        
    }

    private void DrawListboxPresets()
    {
        ImGui.BeginGroup();

        DrawAddPresetButton();

        ImGui.SameLine();

        DrawImportExport();

        ImGui.SameLine();

        DrawDeletePreset();

        TimedWarning();

        if (ImGui.BeginListBox("", new Vector2(175, -1)))
        {
            if (ImGui.Selectable(UIStrings.None, _hookPresets.SelectedPreset == null))
            {
                _hookPresets.SelectedPreset = null;
            }

            foreach (var preset in _hookPresets.CustomPresets)
            {
                if (ImGui.Selectable(preset.PresetName, preset.PresetName == _hookPresets.SelectedPreset?.PresetName))
                {
                    _hookPresets.SelectedPreset = preset;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(UIStrings.RightClickToRename);
                
                DrawEditPresetNameListbox(preset.PresetName);
            }
            ImGui.EndListBox();
        }

        ImGui.EndGroup();

        ImGui.SameLine();
        
        DrawStandardTabs();
    }

    private void DrawStandardTabs()
    {
        if (_hookPresets.SelectedPreset == null)
            return;
        
        if (ImGui.BeginTabBar(@"TabBarsPreset", ImGuiTabBarFlags.NoTooltip))
        {
            if (ImGui.BeginTabItem(UIStrings.Hooking))
            {   
                DrawUtil.HoveredTooltip(UIStrings.HookingTabHelpText);
                
                if (ImGui.BeginTabBar(@"TabBarCustomHooking", ImGuiTabBarFlags.NoTooltip))
                {
                    if (ImGui.BeginTabItem(UIStrings.Bait))
                    {
                        DrawUtil.HoveredTooltip(UIStrings.BaitTabHelpText);
                        _subTabBaitMooch.IsMooch = false;
                        _subTabBaitMooch.DrawHookTab(_hookPresets.SelectedPreset);
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(UIStrings.Mooch))
                    {
                        DrawUtil.HoveredTooltip(UIStrings.MoochTabHelpText);
                        _subTabBaitMooch.IsMooch = true;
                        _subTabBaitMooch.DrawHookTab(_hookPresets.SelectedPreset);
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
                
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem(_subTabFish.TabName))
            {
                _subTabFish.DrawFishTab(_hookPresets.SelectedPreset);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(UIStrings.ExtraOptions))
            {
                _subTabExtra.DrawExtraTab(_hookPresets.SelectedPreset.ExtraCfg);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(UIStrings.Auto_Casts))
            {
                _subTabAutoCast.DrawAutoCastTab(_hookPresets.SelectedPreset.AutoCastsCfg);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawAddPresetButton()
    {
        ImGui.PushFont(UiBuilder.IconFont);

        var buttonSize = ImGui.CalcTextSize(FontAwesomeIcon.Plus.ToIconString()) + ImGui.GetStyle().FramePadding * 2;
        if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), buttonSize))
        {
            try
            {
                PresetConfig preset = new(@$"{UIStrings.NewPreset} {DateTime.Now}");

                Service.PrintDebug(@$"{UIStrings.NewPreset} {_hookPresets.CustomPresets.Count + 1}");
                _hookPresets.AddPreset(preset);
                _hookPresets.SelectedPreset = preset;
                Service.Save();
            }
            catch (Exception e)
            {
                Service.PluginLog.Error(e.ToString());
            }
        }

        ImGui.PopFont();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(UIStrings.AddNewPreset);
    }

    private void DrawDeletePreset()
    {
        ImGui.PushFont(UiBuilder.IconFont);

        if (_hookPresets.SelectedPreset == null) ImGui.BeginDisabled();

        if (ImGui.Button(@$"{FontAwesomeIcon.Trash.ToIconChar()}", new Vector2(ImGui.GetFrameHeight(), 0)) &&
            ImGui.GetIO().KeyShift)
        {
            if (_hookPresets.SelectedPreset != null)
            {
                _hookPresets.RemovePreset(_hookPresets.SelectedPreset);
                _hookPresets.SelectedPreset = null;
            }

            Service.Save();
        }


        if (_hookPresets.SelectedPreset == null) ImGui.EndDisabled();

        ImGui.PopFont();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(UIStrings.HoldShiftToDelete);
    }

    private void DrawEditPresetNameDropdown()
    {
        if (_hookPresets.SelectedPreset == null)
            return;

        if (ImGui.BeginPopupContextItem(@"PresetName###name"))
        {
            string name = _hookPresets.SelectedPreset.PresetName;

            ImGui.Text(UIStrings.TabPresets_DrawHeader_EditPresetNamePressEnterToConfirm);

            if (ImGui.InputText(UIStrings.PresetName, ref name, 64,
                    ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (_hookPresets.SelectedPreset != null &&
                    _hookPresets.CustomPresets.All(preset => preset.PresetName != name))
                {
                    _hookPresets.SelectedPreset.RenamePreset(name);
                    Service.Save();
                }
            }

            if (ImGui.Button(UIStrings.Close))
            {
                ImGui.CloseCurrentPopup();
                Service.Save();
            }


            ImGui.EndPopup();
        }
    }

    private void DrawEditPresetNameListbox(string presetName)
    {
        if (ImGui.BeginPopupContextItem(@$"PresetName###{presetName}"))
        {
            string name = presetName;

            ImGui.Text(UIStrings.TabPresets_DrawHeader_EditPresetNamePressEnterToConfirm);

            if (ImGui.InputText(UIStrings.PresetName, ref name, 64,
                    ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (_hookPresets.SelectedPreset != null &&
                    _hookPresets.CustomPresets.All(preset => preset.PresetName != name))
                {
                    _hookPresets.CustomPresets.Single(x => x.PresetName == presetName).RenamePreset(name);
                    Service.Save();
                }
            }

            if (ImGui.Button(UIStrings.Close))
            {
                ImGui.CloseCurrentPopup();
                Service.Save();
            }
            
            ImGui.EndPopup();
        }
    }

    private void DrawPresetSelectionDropdown()
    {
        ImGui.TextWrapped(UIStrings.Current_Selected_Preset);
        ImGui.SetNextItemWidth(230);
        if (ImGui.BeginCombo(@"", _hookPresets.SelectedPreset?.PresetName ?? UIStrings.None))
        {
            if (ImGui.Selectable(@$"{UIStrings.None}###disabled", _hookPresets.SelectedPreset == null))
            {
                _hookPresets.SelectedPreset = null;
            }
            
            foreach (var preset in _hookPresets.CustomPresets)
            {
                if (ImGui.Selectable(preset.PresetName, preset.PresetName == _hookPresets.SelectedPreset?.PresetName))
                {
                    _hookPresets.SelectedPreset = preset;
                }
            }

            ImGui.EndCombo();
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(UIStrings.RightClickToRename);

        DrawEditPresetNameDropdown();
    }

    private void DrawImportExport()
    {
        ImGui.PushFont(UiBuilder.IconFont);

        var buttonSize = ImGui.CalcTextSize(FontAwesomeIcon.SignOutAlt.ToIconString()) +
                         ImGui.GetStyle().FramePadding * 2;

        if (ImGui.Button(FontAwesomeIcon.SignOutAlt.ToIconString(), buttonSize))
        {
            try
            {
                ImGui.SetClipboardText(Configuration.ExportActionStack(_hookPresets.SelectedPreset!));

                _alertMessage = UIStrings.PresetExportedToTheClipboard;
                _alertTimer.Start();
            }
            catch (Exception e)
            {
                Service.PrintDebug(e.Message);
                _alertMessage = e.Message;
                _alertTimer.Start();
            }
        }

        ImGui.PopFont();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(UIStrings.ExportPresetToClipboard);

        ImGui.SameLine();

        ImGui.PushFont(UiBuilder.IconFont);

        if (ImGui.Button(FontAwesomeIcon.SignInAlt.ToIconString(), buttonSize))
        {
            try
            {
                _tempImport = Configuration.ImportActionStack(ImGui.GetClipboardText());

                if (_tempImport != null)
                {
                    ImGui.OpenPopup(@"import_new_preset");
                }
            }
            catch (Exception e)
            {
                Service.PrintDebug(@$"[TabCustomPresets] {e.Message}");
                _alertMessage = e.Message;
                _alertTimer.Start();
            }
        }

        ImGui.PopFont();

        if (_tempImport != null)
        {
            if (ImGui.BeginPopup(@"import_new_preset"))
            {
                string name = _tempImport.PresetName;

                if (_tempImport.PresetName.StartsWith(@"[Old Version]"))
                    ImGui.TextColored(ImGuiColors.ParsedOrange, UIStrings.Old_Preset_Warning);
                else
                    ImGui.TextWrapped(UIStrings.ImportThisPreset);

                if (ImGui.InputText(UIStrings.PresetName, ref name, 64, ImGuiInputTextFlags.AutoSelectAll))
                {
                    _tempImport.RenamePreset(name);
                }

                if (ImGui.Button(UIStrings.Import))
                {
                    if (_hookPresets.CustomPresets.Any(preset => preset.PresetName == name))
                    {
                        _alertMessage = UIStrings.PresetAlreadyExist;
                        _alertTimer.Start();
                    }
                    else
                    {
                        _hookPresets.AddPreset(_tempImport);
                        _hookPresets.SelectedPreset = _tempImport;
                        _tempImport = null;
                        Service.Save();
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button(UIStrings.DrawImportExport_Cancel))
                {
                    _tempImport = null;
                    ImGui.CloseCurrentPopup();
                }

                TimedWarning();

                ImGui.EndPopup();
            }
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(UIStrings.ImportStackFromClipboard);

        if (!Service.Configuration.ShowPresetsAsSidebar)
        {
            TimedWarning();
        }
    }

    private const double TimeLimit = 5000;
    private readonly Stopwatch _alertTimer = new();
    private string _alertMessage = @"-";

    private void TimedWarning()
    {
        if (_alertTimer.IsRunning)
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, _alertMessage);

            if (_alertTimer.ElapsedMilliseconds > TimeLimit)
            {
                _alertTimer.Reset();
            }
        }
    }
}