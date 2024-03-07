using AutoHook.Classes;
using AutoHook.Configurations;
using AutoHook.Data;
using AutoHook.Enums;
using AutoHook.Resources.Localization;
using AutoHook.SeFunctions;
using AutoHook.Utils;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AutoHook;

public class HookingManager : IDisposable
{
    // todo: refactor this entire class
    private static readonly HookPresets Presets = Service.Configuration.HookPresets;

    private static double _lastTickMs = 0;

    private static double _debugValueLast = 3000;
    private readonly Stopwatch _recastTimer = new();

    private double _timeout;
    private readonly Stopwatch _fishingTimer = new();

    private readonly Stopwatch _timerState = new();

    private Hook<UpdateCatchDelegate>? _catchHook;

    private FishingState _lastState = FishingState.NotFishing;
    private FishingSteps _lastStep = 0;

    private BaitFishClass? _lastCatch;

    public static IntuitionStatus IntuitionStatus { get; private set; } = IntuitionStatus.NotActive;

    private SpectralCurrentStatus _spectralCurrentStatus = SpectralCurrentStatus.NotActive;

    private bool _isMooching;

    private delegate bool UseActionDelegate(IntPtr manager, ActionType actionType, uint actionId, GameObjectID targetId,
        uint a4, uint a5,
        uint a6, IntPtr a7);

    private Hook<UseActionDelegate>? _useActionHook;

    public HookingManager()
    {
        CreateDalamudHooks();
        Enable();
    }

    public void Dispose()
    {
        Disable();

        _catchHook?.Dispose();
        _useActionHook?.Dispose();
    }

    private unsafe void CreateDalamudHooks()
    {
        _catchHook = new UpdateFishCatch(Service.SigScanner).CreateHook(OnCatchUpdate);
        var hookPtr = (IntPtr)ActionManager.MemberFunctionPointers.UseAction;
        _useActionHook = Service.GameInteropProvider.HookFromAddress<UseActionDelegate>(hookPtr, OnUseAction);
    }

    private void Enable()
    {
        Service.Framework.Update += OnFrameworkUpdate;
        _catchHook?.Enable();
        _useActionHook?.Enable();
    }

