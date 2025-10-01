using Dalamud.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace ChatTwo;

public static class Sheets
{
    public static readonly ExcelSheet<Item> ItemSheet;
    public static readonly ExcelSheet<World> WorldSheet;
    public static readonly ExcelSheet<Status> StatusSheet;
    public static readonly ExcelSheet<LogFilter> LogFilterSheet;
    public static readonly ExcelSheet<EventItem> EventItemSheet;
    public static readonly ExcelSheet<Completion> CompletionSheet;
    public static readonly ExcelSheet<TerritoryType> TerritorySheet;
    public static readonly ExcelSheet<TextCommand> TextCommandSheet;
    public static readonly ExcelSheet<EventItemHelp> EventItemHelpSheet;

    static Sheets()
    {
        ItemSheet = Plugin.DataManager.GetExcelSheet<Item>(ClientLanguage.ChineseSimplified);
        WorldSheet = Plugin.DataManager.GetExcelSheet<World>(ClientLanguage.ChineseSimplified);
        StatusSheet = Plugin.DataManager.GetExcelSheet<Status>(ClientLanguage.ChineseSimplified);
        EventItemSheet = Plugin.DataManager.GetExcelSheet<EventItem>(ClientLanguage.ChineseSimplified);
        LogFilterSheet = Plugin.DataManager.GetExcelSheet<LogFilter>(ClientLanguage.ChineseSimplified);
        CompletionSheet = Plugin.DataManager.GetExcelSheet<Completion>(ClientLanguage.ChineseSimplified);
        TerritorySheet = Plugin.DataManager.GetExcelSheet<TerritoryType>(ClientLanguage.ChineseSimplified);
        TextCommandSheet = Plugin.DataManager.GetExcelSheet<TextCommand>(ClientLanguage.ChineseSimplified);
        EventItemHelpSheet = Plugin.DataManager.GetExcelSheet<EventItemHelp>(ClientLanguage.ChineseSimplified);
    }

    public static bool IsInForay() =>
        TerritorySheet.TryGetRow(Plugin.ClientState.TerritoryType, out var row) &&
        row.TerritoryIntendedUse.RowId is 41 or 61;
}