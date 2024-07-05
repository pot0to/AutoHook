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
using System.Linq;
using System.Numerics;
using System.Reflection;
using AutoHook.Resources.Localization;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;

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

    public PluginUi() : base($"{Service.PluginName} {Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? ""}###MainAutoHook")
    {
        Service.WindowSystem.AddWindow(this);
        
        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        
        TitleBarButtons.Add(new()
        {
            Click = (m) => { OpenBrowser(@"https://ko-fi.com/initialdet"); },
            Icon = FontAwesomeIcon.Heart,
            ShowTooltip = () => ImGui.SetTooltip("Support AutoHook"),
        });
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

        DrawUtil.Info(UIStrings.StartActionHelpText);

        ImGui.SameLine(0, 3);
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Play, UIStrings.StartActions))
            AutoHook.Plugin.HookManager.StartFishing();

        ImGui.SameLine();

        DrawUtil.Checkbox("###PluginEnable", ref Service.Configuration.PluginEnabled);

        ImGui.SameLine(0, 1);

        if (Service.Configuration.PluginEnabled)
            ImGui.TextColored(ImGuiColors.HealerGreen, UIStrings.Plugin_Enabled);
        else
            ImGui.TextColored(ImGuiColors.DalamudRed, UIStrings.Plugin_Disabled);

        ImGui.SameLine();

        DrawChangelog();
       
        /*if (ImGui.IsItemHovered())
            ImGui.SetTooltip(
                "Start using your Auto Casts!\n\nYou can also use the command /ahstart to start fishing based on your Auto Cast settings. Try making a macro with it!");*/

        if (Service.Configuration.ShowDebugConsole)
        {
            ImGui.Spacing();
            if (ImGui.Button(UIStrings.Open_Console))
                Service.OpenConsole = !Service.OpenConsole;
#if DEBUG
            ImGui.SameLine();
            TestButtons();
#endif
            if (Service.OpenConsole)
                Debug();
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

        ImGui.Spacing();
        DrawTabs();
    }

    private void Debug()
    {
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
                    tab.Draw();
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


    private static void OpenBrowser(string url)
    {
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }

    private bool _openChangelog = false;

    [Localizable(false)]
    private void DrawChangelog()
    {
        var text = UIStrings.Changelog;
        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ImGuiHelpers.GetButtonSize(text).X - 5);

        if (ImGui.Button(text))
            _openChangelog = !_openChangelog;

        if (!_openChangelog)
            return;

        ImGui.SetNextItemWidth(400);
        if (ImGui.Begin($"{text}", ref _openChangelog))
        {
            var changes = PluginChangelog.Versions;

            if (changes.Count > 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
                ImGui.TextWrapped($"{changes[0].VersionNumber}");
                ImGui.PopStyleColor();
                ImGui.Separator();

                //First value is the current Version
                foreach (var mainChange in changes[0].Main)
                {
                    ImGui.TextWrapped($"- {mainChange}");
                }

                ImGui.Spacing();

                if (changes[0].Minor.Count > 0)
                {
                    ImGui.TextWrapped("Minor Changes");
                    foreach (var minorChange in changes[0].Minor)
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

                        foreach (var mainChange in changes[i].Main)
                            ImGui.TextWrapped($"- {mainChange}");

                        if (changes[i].Minor.Count > 0)
                        {
                            ImGui.Spacing();
                            ImGui.TextWrapped("Minor Changes");

                            foreach (var minorChange in changes[i].Minor)
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
        try
        {
            if (ImGui.Button("Check"))
            {
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}