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
    
    [NonSerialized]private bool HookWeakEnabled = true;
    [NonSerialized]private bool HookWeakOnlyWhenActiveSlap = false;
    [NonSerialized]private bool HookWeakOnlyWhenNOTActiveSlap = false;
    [NonSerialized]private HookType HookTypeWeak = HookType.Precision;

    [NonSerialized]private bool HookStrongEnabled = true;
    [NonSerialized]private bool HookStrongOnlyWhenActiveSlap = false;
    [NonSerialized]private bool HookStrongOnlyWhenNOTActiveSlap = false;
    [NonSerialized]private HookType HookTypeStrong = HookType.Powerful;

    [NonSerialized]private bool HookLegendaryEnabled = true;
    [NonSerialized]private bool HookLegendaryOnlyWhenActiveSlap = false;
    [NonSerialized]private bool HookLegendaryOnlyWhenNOTActiveSlap = false;
    [NonSerialized]private HookType HookTypeLegendary = HookType.Powerful;

    [NonSerialized]private bool HookWeakIntuitionEnabled = true;
    [NonSerialized]private HookType HookTypeWeakIntuition = HookType.Precision;

    [NonSerialized]private bool HookStrongIntuitionEnabled = true;
    [NonSerialized]private HookType HookTypeStrongIntuition = HookType.Powerful;

    [NonSerialized]private bool HookLegendaryIntuitionEnabled = true;
    [NonSerialized]private HookType HookTypeLegendaryIntuition = HookType.Powerful;

    [NonSerialized]private bool UseDoubleHook = false;
    [NonSerialized]private bool UseTripleHook = false;
    [NonSerialized]private bool LetFishEscape = false;

    [NonSerialized]private bool HookWeakDHTHEnabled = true;
    [NonSerialized]private bool HookStrongDHTHEnabled = true;
    [NonSerialized]private bool HookLegendaryDHTHEnabled = true;

    //[NonSerialized] public bool UseDHTHPatience = false;
    [NonSerialized] private bool UseDHTHOnlyIdenticalCast = false;
    [NonSerialized] private bool UseDHTHOnlySurfaceSlap = false;

    [NonSerialized] private bool UseChumTimer = false;
    [NonSerialized] private double MaxChumTimeDelay = 0;
    [NonSerialized] private double MinChumTimeDelay = 0;

    [NonSerialized] private double MaxTimeDelay = 0;
    [NonSerialized] private double MinTimeDelay = 0;
    [NonSerialized] private bool StopAfterCaught = false;
    [NonSerialized] private int StopAfterCaughtLimit = 1;
    [NonSerialized] private bool StopAfterResetCount = false;

    [NonSerialized] private FishingSteps StopFishingStep = FishingSteps.None;

    public void ConvertV3ToV4()
    {
        Convert(NormalHook, false);
        Convert(IntuitionHook, true);
    }

    private void Convert(BaseHookset hookset, bool isIntuition)
    {
        Dictionary<BaseBiteConfig, (bool, HookType, bool, bool)> normal;

        if (isIntuition)
        {
            normal = new Dictionary<BaseBiteConfig, (bool, HookType, bool, bool)>
            {
                {
                    hookset.PatienceWeak,
                    (HookWeakIntuitionEnabled, HookTypeWeakIntuition, false, false)
                },
                {
                    hookset.PatienceStrong,
                    (HookStrongIntuitionEnabled, HookTypeStrongIntuition, false, false)
                },
                {
                    hookset.PatienceLegendary,
                    (HookLegendaryIntuitionEnabled, HookTypeLegendaryIntuition, false, false)
                },
            };
        }
        else
        {
            normal = new Dictionary<BaseBiteConfig, (bool, HookType, bool, bool)>
            {
                {
                    hookset.PatienceWeak,
                    (HookWeakEnabled, HookTypeWeak, HookWeakOnlyWhenActiveSlap, HookWeakOnlyWhenNOTActiveSlap)
                },
                {
                    hookset.PatienceStrong,
                    (HookStrongEnabled, HookTypeStrong, HookStrongOnlyWhenActiveSlap, HookStrongOnlyWhenNOTActiveSlap)
                },
                {
                    hookset.PatienceLegendary,
                    (HookLegendaryEnabled, HookTypeLegendary, HookLegendaryOnlyWhenActiveSlap,
                        HookLegendaryOnlyWhenNOTActiveSlap)
                },
            };
        }

        var doubleHook = new Dictionary<BaseBiteConfig, (bool, HookType, bool, bool)>
        {
            {
                hookset.DoubleWeak,
                (HookWeakDHTHEnabled, HookType.Double, UseDHTHOnlyIdenticalCast, UseDHTHOnlySurfaceSlap)
            },
            {
                hookset.DoubleStrong,
                (HookStrongDHTHEnabled, HookType.Double, UseDHTHOnlyIdenticalCast, UseDHTHOnlySurfaceSlap)
            },
            {
                hookset.DoubleLegendary,
                (HookLegendaryDHTHEnabled, HookType.Double, UseDHTHOnlyIdenticalCast, UseDHTHOnlySurfaceSlap)
            }
        };

        var tripleHook = new Dictionary<BaseBiteConfig, (bool, HookType, bool, bool)>
        {
            {
                hookset.TripleWeak,
                (HookWeakDHTHEnabled, HookType.Triple, UseDHTHOnlyIdenticalCast, UseDHTHOnlySurfaceSlap)
            },
            {
                hookset.TripleStrong,
                (HookStrongDHTHEnabled, HookType.Triple, UseDHTHOnlyIdenticalCast, UseDHTHOnlySurfaceSlap)
            },
            {
                hookset.TripleLegendary,
                (HookLegendaryDHTHEnabled, HookType.Triple, UseDHTHOnlyIdenticalCast, UseDHTHOnlySurfaceSlap)
            }
        };

        var list = new List<Dictionary<BaseBiteConfig, (bool, HookType, bool, bool)>>
            { normal, doubleHook, tripleHook };

        foreach (var dict in list)
        {
            foreach (var (bite, (enabled, type, onlyIdentical, onlySlap)) in dict)
            {
                bite.HooksetEnabled = enabled;
                bite.HooksetType = type;
                bite.OnlyWhenActiveIdentical = onlyIdentical;
                bite.OnlyWhenActiveSlap = onlySlap;

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