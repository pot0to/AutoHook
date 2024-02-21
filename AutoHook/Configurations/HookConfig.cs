using System;
using System.Collections.Generic;
using AutoHook.Classes;
using AutoHook.Data;
using AutoHook.Enums;
using AutoHook.Utils;

namespace AutoHook.Configurations;

public class HookConfig
{
    public bool Enabled = true;

    private Guid _uniqueId;

    public BaitFishClass BaitFish = new();

    public BaseHookset NormalHook = new(IDs.Status.None);
    public BaseHookset IntuitionHook = new(IDs.Status.FishersIntuition);

    //todo enable more hook settings based on the current status
    //List<BaseHookset> CustomHooksets = new();
    
    public BaseHookset Hookset => GetHookset();
    
    public HookConfig(BaitFishClass baitFish)
    {
        BaitFish = baitFish;
        _uniqueId = Guid.NewGuid();
    }

    private BaseHookset GetHookset()
    {
        var requiredStatusPreset = new List<BaseHookset> { IntuitionHook };
        
        foreach (var preset in requiredStatusPreset)
        {
            if (PlayerResources.HasStatus(preset.RequiredStatus) && preset.UseCustomStatusHook)
            {
                return preset;
            }
        }

        return NormalHook;
    }

    public HookType? GetHook(BiteType bite, double timePassed)
    {
        var hookset = GetHookset();

        var hookDictionary = new Dictionary<BiteType, (BaseBiteConfig th, BaseBiteConfig dh, BaseBiteConfig ph)>
        {
            { BiteType.Weak, (hookset.TripleWeak, hookset.DoubleWeak, hookset.PatienceWeak) },
            { BiteType.Strong, (hookset.TripleStrong, hookset.DoubleStrong, hookset.PatienceStrong) },
            { BiteType.Legendary, (hookset.TripleLegendary, hookset.DoubleLegendary, hookset.PatienceLegendary) }
        };

        if (hookDictionary.TryGetValue(bite, out var hook))
        {
            if (hookset.UseTripleHook && hook.th.HooksetEnabled && CheckHook(hook.th, timePassed))
                return hook.th.HooksetType;

            if (hookset.UseDoubleHook && hook.dh.HooksetEnabled && CheckHook(hook.dh, timePassed))
                return hook.dh.HooksetType;

            if (hook.ph.HooksetEnabled && CheckHook(hook.ph, timePassed))
                return hook.ph.HooksetType;
        }

        return HookType.None;
    }
    
    private bool CheckHook(BaseBiteConfig hookType, double timePassed)
    {
        if (!CheckIdenticalCast(hookType))
            return false;

        if (!CheckSurfaceSlap(hookType))
            return false;

        if (!CheckTimer(hookType, timePassed))
            return false;

        return true;
    }

    private bool CheckIdenticalCast(BaseBiteConfig hookType)
    {
        if (hookType.OnlyWhenActiveIdentical && !PlayerResources.HasStatus(IDs.Status.IdenticalCast))
            return false;

        if (hookType.OnlyWhenNotActiveIdentical && PlayerResources.HasStatus(IDs.Status.IdenticalCast))
            return false;

        return true;
    }

    private bool CheckSurfaceSlap(BaseBiteConfig hookType)
    {
        if (hookType.OnlyWhenActiveSlap && !PlayerResources.HasStatus(IDs.Status.SurfaceSlap))
            return false;

        if (hookType.OnlyWhenNotActiveSlap && PlayerResources.HasStatus(IDs.Status.SurfaceSlap))
            return false;

        return true;
    }

