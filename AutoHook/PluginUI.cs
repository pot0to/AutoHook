using AutoHook.Resources.Localization;
using AutoHook.Ui;
using AutoHook.Utils;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PunishLib.ImGuiMethods;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Components;

namespace AutoHook;

public class PluginUi : Window, IDisposable
{
    private readonly List<BaseTab> _tabs = new()
    {
        new TabGlobalPreset(),
        new TabCustomPresets(),
        new TabAutoGig(),
        new TabConfigGuides()
    };

    public PluginUi() : base(string.Format(UIStrings.Plugin_Name_Settings, Service.PluginName))
    {
        Service.WindowSystem.AddWindow(this);

        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.NoScrollWithMouse;
    }

    public void Dispose()
    {
        Service.Save();

        foreach (var tab in _tabs)
        {
            tab.Dispose();
        }

        Service.WindowSystem.RemoveWindow(this);
    }

    public override void Draw()
    {
        if (!IsOpen)
            return;

        //ImGui.TextColored(ImGuiColors.DalamudYellow, "Major plugin rework!!! Please, recheck all of your presets");
        ImGui.Spacing();
        DrawUtil.Checkbox("", ref Service.Configuration.PluginEnabled);

        ImGui.SameLine();

        if (Service.Configuration.PluginEnabled)
            ImGui.TextColored(ImGuiColors.HealerGreen, UIStrings.Plugin_Enabled);
        else
            ImGui.TextColored(ImGuiColors.DalamudRed, UIStrings.Plugin_Disabled);

        ImGuiComponents.HelpMarker(UIStrings.PluginUi_Draw_Enables_Disables);

        ImGui.SameLine();

        DrawLanguageSelector();
        ImGui.SameLine();
        DrawChangelog();
        ImGui.Spacing();

        if (!Service.Configuration.HideLocButton)
        {
            if (ImGui.Button(UIStrings.TabGeneral_DrawHeader_Localization_Help))
            {
                if (ImGui.GetIO().KeyShift)
                {
                    Service.Configuration.HideLocButton = true;
                }
                else Process.Start(new ProcessStartInfo
                    { FileName = "https://crowdin.com/project/autohook", UseShellExecute = true });
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(
                    "This button will be removed soon, im just bringing attention to the localization for a bit. Or if you've read this message, hold shift and click to hide ");
        }
        

        if (Service.Configuration.ShowDebugConsole)
        {
            if (ImGui.Button(UIStrings.Open_Console))
                Service.OpenConsole = !Service.OpenConsole;

            ImGui.SameLine();
#if DEBUG
            TestButtons();
#endif
            if (Service.OpenConsole)
                Debug();

            ImGui.Spacing();
        }

        if (Service.Configuration.ShowStatusHeader)
        {
            if (string.IsNullOrEmpty(Service.Status))
            {
                ImGui.Dummy(new Vector2(ImGui.GetFontSize()));
            }
            else
            {
                ImGui.TextColored(ImGuiColors.DalamudViolet, Service.Status);
            }
        }

        DrawTabs();
    }

    private void Debug()
    {
        if (!Service.OpenConsole)
            return;

        ImGui.PushID(@"debug");
        ImGui.SetNextItemWidth(300);
        if (ImGui.Begin($"DebugWIndows", ref Service.OpenConsole))
        {
            var logs = Service.LogMessages.ToArray().Reverse().ToList();
            for (var i = 0; i < logs.Count; i++)
            {
                if (i == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
                    ImGui.TextWrapped($"{i + 1} - {logs[i]}");
                    ImGui.PopStyleColor();
                }
                else
                    ImGui.TextWrapped($"{i + 1} - {logs[i]}");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }
        }

        ImGui.End();
        ImGui.PopID();
    }

    private void DrawTabs()
    {
        if (ImGui.BeginTabBar(@"AutoHook###TabBars", ImGuiTabBarFlags.NoTooltip))
        {
            foreach (var tab in _tabs)
            {
                if (tab.Enabled == false) continue;

                if (ImGui.BeginTabItem(tab.TabName))
                {
                    ImGui.PushID(tab.TabName);
                    tab.DrawHeader();
                    if (ImGui.BeginChild(tab.TabName, new Vector2(0, 0), true))
                    {
                        tab.Draw();
                        ImGui.EndChild();
                    }

                    ImGui.PopID();
                    ImGui.EndTabItem();
                }
            }

            if (ImGui.BeginTabItem(UIStrings.AboutTab))
            {
                AboutTab.Draw("AutoHook");
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    public override void OnClose()
    {
        Service.Save();
    }

    public static void ShowKofi()
    {
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);

        if (ImGui.Button("Ko-fi"))
        {
            OpenBrowser(@"https://ko-fi.com/initialdet");
        }

        ImGui.PopStyleColor(3);
    }

    private void DrawLanguageSelector()
    {
        ImGui.SetNextItemWidth(55);
        var languages = new List<string>
        {
            @"en",
            @"es",
            @"fr",
            @"de",
            @"ja",
            @"ko",
            @"ru",
            @"zh"
        };
        var currentLanguage = languages.IndexOf(Service.Configuration.CurrentLanguage);

        if (!ImGui.Combo("", ref currentLanguage, languages.ToArray(), languages.Count))
            return;

        Service.Configuration.CurrentLanguage = languages[currentLanguage];
        UIStrings.Culture = new CultureInfo(Service.Configuration.CurrentLanguage);
        Service.Save();
        //Service.Chat.Print("Saved");
    }

    private static void OpenBrowser(string url)
    {
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }

    private bool _openChangelog = false;

    [Localizable(false)]
    private void DrawChangelog()
    {
        if (ImGui.Button(UIStrings.Changelog))
            _openChangelog = !_openChangelog;

        if (!_openChangelog)
            return;

        ImGui.SetNextItemWidth(400);
        if (ImGui.Begin($"{UIStrings.Changelog}", ref _openChangelog))
        {
            var changes = PluginChangelog.Versions;

            if (changes.Count > 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
                ImGui.TextWrapped($"{changes[0].VersionNumber}");
                ImGui.PopStyleColor();
                ImGui.Separator();

                //First value is the current Version
                foreach (var mainChange in changes[0].MainChanges)
                {
                    ImGui.TextWrapped($"- {mainChange}");
                }

                ImGui.Spacing();

                if (changes[0].MinorChanges.Count > 0)
                {
                    ImGui.TextWrapped("Minor Changes");
                    foreach (var minorChange in changes[0].MinorChanges)
                    {
                        ImGui.TextWrapped($"- {minorChange}");
                    }
                }

                ImGui.Separator();

                if (ImGui.BeginChild("old_versions", new Vector2(0, 0), true))
                {
                    for (var i = 1; i < changes.Count; i++)
                    {
                        if (!ImGui.TreeNode($"{changes[i].VersionNumber}"))
                            continue;

                        foreach (var mainChange in changes[i].MainChanges)
                            ImGui.TextWrapped($"- {mainChange}");

                        if (changes[i].MinorChanges.Count > 0)
                        {
                            ImGui.Spacing();
                            ImGui.TextWrapped("Minor Changes");

                            foreach (var minorChange in changes[i].MinorChanges)
                                ImGui.TextWrapped($"- {minorChange}");
                        }

                        ImGui.TreePop();
                    }
                }

                ImGui.EndChild();
            }
        }

        ImGui.End();
    }

    private static unsafe void TestButtons()
    {
    }
}