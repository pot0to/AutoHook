using System;

namespace AutoHook.Interfaces;

public interface IBaseOption
{
    Guid UniqueId { get; }
    
    void DrawOptions();
}