    private void Disable()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        _useActionHook?.Disable();
        _catchHook?.Disable();
    }

    public void StartFishing()
    {
        if (!PlayerRes.IsCastAvailable())
        {
            Service.PrintChat(@"[AutoHook] You can't cast right now.");
            return;
        }

        var extraCfg = GetExtraCfg();
        if (extraCfg is { ForceBaitSwap: true, Enabled: true })
        {
            var result = Service.EquipedBait.ChangeBait((uint)extraCfg.ForcedBaitId);

            if (result == CurrentBait.ChangeBaitReturn.Success)
            {
                Service.PrintChat(
                    @$"[AutoHook] Starting with bait: {MultiString.GetItemName(extraCfg.ForcedBaitId)}");
                Service.Save();
            }
        }

        _lastStep = FishingSteps.StartedCasting;
        UseAutoCasts();
    }

    private int GetCurrentBaitMoochId()
    {
        if (_isMooching)
            return _lastCatch?.Id ?? 0;

        return (int)Service.EquipedBait.Current;
    }

    // The current config is updates two times: When we began fishing (to get the config based on the mooch/bait) and when we hooked the fish (in case the user updated their configs).
    private void UpdateStatusAndTimer()
    {
        ResetAfkTimer();

        var selected = GetHookCfg();
        var hookset = selected.GetHookset();
        if (selected.Enabled)
        {
            _timeout = PlayerRes.HasStatus(IDs.Status.Chum)
                ? hookset.ChumTimeoutMax
                : hookset.TimeoutMax;
        }
        else
            _timeout = 0;

        if (Service.Configuration.ShowStatusHeader)
        {
            string buffStatus = "";

            if (hookset.RequiredStatus != 0)
            {
                buffStatus = MultiString.GetStatusName(hookset.RequiredStatus);
                buffStatus = @$"({buffStatus})";
            }

            var hookCfgName = GetPresetName();

            string message = !selected.Enabled
                ? @$"No config found. Not hooking"
                : @$"Hook Found: {hookCfgName} {buffStatus}";

            Service.Status = message;
            Service.PrintDebug(@$"[HookManager] {message}");
        }
    }

    public string GetPresetName()
    {
        var customHook = _isMooching
            ? Presets.SelectedPreset?.GetMoochById(GetCurrentBaitMoochId())
            : Presets.SelectedPreset?.GetBaitById(GetCurrentBaitMoochId());

        var globalHook = _isMooching
            ? Presets.DefaultPreset.ListOfMooch.FirstOrDefault()
            : Presets.DefaultPreset.ListOfBaits.FirstOrDefault();

        var presetName = customHook?.Enabled ?? false
            ? @$"{customHook.BaitFish.Name} ({Presets.SelectedPreset?.PresetName})"
            : globalHook?.Enabled ?? false
                ? @$"{(_isMooching ? UIStrings.All_Mooches : UIStrings.All_Baits)} ({Presets.DefaultPreset.PresetName})"
                : @"None";

        return presetName;
    }

    public HookConfig GetHookCfg()
    {
        var customHook = _isMooching
            ? Presets.SelectedPreset?.GetMoochById(GetCurrentBaitMoochId())
            : Presets.SelectedPreset?.GetBaitById(GetCurrentBaitMoochId());

        var defaultHook = _isMooching
            ? Presets.DefaultPreset.ListOfMooch.FirstOrDefault()
            : Presets.DefaultPreset.ListOfBaits.FirstOrDefault();

        var currentHook = customHook?.Enabled ?? false ? customHook : defaultHook!;

        return currentHook;
    }

    public AutoCastsConfig GetAutoCastCfg()
    {
        return Presets.SelectedPreset?.AutoCastsCfg.EnableAll ?? false
            ? Presets.SelectedPreset.AutoCastsCfg
            : Presets.DefaultPreset.AutoCastsCfg;
    }

    public ExtraConfig GetExtraCfg()
    {
        return Presets.SelectedPreset?.ExtraCfg.Enabled ?? false
            ? Presets.SelectedPreset.ExtraCfg
            : Presets.DefaultPreset.ExtraCfg;
    }

    private FishConfig? GetLastCatchConfig()
    {
        if (_lastCatch == null)
            return null;

        return Presets.SelectedPreset?.GetFishById(_lastCatch.Id) ?? Presets.DefaultPreset.GetFishById(_lastCatch.Id);
    }

    private void OnFrameworkUpdate(IFramework _)
    {
        var currentState = Service.EventFramework.FishingState;

        if (!Service.Configuration.PluginEnabled || currentState == FishingState.NotFishing)
            return;

        if (currentState != FishingState.Quit && _lastStep.HasFlag(FishingSteps.Quitting))
        {
            if (PlayerRes.IsCastAvailable())
            {
                PlayerRes.CastActionDelayed(IDs.Actions.Quit, ActionType.Action, @"Quit");
                currentState = FishingState.Quit;
            }
        }

        //CheckFishingState();
        
        if (!_lastStep.HasFlag(FishingSteps.Quitting) && currentState == FishingState.PoleReady)
        {
            CheckPluginActions();
        }
        
        if (currentState == FishingState.Waiting2)
            CheckTimeout();

        if (_lastState == currentState)
            return;

        if (currentState == FishingState.PoleReady)
            Service.Status = @$"";

        _lastState = currentState;

        switch (currentState)
        {
            case FishingState.PullPoleIn: // If a hook is manually used before a bite, dont use auto cast
                if (_lastStep.HasFlag(FishingSteps.BeganFishing) || _lastStep.HasFlag(FishingSteps.BeganMooching))
                    _lastStep = FishingSteps.None;
                _fishingTimer.Reset();
                _lastTickMs = 0;
                break;
            case FishingState.PoleOut:
                if (!_fishingTimer.IsRunning) _fishingTimer.Start();
                break;
            case FishingState.Bite:
                if (!_lastStep.HasFlag(FishingSteps.FishBit)) OnBite();
                break;
            case FishingState.Quit:
                OnFishingStop();
                break;
        }
    }

    private void CheckPluginActions()
    {
        if (!_recastTimer.IsRunning)
            _recastTimer.Start();

        if (!(_recastTimer.ElapsedMilliseconds >= _lastTickMs))
            return;

        _lastTickMs = _recastTimer.ElapsedMilliseconds + 500;
        
        var lastCatch = GetLastCatchConfig();
        var extraCfg = GetExtraCfg();
            
        if (_lastStep.HasFlag(FishingSteps.FishCaught))
            CheckStopCondition();
            
        // the order matters
        CheckExtraActions(extraCfg);

        bool casted = false;
        if (_lastStep.HasFlag(FishingSteps.FishCaught))
        {
            casted = UseFishCaughtActions(lastCatch);
            CheckFishCaughtSwap(lastCatch);
        }
            
        if(!casted)
            UseAutoCasts();
    }

    private void OnBeganFishing()
    {
        if (_lastStep.HasFlag(FishingSteps.BeganFishing) &&
            (_lastState != FishingState.PoleReady || _lastState != FishingState.NotFishing))
            return;

        CastCollect();

        _lastStep = FishingSteps.BeganFishing;
        _isMooching = false;

        UpdateStatusAndTimer();
    }

    private void OnBeganMooch()
    {
        if (_lastStep.HasFlag(FishingSteps.BeganMooching) && _lastState != FishingState.PoleReady)
            return;

        CastCollect();

        _lastStep = FishingSteps.BeganMooching;
        _isMooching = true;

        UpdateStatusAndTimer();
    }

    private void CastCollect()
    {
        var cfg = GetAutoCastCfg();
        if (cfg.TryCastAction(cfg.CastCollect))
            return;
    }

    private void OnBite()
    {
        UpdateStatusAndTimer();
        var currentHook = GetHookCfg();
        _fishingTimer.Stop();

        _lastCatch = null;
        _lastStep = FishingSteps.FishBit;
        HookFish(Service.TugType?.Bite ?? BiteType.Unknown, currentHook);
    }

    private async void HookFish(BiteType bite, HookConfig currentHook)
    {
        var delay = new Random().Next(Service.Configuration.DelayBetweenHookMin,
            Service.Configuration.DelayBetweenHookMax);

        if (!currentHook.Enabled)
            return;

        var timePassed = Math.Truncate(_fishingTimer.ElapsedMilliseconds / 1000.0 * 100) / 100;

        var hook = currentHook.GetHook(bite, timePassed);

        if (hook is null or HookType.None)
            return;

        await Task.Delay(delay);

        PlayerRes.CastActionDelayed((uint)hook, ActionType.Action, @$"{hook.ToString()}");
        Service.PrintDebug(@$"[HookManager] Using {hook.ToString()} hook. (Bite: {bite})");
    }

    private void OnCatch(uint fishId, uint amount)
    {
        _lastCatch = GameRes.Fishes.FirstOrDefault(fish => fish.Id == fishId) ?? new BaitFishClass(@"-", -1);
        var lastFishCatchCfg = GetLastCatchConfig();

        Service.LastCatch = _lastCatch;

        Service.PrintDebug(@$"[HookManager] Caught {_lastCatch.Name} (id {_lastCatch.Id})");

        _lastStep = FishingSteps.FishCaught;

        if (lastFishCatchCfg != null)
        {
            for (var i = 0; i < amount; i++)
            {
                FishingHelper.AddFishCount(lastFishCatchCfg.GetUniqueId());
            }
        }

        var hook = GetHookCfg();
        if (hook.Enabled)
            FishingHelper.AddFishCount(hook.GetUniqueId());
    }

    private void OnFishingStop()
    {
        _lastStep = FishingSteps.None;

        if (_fishingTimer.IsRunning)
            _fishingTimer.Reset();

        if (_recastTimer.IsRunning)
            _recastTimer.Reset();

        if (_timerState.IsRunning)
            _timerState.Reset();

        Service.Status = "";

        _lastTickMs = 0;

        FishingHelper.Reset();

        PlayerRes.CastActionNoDelay(IDs.Actions.Quit);
        PlayerRes.DelayNextCast(0);
    }

    private void UseAutoCasts()
    {
        // if _lastStep is FishBit but currentState is FishingState.PoleReady, it means that the fish was hooked, but it escaped.
        if (_lastStep.HasFlag(FishingSteps.None) || _lastStep.HasFlag(FishingSteps.BeganFishing) || _lastStep.HasFlag(FishingSteps.BeganMooching) || _lastStep.HasFlag(FishingSteps.Quitting))
        {
            return;
        }
        
        if (!PlayerRes.IsCastAvailable())
            return;

        var lastFishCatchCfg = GetLastCatchConfig();
        
        var acCfg = GetAutoCastCfg();

        var ignoreMooch = lastFishCatchCfg?.NeverMooch ?? false;
        var autoCast = acCfg.GetNextAutoCast(ignoreMooch);

        if (acCfg.TryCastAction(autoCast, false, ignoreMooch))
            return;

        CastLineMoochOrRelease(acCfg, lastFishCatchCfg);
    }

    private bool UseFishCaughtActions(FishConfig? lastFishCatchCfg)
    {
        BaseActionCast? cast = null;

        if (lastFishCatchCfg == null || !lastFishCatchCfg.Enabled)
            return false;

        if (PlayerRes.HasStatus(IDs.Status.FishersIntuition) && lastFishCatchCfg.IgnoreOnIntuition)
            return false;

        var caughtCount = FishingHelper.GetFishCount(lastFishCatchCfg.GetUniqueId());

        if (lastFishCatchCfg.IdenticalCast.IsAvailableToCast(caughtCount))
            cast = lastFishCatchCfg.IdenticalCast;

        if (lastFishCatchCfg.SurfaceSlap.IsAvailableToCast())
            cast = lastFishCatchCfg.SurfaceSlap;

        if (cast != null)
        {
            PlayerRes.CastActionDelayed(cast.Id, cast.ActionType, cast.Name);
            return true;
        }

        return false;
    }

    private void CheckFishCaughtSwap(FishConfig? lastCatchCfg)
    {
        if (lastCatchCfg == null || !lastCatchCfg.Enabled)
            return;

        var caughtCount = FishingHelper.GetFishCount(lastCatchCfg.GetUniqueId());
        var guid = lastCatchCfg.GetUniqueId();
        
        if (lastCatchCfg.SwapPresets && !FishingHelper.SwappedPreset(guid) && !_lastStep.HasFlag(FishingSteps.PresetSwapped))
        {
            if (caughtCount >= lastCatchCfg.SwapPresetCount && lastCatchCfg.PresetToSwap != Presets.SelectedPreset?.PresetName)
            {
                var preset =
                    Presets.CustomPresets.FirstOrDefault(preset => preset.PresetName == lastCatchCfg.PresetToSwap);

                FishingHelper.AddPresetSwap(guid); // one try per catch
                _lastStep |= FishingSteps.PresetSwapped;
                
                if (preset == null)
                    Service.PrintChat(@$"Preset {lastCatchCfg.PresetToSwap} not found.");
                else
                {
                    Presets.SelectedPreset = preset;
                    Service.PrintChat(@$"[Fish Caught] Swapping current preset to {lastCatchCfg.PresetToSwap}");
                    Service.Save();
                }
            }
        }
        
        if (lastCatchCfg.SwapBait && !FishingHelper.SwappedBait(guid) && !_lastStep.HasFlag(FishingSteps.BaitSwapped))
        {
            if (caughtCount >= lastCatchCfg.SwapBaitCount && lastCatchCfg.BaitToSwap.Id != Service.EquipedBait.Current)
            {
                var result = Service.EquipedBait.ChangeBait(lastCatchCfg.BaitToSwap);

                FishingHelper.AddBaitSwap(guid); // one try per catch
                _lastStep |= FishingSteps.BaitSwapped;
                if (result == CurrentBait.ChangeBaitReturn.Success)
                {
                    Service.PrintChat(@$"[Fish Caught] Swapping bait to {lastCatchCfg.BaitToSwap.Name}");
                    Service.Save();
                }
            }
        }
    }

    private void CheckExtraActions(ExtraConfig extraCfg)
    {
        CheckIntuition(extraCfg);
        CheckSpectral(extraCfg);
        CheckAnglersArt(extraCfg);
    }

    private void CheckSpectral(ExtraConfig extraCfg)
    {
        if (_spectralCurrentStatus == SpectralCurrentStatus.NotActive)
        {
            if (!PlayerRes.IsInActiveSpectralCurrent())
                return;

            _spectralCurrentStatus = SpectralCurrentStatus.Active;

            if (!extraCfg.Enabled)
                return;

            // Check if the preset was already swapped
            if (extraCfg.SwapPresetSpectralCurrentGain && !_lastStep.HasFlag(FishingSteps.PresetSwapped))
            {
                var preset =
                    Presets.CustomPresets.FirstOrDefault(preset =>
                        preset.PresetName == extraCfg.PresetToSwapSpectralCurrentGain);

                _lastStep |= FishingSteps.PresetSwapped; // one try
                if (preset != null)
                {
                    Presets.SelectedPreset = preset;
                    Service.PrintChat(
                        @$"[Extra] Spectral Current Active: Swapping preset to {extraCfg.PresetToSwapSpectralCurrentGain}");
                    Service.Save();
                }
                else
                    Service.PrintChat(@$"Preset {extraCfg.PresetToSwapSpectralCurrentGain} not found.");
            }

            // Check if the bait was already swapped
            if (extraCfg.SwapBaitSpectralCurrentGain && !_lastStep.HasFlag(FishingSteps.BaitSwapped))
            {
                var result = Service.EquipedBait.ChangeBait(extraCfg.BaitToSwapSpectralCurrentGain);

                _lastStep |= FishingSteps.BaitSwapped; // one try
                if (result == CurrentBait.ChangeBaitReturn.Success)
                {
                    Service.PrintChat(@$"[Extra] Spectral Current Active: Swapping bait to {extraCfg.BaitToSwapSpectralCurrentGain.Name}");
                    Service.Save();
                }
            }
        }

        if (_spectralCurrentStatus == SpectralCurrentStatus.Active)
        {
            if (PlayerRes.IsInActiveSpectralCurrent())
                return;

            _spectralCurrentStatus = SpectralCurrentStatus.NotActive;

            // Check if the preset was already swapped
            if (!extraCfg.Enabled)
                return;

            if (extraCfg.SwapPresetSpectralCurrentLost && !_lastStep.HasFlag(FishingSteps.PresetSwapped))
            {
                var preset =
                    Presets.CustomPresets.FirstOrDefault(preset =>
                        preset.PresetName == extraCfg.PresetToSwapSpectralCurrentLost);

                _lastStep |= FishingSteps.PresetSwapped; // one try

                if (preset != null)
                {
                    Presets.SelectedPreset = preset;
                    Service.PrintChat(@$"[Extra] Spectral Current Ended: Swapping preset to {extraCfg.SwapPresetSpectralCurrentLost}");
                    Service.Save();
                }
                else
                    Service.PrintChat(@$"Preset {extraCfg.SwapPresetSpectralCurrentLost} not found.");
            }

            // Check if the bait was already swapped
            if (extraCfg.SwapBaitSpectralCurrentLost && !_lastStep.HasFlag(FishingSteps.BaitSwapped))
            {
                var result = Service.EquipedBait.ChangeBait(extraCfg.BaitToSwapSpectralCurrentLost);

                _lastStep |= FishingSteps.BaitSwapped; // one try

                if (result == CurrentBait.ChangeBaitReturn.Success)
                {
                    Service.PrintChat(@$"[Extra] Spectral Current Ended: Swapping bait to {extraCfg.BaitToSwapSpectralCurrentLost.Name}");
                    Service.Save();
                }
            }
        }
    }

    private void CheckIntuition(ExtraConfig extraCfg)
    {
        if (IntuitionStatus == IntuitionStatus.NotActive)
        {
            if (!PlayerRes.HasStatus(IDs.Status.FishersIntuition))
                return;

            IntuitionStatus = IntuitionStatus.Active; // only one try

            if (!extraCfg.Enabled)
                return;
            ExtraCfgGainedIntuition(extraCfg);
        }

        if (IntuitionStatus == IntuitionStatus.Active)
        {
            if (PlayerRes.HasStatus(IDs.Status.FishersIntuition))
                return;

            IntuitionStatus = IntuitionStatus.NotActive; // only one try

            if (!extraCfg.Enabled)
                return;

            ExtraCfgLostIntuition(extraCfg);
        }
    }

    private void ExtraCfgGainedIntuition(ExtraConfig extraCfg)
    {
        // Check if the preset was already swapped
        if (extraCfg.SwapPresetIntuitionGain && !_lastStep.HasFlag(FishingSteps.PresetSwapped))
        {
            var preset = Presets.CustomPresets.FirstOrDefault(preset =>
                preset.PresetName == extraCfg.PresetToSwapIntuitionGain);

            _lastStep |= FishingSteps.PresetSwapped;
            if (preset != null)
            {
                Presets.SelectedPreset = preset;
                Service.PrintChat(@$"[Extra] Intuition Active - Swapping preset to {extraCfg.PresetToSwapIntuitionGain}");
                Service.Save();
            }
            else
                Service.PrintChat(@$"[Extra] Intuition Active - Preset {extraCfg.PresetToSwapIntuitionGain} not found.");
        }

        // Check if the bait was already swapped
        if (extraCfg.SwapBaitIntuitionGain && !_lastStep.HasFlag(FishingSteps.BaitSwapped))
        {
            var result = Service.EquipedBait.ChangeBait(extraCfg.BaitToSwapIntuitionGain);

            _lastStep |= FishingSteps.BaitSwapped; // one try per catch

            if (result == CurrentBait.ChangeBaitReturn.Success)
            {
                Service.PrintChat(@$"[Extra] Intuition Active - Swapping bait to {extraCfg.BaitToSwapIntuitionGain.Name}");
                Service.Save();
            }
        }
    }

    private void ExtraCfgLostIntuition(ExtraConfig extraCfg)
    {
        // Check if the preset was already swapped
        if (extraCfg.SwapPresetIntuitionLost && !_lastStep.HasFlag(FishingSteps.PresetSwapped))
        {
            var preset =
                Presets.CustomPresets.FirstOrDefault(preset =>
                    preset.PresetName == extraCfg.PresetToSwapIntuitionLost);

            _lastStep |= FishingSteps.PresetSwapped;

            if (preset != null)
            {
                // one try per catch
                Presets.SelectedPreset = preset;
                Service.PrintChat(@$"[Extra] Intuition Lost - Swapping preset to {extraCfg.PresetToSwapIntuitionLost}");
                Service.Save();
            }
            else
                Service.PrintChat(@$"[Extra] Intuition Lost - Preset {extraCfg.PresetToSwapIntuitionLost} not found.");
        }

        // Check if the bait was already swapped
        if (extraCfg.SwapBaitIntuitionLost && !_lastStep.HasFlag(FishingSteps.BaitSwapped))
        {
            var result = Service.EquipedBait.ChangeBait(extraCfg.BaitToSwapIntuitionLost);

            // one try per catch
            _lastStep |= FishingSteps.BaitSwapped;
            if (result == CurrentBait.ChangeBaitReturn.Success)
            {
                Service.PrintChat(@$"[Extra] Intuition Lost - Swapping bait to {extraCfg.BaitToSwapIntuitionLost.Name}");
                Service.Save();
            }
        }

        if (extraCfg.QuitOnIntuitionLost)
        {
            _lastStep = FishingSteps.Quitting;
        }

        if (extraCfg.StopOnIntuitionLost)
        {
            _lastStep = FishingSteps.None;
        }
    }

    private void CheckAnglersArt(ExtraConfig extraCfg)
    {
        if (!PlayerRes.HasAnglersArtStacks(extraCfg.AnglerStackQtd))
            return;

        if (extraCfg.SwapPresetAnglersArt && !_lastStep.HasFlag(FishingSteps.PresetSwapped))
        {
            var preset =
                Presets.CustomPresets.FirstOrDefault(preset =>
                    preset.PresetName == extraCfg.PresetToSwapAnglersArt);

            _lastStep |= FishingSteps.PresetSwapped;

            if (preset != null)
            {
                Presets.SelectedPreset = preset;
                Service.PrintChat(
                    @$"[Extra] Angler's Stack - Swapping preset to {extraCfg.PresetToSwapAnglersArt}");
                Service.Save();
            }
            else
                Service.PrintChat(@$"[Extra] Anglers Stack - Preset {extraCfg.PresetToSwapAnglersArt} not found.");
        }

        if (extraCfg.SwapBaitAnglersArt && !_lastStep.HasFlag(FishingSteps.BaitSwapped))
        {
            var result = Service.EquipedBait.ChangeBait(extraCfg.BaitToSwapAnglersArt);
            _lastStep |= FishingSteps.BaitSwapped;
            if (result == CurrentBait.ChangeBaitReturn.Success)
            {
                Service.PrintChat(
                    @$"[Extra] Angler's Stack - Swapping bait to {extraCfg.BaitToSwapAnglersArt.Name}");
                Service.Save();
            }
        }
    }

    private void CastLineMoochOrRelease(AutoCastsConfig acCfg, FishConfig? lastFishCatchCfg)
    {
        var blockMooch = lastFishCatchCfg is { Enabled: true, NeverMooch: true };

        if (!blockMooch)
        {
            if (lastFishCatchCfg is { Enabled: true } && lastFishCatchCfg.Mooch.IsAvailableToCast())
            {
                PlayerRes.CastActionNoDelay(lastFishCatchCfg.Mooch.Id, lastFishCatchCfg.Mooch.ActionType,
                    UIStrings.Mooch);
                return;
            }

            if (acCfg.TryCastAction(acCfg.CastMooch, true))
                return;
        }

        if (acCfg.TryCastAction(acCfg.CastLine, true))
            return;
    }

    private void CheckStopCondition()
    {
        var lastFishCatchCfg = GetLastCatchConfig();
        var currentHook = GetHookCfg();
        var hookset = currentHook.GetHookset();
        var extra = GetExtraCfg();

        if (lastFishCatchCfg?.StopAfterCaught ?? false)
        {
            var guid = lastFishCatchCfg.GetUniqueId();
            var total = FishingHelper.GetFishCount(guid);

            if (total >= lastFishCatchCfg.StopAfterCaughtLimit)
            {
                Service.PrintChat(string.Format(UIStrings.Caught_Limited_Reached_Chat_Message,
                    @$"{lastFishCatchCfg.Fish.Name}: {lastFishCatchCfg.StopAfterCaughtLimit}"));

                _lastStep = lastFishCatchCfg.StopFishingStep;
                if (lastFishCatchCfg.StopAfterResetCount) FishingHelper.RemoveId(guid);
            }
        }

        if (currentHook.Enabled && hookset.StopAfterCaught)
        {
            var guid = currentHook.GetUniqueId();
            var total = FishingHelper.GetFishCount(guid);

            if (total >= hookset.StopAfterCaughtLimit)
            {
                Service.PrintChat(string.Format(UIStrings.Hooking_Limited_Reached_Chat_Message,
                    @$"{currentHook.BaitFish.Name}: {hookset.StopAfterCaughtLimit}"));

                _lastStep = hookset.StopFishingStep;
                if (hookset.StopAfterResetCount) FishingHelper.RemoveId(guid);
            }
        }

        if (extra.StopAfterAnglersArt && extra.Enabled)
        {
            if (!PlayerRes.HasAnglersArtStacks(extra.AnglerStackQtd))
                return;

            _lastStep = extra.AnglerStopFishingStep;
            Service.PrintChat(@$"[Extra] Angler's Stack Reached: Stopping fishing");
        }
    }

    private void CheckTimeout()
    {
        if (!_fishingTimer.IsRunning)
            _fishingTimer.Start();

        double maxTime = Math.Truncate(_timeout * 100) / 100;

        var currentTime = Math.Truncate(_fishingTimer.ElapsedMilliseconds / 1000.0 * 100) / 100;

        if (!(maxTime > 0) || !(currentTime > maxTime) || _lastStep.HasFlag(FishingSteps.TimeOut))
            return;

        Service.PrintDebug(@"[HookManager] Timeout. Hooking fish.");
        _lastStep = FishingSteps.TimeOut;
        PlayerRes.CastActionDelayed(IDs.Actions.Hook, ActionType.Action, UIStrings.Hook);
    }

    private static void ResetAfkTimer()
    {
        if (!Service.Configuration.ResetAfkTimer)
            return;

        if (!InputUtil.TryFindGameWindow(out var windowHandle)) return;

        // Virtual key for Right Winkey. Can't be used by FFXIV normally, and in tests did not seem to cause any
        // unusual interference.
        InputUtil.SendKeycode(windowHandle, 0x5C);
    }

    // ReSharper disable once UnusedMember.Local
    private void CheckFishingState()
    {
        if (!_timerState.IsRunning)
            _timerState.Start();

        if (!(_timerState.ElapsedMilliseconds > _debugValueLast + 500))
            return;

        _debugValueLast = _timerState.ElapsedMilliseconds;
        Service.PrintDebug(
            @$"[HookManager] Fishing State: {Service.EventFramework.FishingState}, LastStep: {_lastStep}");
    }

    private bool OnUseAction(IntPtr manager, ActionType actionType, uint actionId, GameObjectID targetId, uint a4,
        uint a5, uint a6, IntPtr a7)
    {
        try
        {
            if (actionType == ActionType.Action && Service.Configuration.PluginEnabled)
            {
                switch (actionId)
                {
                    case IDs.Actions.Cast:
                        if (PlayerRes.ActionTypeAvailable(actionId)) OnBeganFishing();
                        break;
                    case IDs.Actions.Mooch:
                    case IDs.Actions.Mooch2:
                        if (PlayerRes.ActionTypeAvailable(actionId)) OnBeganMooch();
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Service.PrintDebug(@$"[HookManager] Error: {e.Message}");
        }

        return _useActionHook!.Original(manager, actionType, actionId, targetId, a4, a5, a6, a7);
    }

    private void OnCatchUpdate(IntPtr module, uint fishId, bool large, ushort size, byte amount, byte level, byte unk7,
        byte unk8, byte unk9, byte unk10, byte unk11, byte unk12)
    {
        _catchHook!.Original(module, fishId, large, size, amount, level, unk7, unk8, unk9, unk10, unk11, unk12);

        // Check against collectibles.
        if (fishId > 500000)
            fishId -= 500000;

        OnCatch(fishId, amount);
    }

    // This is my stupid way of handling the counter for stop/quit fishing and bait/preset swap
    public static class FishingHelper
    {
        public static Dictionary<Guid, int> FishCount = new();
        public static List<Guid> FishPresetSwapped = new();
        public static List<Guid> FishBaitSwapped = new();

        public static void AddFishCount(Guid guid)
        {
            FishCount.TryAdd(guid, 0);
            FishCount[guid]++;

            GetFishCount(guid);
        }

        public static void AddBaitSwap(Guid guid)
        {
            if (!FishBaitSwapped.Contains(guid))
                FishBaitSwapped.Add(guid);
        }

        public static void AddPresetSwap(Guid guid)
        {
            if (!FishPresetSwapped.Contains(guid))
                FishPresetSwapped.Add(guid);
        }

        public static int GetFishCount(Guid guid)
        {
            return !FishCount.ContainsKey(guid) ? 0 : FishCount[guid];
        }

        public static bool SwappedBait(Guid guid)
        {
            return FishBaitSwapped.Any(g => g == guid);
        }

        public static bool SwappedPreset(Guid guid)
        {
            return FishPresetSwapped.Any(g => g == guid);
        }

        public static void RemoveId(Guid guid)
        {
            if (FishCount.ContainsKey(guid))
                FishCount.Remove(guid);

            if (FishPresetSwapped.Contains(guid))
                FishCount.Remove(guid);

            if (FishBaitSwapped.Contains(guid))
                FishCount.Remove(guid);
        }

        public static void Reset()
        {
            FishCount = new Dictionary<Guid, int>();
            FishPresetSwapped = [];
            FishBaitSwapped = [];
        }
    }
}