
namespace SproutSight.Display;
internal static class DisplayHelper
{
    public const int RowWidth = 20;
    public const int MaxRowHeight = 128;
    public const int MinRowHeight = 3;
    public const int ZeroDataRowHeight = 2;
    public const int DefaultNumYearsSelected = 5;
    public const string TodayTint = "#000000";
    public const string FutureTint = "#959595";
    public const string YearTint = "#40FC05";
    public const string CashFlowInTint = "#696969";
    public const string CashFlowOutTint = "#B22222"; 
    public static string FormatGoldNumber(int number) => $"{number:N0}g";

    public static string GetTint(Season season) 
    {
        return season switch
        {
            Season.Spring => "#2CA014",
            Season.Summer => "#FEFF17",
            Season.Fall => "#D13400",
            Season.Winter => "#A9F0FF",
            _ => "White"
        };
    }
    
    public static int CalculateRowHeight(int value, int highest)
    {
        var rowHeight = ZeroDataRowHeight;
        if (value > 0)
        {
            var scale = (float)value / highest;
            rowHeight = (int)Math.Round(Math.Max(MinRowHeight, scale * MaxRowHeight));
        }
        return rowHeight;
    }
    
    public static string FormatLayout(int rowHeight)
    {
        return $"{RowWidth}px {rowHeight}px";
    }
}