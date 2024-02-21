using System;
using AutoHook.Resources.Localization;
using ImGuiNET;

namespace AutoHook.Ui;

public abstract class BaseTab : IDisposable
{
    public abstract string TabName { get; }
    public abstract bool Enabled { get; }

    private bool _showDescription;

    public abstract void DrawHeader();

    public abstract void Draw();

    public virtual void Dispose()
    {
    }

    public void DrawTabDescription(string tabDescription)
    {
        if (!Service.Configuration.HideTabDescription)
        {
            if (ImGui.TreeNodeEx(UIStrings.Tab_Description, ImGuiTreeNodeFlags.FramePadding))
            {
                _showDescription = true;
                ImGui.TreePop();
            }
            else
                _showDescription = false;

            if (_showDescription)
            {
                ImGui.TextWrapped(tabDescription);
                ImGui.Spacing();
            }
        }
    }
}