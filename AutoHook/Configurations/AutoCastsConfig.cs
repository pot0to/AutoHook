using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using AutoHook.Classes;
using AutoHook.Classes.AutoCasts;
using AutoHook.Utils;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace AutoHook.Configurations;

public class AutoCastsConfig
{
    public bool EnableAll = false;

    [DefaultValue(true)] public bool DontCancelMooch = true;

    public TimeOnly StartTime = new(0);
    public TimeOnly EndTime = new(0);

    public bool OnlyCastDuringSpecificTime = false;

    public AutoCastLine CastLine = new();
    public AutoMooch CastMooch = new();
    public AutoChum CastChum = new();
    public AutoCollect CastCollect = new();
    public AutoCordial CastCordial = new();
    public AutoFishEyes CastFishEyes = new();
    public AutoMakeShiftBait CastMakeShiftBait = new();
    public AutoPatience CastPatience = new();
    public AutoPrizeCatch CastPrizeCatch = new();
    public AutoThaliaksFavor CastThaliaksFavor = new();

    private List<BaseActionCast> GetAutoCastOrder()
    {
        var output = new List<BaseActionCast>
        {
            CastThaliaksFavor,
            CastCordial,
            CastPatience,
            CastMakeShiftBait,
            CastChum,
            CastFishEyes,
            CastPrizeCatch,
            CastCollect
        }.OrderBy(x => x.Priority).ToList();

        return output;
    }

    public BaseActionCast? GetNextAutoCast(FishConfig? lastFishCatchCfg)
    {
        if (!EnableAll)
            return null;

        var ignoreCurrentMooch = lastFishCatchCfg != null && lastFishCatchCfg.NeverMooch;

        BaseActionCast? cast = null;

        var order = GetAutoCastOrder();

        foreach (var action in order.Where(action => action.IsAvailableToCast(ignoreCurrentMooch)))
        {
            if (OnlyCastDuringSpecificTime && action.RequiresTimeWindow() && !InsideCastWindow())
                continue;

            Service.PrintDebug($"[AutoCast] Returning {action.Name}");
            return action;
        }

        return cast;
    }

    private unsafe bool InsideCastWindow()
    {
        var clientTime = Framework.Instance()->ClientTime.EorzeaTime;
        var eorzeaTime = TimeOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(clientTime).DateTime);

        return eorzeaTime.IsBetween(StartTime, EndTime);
    }


    public bool TryCastAction(BaseActionCast? action, bool noDelay = false)
    {
        if (action == null || EnableAll == false)
            return false;

        if (OnlyCastDuringSpecificTime && action.RequiresTimeWindow() && !InsideCastWindow())
            return false;

        if (!action.Enabled || !action.IsAvailableToCast())
            return false;

        if (noDelay)
            PlayerRes.CastActionNoDelay(action.Id, action.ActionType, action.GetName());
        else
            PlayerRes.CastActionDelayed(action.Id, action.ActionType, action.GetName());

        return true;
    }
}