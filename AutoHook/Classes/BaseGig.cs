using System;
using System.ComponentModel;
using System.Linq;
using AutoHook.Interfaces;
using AutoHook.Resources.Localization;
using AutoHook.Spearfishing.Enums;
using AutoHook.Utils;

namespace AutoHook.Classes;

public class BaseGig : IBaseOption
{
    [DefaultValue(true)]
    public bool Enabled = true;
    
    public Fish? Fish;
    
    public bool UseNaturesBounty;
    
    public BaseGig(int itemId)
    {
        Fish = GameRes.ImportedFishes.FirstOrDefault(f => f.ItemId == itemId);
    }
    
    public SpearfishSpeed Speed => Fish?.Speed ?? SpearfishSpeed.Unknown;
    public SpearfishSize Size => Fish?.Size ?? SpearfishSize.Unknown;
    
    public Guid UniqueId { get; } = Guid.NewGuid();

    public void DrawOptions()
    {
        DrawUtil.DrawComboSelector(
            GameRes.ImportedFishes.Where(f => f.IsSpearFish).ToList(),
            (Fish item) => item.Name,
            Fish?.Name ?? UIStrings.None,
            (Fish item) => this.Fish = item);
            
        DrawUtil.Checkbox(UIStrings.UseNaturesBounty, ref UseNaturesBounty);
    }

    public override bool Equals(object? obj)
    {
        return obj is BaseGig settings &&
               Fish?.ItemId == settings.Fish?.ItemId;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(UniqueId);
    }
}