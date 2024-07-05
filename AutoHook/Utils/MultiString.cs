using AutoHook.Resources.Localization;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using Action = Lumina.Excel.GeneratedSheets2.Action;

namespace AutoHook.Utils;

public readonly struct MultiString
{
    public static string ParseSeStringLumina(SeString? luminaString)
        => luminaString == null ? string.Empty : Dalamud.Game.Text.SeStringHandling.SeString.Parse(luminaString.RawData).TextValue;

    public static string GetStatusName(uint statusId)
    {
        return ParseSeStringLumina(Service.DataManager.GetExcelSheet<Status>()!.GetRow(statusId)?.Name);
    }
    
    public static string GetItemName(uint id)
    {
        if (id == 0)
            return UIStrings.None;
       
        var item = ParseSeStringLumina(Service.DataManager.GetExcelSheet<Item>()!.GetRow(id)?.Name);
        
        return item == string.Empty ? UIStrings.None : ParseSeStringLumina(Service.DataManager.GetExcelSheet<Item>()!.GetRow(id)?.Name);
    } 
    
    public static string GetItemName(int id)
    {
        return GetItemName((uint)id);
    } 
}
