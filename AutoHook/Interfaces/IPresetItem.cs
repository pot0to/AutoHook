using System;

namespace AutoHook.Interfaces;

public interface IPresetItem
{
    string Name { get; set; }
    
    Guid UniqueId { get; }
    
    void DrawOptions();
    
    void Rename(string newName);
    
    void AddItem(IBaseOption item);
    
    void RemoveItem(Guid value);
}