    private bool CheckTimer(BaseBiteConfig hookType, double timePassed)
    {
        var minimumTime = hookType.MinHookTimer;
        var maximumTime = hookType.MaxHookTimer;

        if (PlayerResources.HasStatus(IDs.Status.Chum))
        {
            minimumTime = hookType.ChumMinHookTimer;
            maximumTime = hookType.ChumMaxHookTimer;
        }

        if (minimumTime > 0 && timePassed < minimumTime)
        {
            Service.PrintDebug(@"[CheckTimer] minimum time has not been met: " + timePassed + @" < " + minimumTime);
            return false;
        }

        if (maximumTime > 0 && timePassed > maximumTime)
        {
            Service.PrintDebug(@"[CheckTimer] maximum time has been exceeded: " + timePassed + @" > " + maximumTime);
            return false;
        }

        return true;
    }

    public Guid GetUniqueId()
    {
        if (_uniqueId == Guid.Empty)
            _uniqueId = Guid.NewGuid();

        return _uniqueId;
    }

    public override bool Equals(object? obj)
    {
        return obj is HookConfig settings &&
               BaitFish == settings.BaitFish;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetUniqueId());
    }
    
    public bool HookWeakEnabled = true;
    public bool HookWeakOnlyWhenActiveSlap = false;
    public bool HookWeakOnlyWhenNOTActiveSlap = false;
    public HookType HookTypeWeak = HookType.Precision;

    public bool HookStrongEnabled = true;
    public bool HookStrongOnlyWhenActiveSlap = false;
    public bool HookStrongOnlyWhenNOTActiveSlap = false;
    public HookType HookTypeStrong = HookType.Powerful;

    public bool HookLegendaryEnabled = true;
    public bool HookLegendaryOnlyWhenActiveSlap = false;
    public bool HookLegendaryOnlyWhenNOTActiveSlap = false;
    public HookType HookTypeLegendary = HookType.Powerful;

    public bool HookWeakIntuitionEnabled = true;
    public HookType HookTypeWeakIntuition = HookType.Precision;

    public bool HookStrongIntuitionEnabled = true;
    public HookType HookTypeStrongIntuition = HookType.Powerful;

    public bool HookLegendaryIntuitionEnabled = true;
    public HookType HookTypeLegendaryIntuition = HookType.Powerful;

    public bool UseDoubleHook = false;
    public bool UseTripleHook = false;
    public bool LetFishEscape = false;

    public bool HookWeakDHTHEnabled = true;
    public bool HookStrongDHTHEnabled = true;
    public bool HookLegendaryDHTHEnabled = true;

    //public bool UseDHTHPatience = false;
    public bool UseDHTHOnlyIdenticalCast = false;
    public bool UseDHTHOnlySurfaceSlap = false;

    public bool UseChumTimer = false;
    public double MaxChumTimeDelay = 0;
    public double MinChumTimeDelay = 0;

    public double MaxTimeDelay = 0;
    public double MinTimeDelay = 0;
    public bool StopAfterCaught = false;
    public int StopAfterCaughtLimit = 1;
    public bool StopAfterResetCount = false;

    private FishingSteps StopFishingStep = FishingSteps.None;

    public void ConvertV3ToV4()
    {
        Service.PrintDebug("Starting conversion");
        
        if (NormalHook == null)
            NormalHook =  new(IDs.Status.None);

        if (IntuitionHook == null)
            IntuitionHook =  new(IDs.Status.None);
        
        Convert(NormalHook, false);
        Convert(IntuitionHook, true);
    }

    private void Convert(BaseHookset hookset, bool isIntuition)
    {
        Dictionary<BaseBiteConfig, (bool, HookType, bool, bool, bool)> normal;

        if (isIntuition)
        {
            normal = new ()
            {
                {
                    hookset.PatienceWeak,
                    (HookWeakIntuitionEnabled, HookTypeWeakIntuition, HookWeakOnlyWhenActiveSlap, HookWeakOnlyWhenNOTActiveSlap, false)
                },
                {
                    hookset.PatienceStrong,
                    (HookStrongIntuitionEnabled, HookTypeStrongIntuition, HookStrongOnlyWhenActiveSlap, HookStrongOnlyWhenNOTActiveSlap, false)
                },
                {
                    hookset.PatienceLegendary,
                    (HookLegendaryIntuitionEnabled, HookTypeLegendaryIntuition, HookLegendaryOnlyWhenActiveSlap, HookLegendaryOnlyWhenNOTActiveSlap, false)
                },
            };
        }
        else
        {
            normal = new()
            {
                {
                    hookset.PatienceWeak,
                    (HookWeakEnabled, HookTypeWeak, HookWeakOnlyWhenActiveSlap, HookWeakOnlyWhenNOTActiveSlap, false)
                },
                {
                    hookset.PatienceStrong,
                    (HookStrongEnabled, HookTypeStrong, HookStrongOnlyWhenActiveSlap, HookStrongOnlyWhenNOTActiveSlap, false)
                },
                {
                    hookset.PatienceLegendary,
                    (HookLegendaryEnabled, HookTypeLegendary, HookLegendaryOnlyWhenActiveSlap, HookLegendaryOnlyWhenNOTActiveSlap, false)
                },
            };
        }

        var doubleHook = new Dictionary<BaseBiteConfig, (bool, HookType, bool, bool, bool)>
        {
            {
                hookset.DoubleWeak,
                (HookWeakDHTHEnabled, HookType.Double, UseDHTHOnlySurfaceSlap, false, UseDHTHOnlyIdenticalCast)
            },
            {
                hookset.DoubleStrong,
                (HookStrongDHTHEnabled, HookType.Double, UseDHTHOnlySurfaceSlap, false, UseDHTHOnlyIdenticalCast)
            },
            {
                hookset.DoubleLegendary,
                (HookLegendaryDHTHEnabled, HookType.Double, UseDHTHOnlySurfaceSlap, false, UseDHTHOnlyIdenticalCast)
            }
        };

        var tripleHook = new Dictionary<BaseBiteConfig, (bool, HookType, bool, bool, bool)>
        {
            {
                hookset.TripleWeak,
                (HookWeakDHTHEnabled, HookType.Triple, UseDHTHOnlySurfaceSlap, false, UseDHTHOnlyIdenticalCast)
            },
            {
                hookset.TripleStrong,
                (HookStrongDHTHEnabled, HookType.Triple, UseDHTHOnlySurfaceSlap, false, UseDHTHOnlyIdenticalCast)
            },
            {
                hookset.TripleLegendary,
                (HookLegendaryDHTHEnabled, HookType.Triple, UseDHTHOnlySurfaceSlap, false, UseDHTHOnlyIdenticalCast)
            }
        };

        var list = new List<Dictionary<BaseBiteConfig, (bool, HookType, bool, bool, bool)>>
            { normal, doubleHook, tripleHook };

        foreach (var dict in list)
        {
            foreach (var (bite, (enabled, type, slapActive, slapNotActive, identicalActive)) in dict)
            {
                bite.HooksetEnabled = enabled;
                bite.HooksetType = type;
                bite.OnlyWhenActiveSlap = slapActive;
                bite.OnlyWhenNotActiveSlap = slapNotActive;
                
                bite.OnlyWhenActiveIdentical = identicalActive;

                bite.MinHookTimer = MinTimeDelay;
                bite.MaxHookTimer = MaxTimeDelay;

                if (MinTimeDelay > 0 || MaxTimeDelay > 0)
                {
                    bite.HookTimerEnabled = true;
                }
                bite.ChumMinHookTimer = MinChumTimeDelay;
                bite.ChumMaxHookTimer = MaxChumTimeDelay;
                bite.ChumTimerEnabled = UseChumTimer;
            }
        }

        hookset.UseDoubleHook = UseDoubleHook;
        hookset.LetFishEscapeDoubleHook = LetFishEscape;

        hookset.UseTripleHook = UseTripleHook;
        hookset.LetFishEscapeTripleHook = LetFishEscape;

        hookset.StopAfterCaught = StopAfterCaught;
        hookset.StopAfterCaughtLimit = StopAfterCaughtLimit;
        hookset.StopAfterResetCount = StopAfterResetCount;
        hookset.StopFishingStep = StopFishingStep;
    }
}