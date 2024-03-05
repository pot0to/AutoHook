using System;
using System.Collections.Generic;
using System.Numerics;
using AutoHook.Configurations;
using AutoHook.Interfaces;
using AutoHook.Resources.Localization;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace AutoHook.Utils;

public static class DrawUtil
{
    public static void NumericDisplay(string label, int value)
    {
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.Text($"{value}");
    }

    public static void NumericDisplay(string label, string formattedString)
    {
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.Text(formattedString);
    }

    public static void NumericDisplay(string label, int value, Vector4 color)
    {
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.TextColored(color, $"{value}");
    }

    public static bool EditNumberField(string label, ref int refValue, string helpText = "")
    {
        return EditNumberField(label, 30, ref refValue, helpText);
    }

    public static bool EditNumberField(string label, float fieldWidth, ref int refValue, string helpText = "")
    {
        TextV(label);

        ImGui.SameLine();

        ImGui.PushItemWidth(fieldWidth * ImGuiHelpers.GlobalScale);
        var clicked = ImGui.InputInt($"##{label}###", ref refValue, 0, 0);
        ImGui.PopItemWidth();

        if (helpText != string.Empty)
        {
            ImGuiComponents.HelpMarker(helpText);
        }

        return clicked;
    }

    public static void TextV(string s)
    {
        var cur = ImGui.GetCursorPos();
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0);
        ImGui.Button("");
        ImGui.PopStyleVar();
        ImGui.SameLine();
        ImGui.SetCursorPos(cur);
        ImGui.TextUnformatted(s);
    }
    
    public static void Info(string text)
    {
        var cur = ImGui.GetCursorPos();
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0);
        ImGui.Button("");
        ImGui.PopStyleVar();
        ImGui.SameLine(0,1);
        ImGui.SetCursorPos(cur);
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextDisabled(FontAwesomeIcon.QuestionCircle.ToIconString());
        ImGui.PopFont();
        
        HoveredTooltip(text);
    }
    
    public static void HoveredTooltip(string text)
    {
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(text);
    }

    public static bool Checkbox(string label, ref bool refValue, string helpText = "", bool hoverHelpText = false)
    {
        bool clicked = false;

        if (ImGui.Checkbox($"{label}", ref refValue))
        {
            clicked = true;
            Service.Save();
        }
        
        if (helpText != string.Empty)
        {
            if (hoverHelpText)
            {
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(helpText);
            }
            else
                ImGuiComponents.HelpMarker(helpText);
        }

        return clicked;
    }

    public static void DrawWordWrappedString(string message)
    {
        var words = message.Split(' ');

        var windowWidth = ImGui.GetContentRegionAvail().X;
        var cumulativeSize = 0.0f;
        var padding = 2.0f;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2.0f, 0.0f));

        foreach (var word in words)
        {
            var wordWidth = ImGui.CalcTextSize(word).X;

            if (cumulativeSize == 0)
            {
                ImGui.Text(word);
                cumulativeSize += wordWidth + padding;
            }
            else if ((cumulativeSize + wordWidth) < windowWidth)
            {
                ImGui.SameLine();
                ImGui.Text(word);
                cumulativeSize += wordWidth + padding;
            }
            else if ((cumulativeSize + wordWidth) >= windowWidth)
            {
                ImGui.Text(word);
                cumulativeSize = wordWidth + padding;
            }
        }

        ImGui.PopStyleVar();
    }

    private static string _filterText = "";

    public static void DrawComboSelector<T>(
        List<T> itemList,
        Func<T, string> getItemName,
        string selectedItem,
        Action<T> onSelect)
    {
        ImGui.SetNextItemWidth(220 * ImGuiHelpers.GlobalScale);

        if (ImGui.BeginCombo("###search", selectedItem))
        {
            string clearText = "";
            ImGui.SetNextItemWidth(190 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputTextWithHint("", UIStrings.Search_Hint, ref clearText, 100))
            {
                _filterText = new string(clearText);
            }

            ImGui.Separator();

            if (ImGui.BeginChild("ComboSelector", new Vector2(0, 100 * ImGuiHelpers.GlobalScale), false))
            {
                foreach (var item in itemList)
                {
                    var itemName = getItemName(item);
                    var filterTextLower = _filterText.ToLower();

                    if (_filterText.Length != 0 && !itemName.ToLower().Contains(filterTextLower))
                        continue;

                    if (ImGui.Selectable(itemName, false))
                    {
                        onSelect(item);
                        _filterText = "";
                        clearText = "";
                        Service.Save();
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.EndChild();
            }

            ImGui.EndCombo();
        }
    }

    private static string _presetFilter = "";

    public static void DrawComboSelectorPreset(IPresetConfig itemList)
    {
        ImGui.SetNextItemWidth(220 * ImGuiHelpers.GlobalScale);

        var selectedPreset = itemList.GetISelectedPreset();
        if (ImGui.BeginCombo("###search", selectedPreset?.Name ?? UIStrings.None))
        {
            string clearText = "";
            ImGui.SetNextItemWidth(210 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputTextWithHint("", UIStrings.Search_Hint, ref clearText, 100))
            {
                _presetFilter = new string(clearText);
            } else _presetFilter = string.Empty;

            ImGui.Separator();

            if (ImGui.BeginChild("ComboSelector", new Vector2(0, 100 * ImGuiHelpers.GlobalScale), false))
            {
                foreach (var item in itemList.GetIPresets())
                {
                    if (item.UniqueId == selectedPreset?.UniqueId)
                        continue;

                    var itemName = new string(item.Name);
                    var filterTextLower = _presetFilter.ToLower();

                    if (_presetFilter.Length != 0 && !itemName.ToLower().Contains(filterTextLower))
                        continue;

                    if (ImGui.Selectable(itemName, false))
                    {
                        itemList.SetSelectedPreset(item.UniqueId);
                        _presetFilter = string.Empty;
                        clearText = string.Empty;
                        Service.Save();
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.EndChild();
            }

            ImGui.EndCombo();
        }
        else if (selectedPreset != null)
        {
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(UIStrings.RightClickToRename);

            if (!ImGui.BeginPopupContextItem(@$"PresetName###{selectedPreset.Name}"))
                return;
            ImGui.Text(UIStrings.TabPresets_DrawHeader_EditPresetNamePressEnterToConfirm);
            var name = selectedPreset.Name;
            if (ImGui.InputText(UIStrings.PresetName, ref name, 64,
                    ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
            {
                selectedPreset.Rename(name);
                Service.Save();
                ImGui.CloseCurrentPopup();
            }

            if (ImGui.Button(UIStrings.Close))
            {
                Service.Save();
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    public static void DrawAddNewPresetButton(IPresetConfig presetConfig)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var buttonSize = ImGui.CalcTextSize(FontAwesomeIcon.Plus.ToIconString()) + ImGui.GetStyle().FramePadding * 2;
        if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), buttonSize))
        {
            try
            {
                presetConfig.AddPresetItem(new(@$"{UIStrings.NewPreset} {DateTime.Now}"));
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

    public static void DrawDeletePresetButton(IPresetConfig itemList)
    {
        var selectedPreset = itemList.GetISelectedPreset();

        if (selectedPreset == null) ImGui.BeginDisabled();
        ImGui.PushFont(UiBuilder.IconFont);
        var buttonSize = ImGui.CalcTextSize(FontAwesomeIcon.Trash.ToIconString()) + ImGui.GetStyle().FramePadding * 2;
        if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString(), buttonSize))
        {
            try
            {
                itemList.RemovePresetItem(selectedPreset?.UniqueId ?? Guid.Empty);

                Service.Save();
            }
            catch (Exception e)
            {
                Service.PluginLog.Error(e.ToString());
            }
        }

        ImGui.PopFont();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(UIStrings.HoldShiftToDelete);

        if (selectedPreset == null) ImGui.EndDisabled();
    }

    public static void DrawCheckboxTree(string treeName, ref bool enable, Action action, string helpText = "")
    {
        ImGui.PushID(treeName);
        
        if (ImGui.Checkbox($"##{treeName}###", ref enable))
            Service.Save();

        if (helpText != string.Empty)
        {
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(helpText);
        }

        ImGui.SameLine(0,3);
        if (Service.Configuration.SwapToButtons)
        {
            switch (Service.Configuration.SwapType)
            {
                case 0:
                    DrawButtonPopupType0(treeName, action, helpText);
                    break;
                case 1:
                    DrawButtonPopupType1(treeName, action, helpText);
                    break;
            }
        }
        else
        {
            var x = ImGui.GetCursorPosX();
            if (ImGui.TreeNodeEx(treeName, ImGuiTreeNodeFlags.FramePadding))
            {
                if (ImGui.IsItemHovered() && helpText != string.Empty)
                    ImGui.SetTooltip(helpText);
                
                ImGui.SetCursorPosX(x);
                ImGui.BeginGroup();
                action();
                ImGui.Separator();
                ImGui.EndGroup();
                
                ImGui.TreePop();
            }
        }

        ImGui.PopID();
    }

    public static void DrawTreeNodeEx(string treeName, Action action, string helpText = "")
    {
        ImGui.PushID(treeName);

        if (Service.Configuration.SwapToButtons)
        {
            switch (Service.Configuration.SwapType)
            {
                case 0:
                    DrawButtonPopupType0(treeName, action, helpText);
                    break;
                case 1:
                    DrawButtonPopupType1(treeName, action, helpText);
                    break;
            }
        }
        else
        {
            var x = ImGui.GetCursorPosX();
            if (ImGui.TreeNodeEx(treeName, ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.AllowItemOverlap))
            {
                if (ImGui.IsItemHovered() && helpText != string.Empty)
                    ImGui.SetTooltip(helpText);

                ImGui.SetCursorPosX(x);
                ImGui.BeginGroup();
                action();
                ImGui.Separator();
                ImGui.EndGroup();
                ImGui.TreePop();
            }
            else if (ImGui.IsItemHovered() && helpText != string.Empty)
                ImGui.SetTooltip(helpText);
        }

        ImGui.PopID();
    }

    public static void DrawButtonPopupType0(string popupName, Action action, string helpText = "")
    {
        ImGui.PushID(popupName);

        TextV(popupName);
        ImGui.SameLine();
        if (ImGui.Button(UIStrings.Configure))
        {
            ImGui.OpenPopup(popupName);
        }

        if (ImGui.IsItemHovered() && helpText != string.Empty)
            ImGui.SetTooltip(helpText);

        if (ImGui.BeginPopup(popupName, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Tooltip))
        {
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();
            ImGui.GetForegroundDrawList()
                .AddRect(windowPos, windowPos + windowSize, ImGui.GetColorU32(ImGuiCol.Separator));

            action();
            ImGui.EndPopup();
        }

        ImGui.PopID();
    }

    public static void DrawButtonPopupType1(string popupName, Action action, string helpText = "")
    {
        ImGui.PushID(popupName);
        if (ImGui.Button(popupName))
        {
            ImGui.OpenPopup(popupName);
        }

        if (ImGui.IsItemHovered() && helpText != string.Empty)
            ImGui.SetTooltip(helpText);

        if (ImGui.BeginPopup(popupName, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Tooltip))
        {
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();
            ImGui.GetForegroundDrawList()
                .AddRect(windowPos, windowPos + windowSize, ImGui.GetColorU32(ImGuiCol.Separator));

            action();
            ImGui.EndPopup();
        }

        ImGui.PopID();
    }

    public static void SpacingSeparator()
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }
}