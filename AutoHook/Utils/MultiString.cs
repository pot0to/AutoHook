using AutoHook.Resources.Localization;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;

namespace AutoHook.Utils;

public readonly struct MultiString
{
    public static string ParseSeString(SeString? luminaString)
        => luminaString == null ? string.Empty : Dalamud.Game.Text.SeStringHandling.SeString.Parse(luminaString.RawData).TextValue;

    public static string GetStatusName(uint statusId)
    {
        return ParseSeString(Service.DataManager.GetExcelSheet<Status>()!.GetRow(statusId)?.Name);
    }
    
    public static string GetItemName(uint id)
    {
        if (id == 0)
            return UIStrings.None;
       
        var itemName = ParseSeString(Service.DataManager.GetExcelSheet<Item>()!.GetRow(id)?.Name);
        
        return itemName == string.Empty ? UIStrings.None : itemName;
    } 
    
    public static string GetItemName(int id)
    {
        return GetItemName((uint)id);
    } 
}
