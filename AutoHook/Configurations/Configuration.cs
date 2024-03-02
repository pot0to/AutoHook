using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using AutoHook.Classes;
using AutoHook.Configurations.old_config;
using AutoHook.Resources.Localization;
using AutoHook.Utils;

namespace AutoHook.Configurations;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 4;
    public string CurrentLanguage { get; set; } = @"en";

    public bool HideLocButton = false;

    [DefaultValue(true)] public bool PluginEnabled = true;

    public HookPresets HookPresets = new();

    public AutoGigConfig AutoGigConfig = new();

    public bool ShowDebugConsole = false;

    [DefaultValue(true)] public bool ShowChatLogs = true;

    public int DelayBetweenCastsMin = 600;
    public int DelayBetweenCastsMax = 1000;

    public int DelayBetweenHookMin = 0;
    public int DelayBetweenHookMax = 0;

    [DefaultValue(true)] public bool ShowStatusHeader = true;
    public bool ShowPresetsAsSidebar = false;

    public bool HideTabDescription = false;

    public bool SwapToButtons = false;
    public int SwapType;

    [DefaultValue(true)] public bool ResetAfkTimer = true;

    // old config
    public List<BaitPresetConfig> BaitPresetList = new();

    public void Save()
    {
        Service.PluginInterface!.SavePluginConfig(this);
    }

    public void UpdateVersion()
    {
        if (Version == 1)
        {
            /*
            Service.PluginLog.Debug(@"Updating to Version 2");
            PresetConfig temp = new(UIStrings.New_Preset);
            temp.AddListOfHook(CustomBait);
            BaitPresetList.Add(temp);
            */
            Version = 2;
        }
        else if (Version == 2)
        {
            try
            {
                foreach (var preset in BaitPresetList)
                {
                    var newPreset = ConvertOldPreset(preset);
                    if (newPreset != null)
                        HookPresets.CustomPresets.Add(newPreset);
                }

                Version = 3;
            }
            catch (Exception e)
            {
                Service.PrintDebug(@$"[Configuration] {e.Message}");
            }
        }
        else if (Version == 3)
        {
            Service.PrintDebug(@$"[Configuration] Updating to v4");
            HookPresets.DefaultPreset.RenamePreset(Service.GlobalPresetName);
            HookPresets.DefaultPreset.ConvertV3ToV4();

            HookPresets.SelectedPreset?.ConvertV3ToV4();

            foreach (var preset in HookPresets.CustomPresets)
            {
                preset.ConvertV3ToV4();
                Save();
            }

            Save();
            Version = 4;
        }
    }

    private static void SetFieldNewClass(HookConfig newOne, BaitConfig old)
    {
        var oldType = old.GetType();
        var newType = newOne.GetType();

        var oldFields = oldType.GetFields();
        var newFields = newType.GetFields();

        foreach (var sourceField in oldFields)
        {
            var targetField =
                newFields.FirstOrDefault(f => f.Name == sourceField.Name && f.FieldType == sourceField.FieldType);
            if (targetField != null)
            {
                var value = sourceField.GetValue(old);
                targetField.SetValue(newOne, value);
            }
        }
    }

    public void Initiate()
    {
        if (HookPresets.DefaultPreset.ListOfBaits.Count != 0)
            return;


        var bait = new BaitFishClass(UIStrings.All_Baits, 0);
        var mooch = new BaitFishClass(UIStrings.All_Mooches, 0);

        Service.PrintDebug(@$"Bait: {bait.Id} {bait.Name}, Mooch: {mooch.Id} {mooch.Name}");

        HookPresets.DefaultPreset.AddBaitConfig(new HookConfig(bait));
        HookPresets.DefaultPreset.AddMoochConfig(new HookConfig(mooch));
    }

    public static Configuration Load()
    {
        try
        {
            if (Service.PluginInterface.GetPluginConfig() is Configuration config)
            {
                config.Initiate();
                config.UpdateVersion();
                config.Save();
                return config;
            }

            config = new Configuration();
            config.Initiate();
            config.Save();
            return config;
        }
        catch (Exception e)
        {
            Service.PrintDebug(@$"[Configuration] {e.Message}");
            throw;
        }
    }

    public static void ResetConfig()
    {
    }

    // Got the export/import function from the UnknownX7's ReAction repo
    public static string ExportPreset(PresetConfig preset)
    {
        return CompressString(JsonConvert.SerializeObject(preset,
            new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }));
    }

    public static PresetConfig? ImportPreset(string import)
    {
        if (import.StartsWith(ExportPrefixV2))
        {
            var old = JsonConvert.DeserializeObject<BaitPresetConfig>(DecompressString(import),
                new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace });
            return ConvertOldPreset(old);
        }

        if (import.StartsWith(ExportPrefixV3))
        {
            var old = JsonConvert.DeserializeObject<OldPresetConfig>(DecompressString(import),
                new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace });

            return ConvertOldPresetV3(old);
        }

        var importActionStack = JsonConvert.DeserializeObject<PresetConfig>(DecompressString(import),
            new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace });
        return importActionStack;
    }

    [NonSerialized] private const string ExportPrefixV2 = "AH_";
    [NonSerialized] private const string ExportPrefixV3 = "AH3_";
    [NonSerialized] private const string ExportPrefixV4 = "AH4_";

    [NonSerialized] private static readonly List<string> ExportPrefixes =
    [
        ExportPrefixV2, ExportPrefixV3, ExportPrefixV4
    ];

    public static string CompressString(string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        using var ms = new MemoryStream();
        using (var gs = new GZipStream(ms, CompressionMode.Compress))
            gs.Write(bytes, 0, bytes.Length);

        return ExportPrefixV4 + Convert.ToBase64String(ms.ToArray());
    }

    public static string DecompressString(string s)
    {
        if (!ExportPrefixes.Any(s.StartsWith))
            throw new ApplicationException(UIStrings.DecompressString_Invalid_Import);

        var prefix = ExportPrefixes.First(s.StartsWith);
        var data = Convert.FromBase64String(s[prefix.Length..]);
        var lengthBuffer = new byte[4];
        Array.Copy(data, data.Length - 4, lengthBuffer, 0, 4);
        var uncompressedSize = BitConverter.ToInt32(lengthBuffer, 0);

        var buffer = new byte[uncompressedSize];
        using (var ms = new MemoryStream(data))
        {
            using var gzip = new GZipStream(ms, CompressionMode.Decompress);
            gzip.Read(buffer, 0, uncompressedSize);
        }

        return Encoding.UTF8.GetString(buffer);
    }

    public static string DecompressBase64(string base64)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64);
            using var compressedStream = new MemoryStream(bytes);
            using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            zipStream.CopyTo(resultStream);
            bytes = resultStream.ToArray();
            return Encoding.UTF8.GetString(bytes, 1, bytes.Length - 1);
        }
        catch (Exception e)
        {
            Service.PluginLog.Error(@$"Failed to DecompressBase64: {e.Message}");
            return "";
        }
    }

    private static PresetConfig? ConvertOldPreset(BaitPresetConfig? preset)
    {
        if (preset == null)
            return null;

        var filteredBaits = new List<HookConfig>();
        var filteredMooch = new List<HookConfig>();
        foreach (var old in preset.ListOfBaits)
        {
            var matchingBait = GameRes.Baits.FirstOrDefault(b => b.Name == old.BaitName);
            var matchingFish = GameRes.Fishes.FirstOrDefault(f => f.Name == old.BaitName);

            if (matchingBait != null)
            {
                var newOne = new HookConfig(matchingBait);
                SetFieldNewClass(newOne, old);
                filteredBaits.Add(newOne);
            }
            else if (matchingFish != null)
            {
                var newOne = new HookConfig(matchingFish);
                SetFieldNewClass(newOne, old);
                filteredMooch.Add(newOne);
            }
        }

        PresetConfig newPreset = new(@$"[Old Version] {preset.PresetName}");
        newPreset.ListOfBaits = filteredBaits;
        newPreset.ListOfMooch = filteredMooch;
        return newPreset;
    }

    private static PresetConfig? ConvertOldPresetV3(OldPresetConfig? old)
    {
        if (old == null)
            return null;

        var newPreset = new PresetConfig(old.PresetName);

        foreach (var bait in old.ListOfBaits)
        {
            bait.ConvertV3ToV4();
            var newBait = new HookConfig(bait.BaitFish);

            newBait.Enabled = bait.Enabled;
            newBait.NormalHook = bait.NormalHook;
            newBait.IntuitionHook = bait.IntuitionHook;
            newBait.IntuitionHook.UseCustomStatusHook = bait.UseCustomIntuitionHook;

            newPreset.AddBaitConfig(newBait);
        }

        foreach (var mooch in old.ListOfMooch)
        {
            mooch.ConvertV3ToV4();
            var newMooch = new HookConfig(mooch.BaitFish);

            newMooch.Enabled = mooch.Enabled;
            newMooch.NormalHook = mooch.NormalHook;
            newMooch.IntuitionHook = mooch.IntuitionHook;
            newMooch.IntuitionHook.UseCustomStatusHook = mooch.UseCustomIntuitionHook;

            newPreset.AddMoochConfig(newMooch);
        }

        newPreset.ListOfFish = old.ListOfFish;
        newPreset.ExtraCfg = old.ExtraCfg;
        newPreset.AutoCastsCfg = old.AutoCastsCfg;

        return newPreset;
    }
}