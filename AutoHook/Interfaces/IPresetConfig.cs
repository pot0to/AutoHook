using System;
using System.Collections.Generic;

namespace AutoHook.Interfaces;

public interface IPresetConfig
{
    
    Guid UniqueId { get; }
    
    void AddPresetItem(string presetName);
    
    void RemovePresetItem(Guid value);
    
    void RenamePreset(Guid value, string newName);
    
    void SetSelectedPreset(Guid value);
    
    IPresetItem? GetISelectedPreset();
    
    List<IPresetItem> GetIPresets();
}