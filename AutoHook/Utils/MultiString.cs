using Lumina.Excel.GeneratedSheets;
using Lumina.Text;

namespace AutoHook.Utils;

public readonly struct MultiString
{
    public static string ParseSeStringLumina(SeString? luminaString)
        => luminaString == null ? string.Empty : Dalamud.Game.Text.SeStringHandling.SeString.Parse(luminaString.RawData).TextValue;

    public static string GetStatusName(uint statusId)
    {
        return ParseSeStringLumina(Service.DataManager.GetExcelSheet<Status>()!.GetRow(statusId)?.Name);
    }
}
