using System.Collections.Generic;
using System.Linq;
using AutoHook.Configurations;

namespace AutoHook;

public class HookPresets
{
    // Global preset, cant rename rn 
    
    public PresetConfig DefaultPreset = new(Service.GlobalPresetName);
    
    public List<PresetConfig> CustomPresets = new();

    private PresetConfig? _selectedPreset;

    // i wanted to make Set private but apparently it makes SelectedPreset be null every time you open the game/reload the plugin??
    public PresetConfig? SelectedPreset
    {
        get => _selectedPreset;
        set => SwapPreset(value);
    }
    
    public void AddPreset(PresetConfig presetConfig)
    {
        if (CustomPresets.All(preset => preset.PresetName != presetConfig.PresetName))
        {
            CustomPresets.Add(presetConfig);
        }
    }
    
    public void RemovePreset(PresetConfig presetConfig)
    {
        if (CustomPresets.Any(preset => preset.PresetName == presetConfig.PresetName))
        {
            CustomPresets.Remove(presetConfig);
        }
    }
    private  void SwapPreset(PresetConfig? preset)
    {
        if (_selectedPreset is {ExtraCfg: { Enabled: true, ResetCounterPresetSwap: true } })
        {
            _selectedPreset.ResetCounter();
        }

        _selectedPreset = preset;
    }
}
