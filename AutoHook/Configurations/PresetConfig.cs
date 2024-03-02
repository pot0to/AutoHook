using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoHook.Configurations;

public class PresetConfig
{
    public string PresetName { get; set; }

    public List<HookConfig> ListOfBaits { get; set; } = new();
    
    public List<HookConfig> ListOfMooch { get; set; } = new();

    public List<FishConfig> ListOfFish { get; set; } = new();

    public AutoCastsConfig AutoCastsCfg = new();
    
    public ExtraConfig ExtraCfg = new();

    public PresetConfig(string presetName)
    {
        PresetName = presetName;
    }

    public void AddBaitConfig(HookConfig hookConfig)
    {
        if (ListOfBaits.All(hook => hook.BaitFish.Id != hookConfig.BaitFish.Id))
        {
            ListOfBaits.Add(hookConfig);
        }
    }
    
    public void RemoveBaitConfig(HookConfig hookConfig)
    {
        if (ListOfBaits.Any(hook => hook.BaitFish.Id == hookConfig.BaitFish.Id))
        {
            ListOfBaits.Remove(hookConfig);
        }
    }
    
    public void ReplaceBaitConfig(HookConfig hookConfig)
    {
        var existing = ListOfBaits.FirstOrDefault(hook => hook.BaitFish.Id == hookConfig.BaitFish.Id);
        if (existing != null)
        {
            ListOfBaits.Remove(existing);
        }
        
        ListOfBaits.Add(hookConfig);
    }
    
    public void AddMoochConfig(HookConfig hookConfig)
    {
        if (ListOfMooch.All(hook => hook.BaitFish.Id != hookConfig.BaitFish.Id))
        {
            ListOfMooch.Add(hookConfig);
        }
    }

    public void RemoveMoochConfig(HookConfig hookConfig)
    {
        if (ListOfMooch.Any(hook => hook.BaitFish.Id == hookConfig.BaitFish.Id))
        {
            ListOfBaits.Remove(hookConfig);
        }
    }
    
    public void ReplaceMoochConfig(HookConfig moochConfig)
    {
        var existing = ListOfMooch.FirstOrDefault(hook => hook.BaitFish.Id == moochConfig.BaitFish.Id);
        if (existing != null)
        {
            ListOfMooch.Remove(existing);
        }
        
        ListOfMooch.Add(moochConfig);
    }

    public void AddFishConfig(FishConfig fishConfig)
    {
        if (ListOfFish.All(fish => fish.Fish.Id != fishConfig.Fish.Id))
        {
            ListOfFish.Add(fishConfig);
        }
    }

    public void RemoveFishConfig(FishConfig fishConfig)
    {
        if (ListOfFish.Any(fish => fish.Fish.Id == fishConfig.Fish.Id))
        {
            ListOfFish.Remove(fishConfig);
        }
    }

    public void ResetCounter()
    {
        foreach (var item in ListOfBaits)
        {
            HookingManager.FishingCounter.Remove(item.GetUniqueId());
        }
        
        foreach (var item in ListOfMooch)
        {
            HookingManager.FishingCounter.Remove(item.GetUniqueId());
        }
        
        foreach (var item in ListOfFish)
        {
            HookingManager.FishingCounter.Remove(item.GetUniqueId());
        }
    }

    public HookConfig? GetBaitByName(string baitName)
    {
        if (PresetName.Equals(Service.GlobalPresetName))
            return ListOfBaits.FirstOrDefault();

        return ListOfBaits.FirstOrDefault(hook => hook.BaitFish.Name == baitName);
    }

    public HookConfig? GetBaitById(int baitId)
    {
        if (PresetName.Equals(Service.GlobalPresetName))
            return ListOfBaits.FirstOrDefault();

        return ListOfBaits.FirstOrDefault(hook => hook.BaitFish.Id == baitId);
    }

    public HookConfig? GetMoochByName(string baitName)
    {
        if (PresetName.Equals(Service.GlobalPresetName))
            return ListOfMooch.FirstOrDefault();

        return ListOfMooch.FirstOrDefault(hook => hook.BaitFish.Name == baitName);
    }

    public HookConfig? GetMoochById(int baitId)
    {
        if (PresetName.Equals(Service.GlobalPresetName))
            return ListOfMooch.FirstOrDefault();

        return ListOfMooch.FirstOrDefault(hook => hook.BaitFish.Id == baitId);
    }
    
    public FishConfig? GetFishByName(string fishName)
    {
        
        return ListOfFish.FirstOrDefault(fish => fish.Fish.Name == fishName);
    }
    
    public FishConfig? GetFishById(int fishId)
    {

        return ListOfFish.FirstOrDefault(fish => fish.Fish.Id == fishId);
    }
    
    public (FishConfig, string)? GetFishAndPresetById(int fishId)
    {
        var item = ListOfFish.FirstOrDefault(fish => fish.Fish.Id == fishId);
        
        if (item == null)
            return null;
        
        return (item, PresetName);
    }

    // This is just for the conversion of the Config version 1 to version 2
    public void AddListOfHook(List<HookConfig> listOfBaits)
    {
        ListOfBaits.AddRange(listOfBaits);
    }

    public override bool Equals(object? obj)
    {
        return obj is PresetConfig settings &&
               PresetName == settings.PresetName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PresetName + @"a");
    }

    public void RenamePreset(string name)
    {
        PresetName = name;
    }

    public void ConvertV3ToV4()
    {
        foreach (var item in ListOfBaits)
        {
            item.ConvertV3ToV4();
        }
        
        foreach (var item in ListOfMooch)
        {
            item.ConvertV3ToV4();
        }
    }
}