using System.ComponentModel;
using StardewValley;
using StardewUI;
using StardewValley.ItemTypeDefinitions;
using PropertyChanged.SourceGenerator;
using xTile;
using StardewModdingAPI;
using System.Collections.Generic;
using StardewUI.Framework;
using System.ComponentModel.DataAnnotations;

namespace SproutSight;

// TODO: Base class for visitor for helper methods
// TODO: Diff levels of aggregation
// TODO: Remove generics :/
// TODO: Use accept
// TODO: Create templates in view
// TODO: Clean up averages
    // All up aggregation
// TODO: fix in/out tooltips
// TODO: Cache HighestOverallWalletGold
// Dimensions
    // Time (Day, Season, Year)
    // Function (min, max, sum, average)

internal partial class SproutSightViewModel
{
    public const int MaxRowHeight = 128;
    public const int MinRowHeight = 2;
    public const int ZeroRowHeight = 1;
    public const int RowWidth = 20;


    // Showing Today's Info
    public StardewDate Date = StardewDate.GetStardewDate();
    public List<TrackedItemStack> CurrentItems { get; internal set; } = new();
    public int TodayGoldIn;
    public int TodayGoldOut;
    public bool ShippedSomething => CurrentItems.Count > 0;
    public string TotalProceeds => $"Current Shipped: {FormatGoldNumber(CurrentItems.Select(item => item.StackCount * item.SalePrice).Sum())}";

    public TrackedDataAggregator TrackedDataAggregator {get; set; }

    public SproutSightViewModel(TrackedData trackedData) {
        TrackedDataAggregator = new(trackedData);
        TrackedDataAggregator.LoadAggregationAndViewVisitor();
    }
    
    // ================================ 
    // Tabs Stuff =====================
    // ================================

    public IReadOnlyList<ShipmentTabViewModel> AllTabs { get; } =
        Enum.GetValues<ShipmentTab>()
            .Select(tab => new ShipmentTabViewModel(tab, tab == ShipmentTab.Today))
            .ToArray();

    [Notify]
    private ShipmentTab selectedTab;

    public void SelectTab(ShipmentTab tab)
    {
        SelectedTab = tab;
        foreach (var tabViewModel in AllTabs)
        {
            tabViewModel.IsActive = tabViewModel.Value == tab;
        }
    }

    public static string FormatGoldNumber(int number)
    {
        return $"{number.ToString("N0")}g";
    }

}

internal partial class TrackedItemStack
{
    public string FormattedSale => $"({StackCount}x{SalePrice}g)";
}

internal class TrackedDataAggregator(TrackedData TrackedData)
{
    // We decompose the Elements here to make it easier to bind in the sml since dot operations are not allowed.

    public List<YearElement<List<SeasonElement<List<DayElement>>>>> WalletGrid { get; set; } = [];
    public string? WalletText { get; private set; } = "";
    public string? WalletTooltip { get; private set; } = "";

    public List<YearElement<List<SeasonElement<List<DayElement>>>>> ShippedGrid { get; set; } = [];
    public int ShippedTotal { get; private set; } = 0;
    
    public List<YearElement<List<SeasonElement<List<InOutElement>>>>> CashFlowGrid { get; set; } = [];
    public int CashFlowNetTotal { get; private set; } = 0;

    public int AverageGoldInWallet { get; private set; } = 0;

    public void LoadAggregationAndViewVisitor()
    {
        // Create the year data structure
        List<YearNode> yearNodes = [];
        RootNode rootNode = new(yearNodes);
        for (int year = 1; year <= Game1.year; year++)
        {
            List<SeasonNode> seasonNodes = [];
            yearNodes.Add(new YearNode(year, seasonNodes));
            foreach (Season season in Enum.GetValues(typeof(Season)))
            {
                List<DayNode> dayNodes = [];
                seasonNodes.Add(new SeasonNode(season, dayNodes));
                for (int day = 1; day <= 28; day++)
                {
                    var date = new StardewDate(year, season, day);
                    dayNodes.Add(new DayNode(date));
                }
            }
        }

        var walletGoldVisitor = new WalletGoldVisitor(TrackedData.GoldInOut, Operation.Average);
        var walletRoot = walletGoldVisitor.Visit(rootNode);
        WalletGrid = walletRoot.YearElements;
        WalletText = walletRoot.Text;
        WalletTooltip = walletRoot.Tooltip;

        // var shippedVisitor = new ShippedVisitor(TrackedData.ShippedData);
        // var shippedRoot = shippedVisitor.Visit(rootNode);
        // ShippedGrid = shippedRoot.Value;
        
        // var cashFlowVisitor = new CashFlowVisitor(TrackedData.GoldInOut);
        // var cashFlowRoot = cashFlowVisitor.Visit(rootNode);
        // CashFlowGrid = cashFlowRoot.Value;

        LogGridStructures();
    }

    public void LogGridStructures() 
    {
        Logging.Monitor.Log("=== Wallet Grid Structure ===", LogLevel.Info);
        foreach (var yearElements in WalletGrid)
        {
            Logging.Monitor.Log($"Year {yearElements.Year}:", LogLevel.Info);
            Logging.Monitor.Log($"  Text: {yearElements.Text}", LogLevel.Info);
            foreach (var seasonElements in yearElements.SeasonElements)
            {
                Logging.Monitor.Log($"  Season {seasonElements.Season}:", LogLevel.Info);
                Logging.Monitor.Log($"    Text: {seasonElements.Text}", LogLevel.Info);
                Logging.Monitor.Log($"    Days:", LogLevel.Info);
                foreach (var dayElements in seasonElements.DayElements)
                {
                    Logging.Monitor.Log($"      Date: {dayElements.Date}, Display: Layout={dayElements.Layout}, Tooltip={dayElements.Tooltip}, Tint={dayElements.Tint}", LogLevel.Info);
                }
            }
        }
    }
}


internal enum Operation
{
    Min,
    Max,
    Sum,
    Average,
}

internal abstract class BaseVisitor
{
    public Operation Operation;

    public int DoOperation(List<int> entries)
    {
        return Operation switch
        {
            Operation.Min => entries.Max(),
            Operation.Max => entries.Min(),
            Operation.Sum => entries.Sum(),
            Operation.Average => (int)Math.Round(entries.Sum() / (float)entries.Count),
            _ => 0
        };
    }
    public static string GetTint(Season season) 
    {
        return season switch
        {
            Season.Spring => "Green",
            Season.Summer => "Yellow",
            Season.Fall => "Brown",
            Season.Winter => "Blue",
            _ => "White"
        };
    }
}

internal class WalletGoldVisitor : BaseVisitor
{
    public Dictionary<StardewDate, GoldInOut> GoldInOut { get; set; } = [];
    public int HighestOverallWalletGold = 0;

    public WalletGoldVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation)
    {
        Operation = operation;
        GoldInOut = goldInOut;
        foreach (var date in GoldInOut.Keys) 
        {
            var daysWalledGold = GoldInOut.GetValueOrDefault(date)?.GoldInWallet ?? 0;
            HighestOverallWalletGold = Math.Max(HighestOverallWalletGold, daysWalledGold);
        }
    }

    public RootElement<List<YearElement<List<SeasonElement<List<DayElement>>>>>> Visit(RootNode root)
    {
        var entries = root.Years.Select(Visit).ToList();
        var walletGolds = entries.Select(entry => entry.Value).ToList();
        int aggregated = DoOperation(walletGolds);
        string tooltip = $"Overall {Operation}: {SproutSightViewModel.FormatGoldNumber(aggregated)}";
        string text = $"Wallet {Operation}";
        var element = new RootElement<List<YearElement<List<SeasonElement<List<DayElement>>>>>>(
                aggregated, entries, $"Overall {Operation} gold in wallet: {SproutSightViewModel.FormatGoldNumber(aggregated)}", null, tooltip);

        return element;
    }
    public YearElement<List<SeasonElement<List<DayElement>>>> Visit(YearNode year)
    {
        var entries = year.Seasons.Select(Visit).ToList();
        entries.Reverse();
        var walletGolds = entries.Select(entry => entry.Value).ToList();
        int aggregated = DoOperation(walletGolds);
        string tooltip = $"{Operation}: {SproutSightViewModel.FormatGoldNumber(aggregated)}";
        var walletGoldYearEntry = new YearElement<List<SeasonElement<List<DayElement>>>>(
                    year.Year, aggregated, entries, year + "", null, tooltip);
        return walletGoldYearEntry;

    }
    public SeasonElement<List<DayElement>> Visit(SeasonNode season)
    {
        var entries = season.Days.Select(Visit).ToList();
        var walletGolds = entries.Select(entry => entry.Value).ToList();
        int aggregated = DoOperation(walletGolds);
        string tooltip = $"{Operation}: {SproutSightViewModel.FormatGoldNumber(aggregated)}";
        var walletGoldSeasonEntry = new SeasonElement<List<DayElement>>(
                season.Season, aggregated, entries,
                season + "", null, tooltip, null, 
                season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);
        return walletGoldSeasonEntry;
    }
    public DayElement Visit(DayNode day)
    {
        var highest = HighestOverallWalletGold;
        var dayWalletGold = GoldInOut.GetValueOrDefault(day.Date)?.GoldInWallet ?? 0;
        var rowHeight = SproutSightViewModel.ZeroRowHeight;
        if (dayWalletGold > 0)
        {
            var scale = (float)dayWalletGold / highest;
            rowHeight = (int)Math.Round(Math.Max(SproutSightViewModel.MinRowHeight, scale * SproutSightViewModel.MaxRowHeight));
        }
        string layout = $"{SproutSightViewModel.RowWidth}px {rowHeight}px";
        string tooltip = $"{day.Date.Season}-{day.Date.Day}: {SproutSightViewModel.FormatGoldNumber(dayWalletGold)}";
        return new DayElement(day.Date, dayWalletGold, "", layout, tooltip, GetTint(day.Date.Season));
    }
}

// internal class WalletGoldVisitor : IDateVisitor<List<SeasonElement<List<DayElement>>>, List<DayElement>>
// {
//     public Dictionary<StardewDate, GoldInOut> GoldInOut { get; set; } = [];
//     public int HighestOverallWalletGold = 0;

//     public WalletGoldVisitor(Dictionary<StardewDate, GoldInOut> goldInOut)
//     {
//         GoldInOut = goldInOut;
//         foreach (var date in GoldInOut.Keys) 
//         {
//             var daysWalledGold = GoldInOut.GetValueOrDefault(date)?.GoldInWallet ?? 0;
//             HighestOverallWalletGold = Math.Max(HighestOverallWalletGold, daysWalledGold);
//         }
//     }

//     public RootElement<List<YearElement<List<SeasonElement<List<DayElement>>>>>> Visit(RootNode root)
//     {
//         var entries = root.Years.Select(Visit).ToList();
//         int average = (int)Math.Round(entries.Sum(entry => entry.Item2) / (float) entries.Count);
//         var yearEntries = entries.Select(entry => entry.Item1).ToList();
//         string tooltip = $"Avg: {SproutSightViewModel.FormatGoldNumber(average)}";
//         var element = new RootElement<List<YearElement<List<SeasonElement<List<DayElement>>>>>>(yearEntries, null, null, tooltip);
//         return element;
//     }
//     public (YearElement<List<SeasonElement<List<DayElement>>>>, int) Visit(YearNode year)
//     {
//         var entries = year.Seasons.Select(Visit).ToList();
//         entries.Reverse();
//         int average = (int)Math.Round(entries.Sum(entry => entry.Item2) / (float) entries.Count);
//         string tooltip = $"Avg: {SproutSightViewModel.FormatGoldNumber(average)}";
//         var walletSeasonsPerYear = entries.Select(entry => entry.Item1).ToList();
//         var walletGoldYearEntry = new YearElement<List<SeasonElement<List<DayElement>>>>(
//                     year.Year, walletSeasonsPerYear, year + "", null, tooltip);
//         return (walletGoldYearEntry, average);

//     }
//     public (SeasonElement<List<DayElement>>, int) Visit(SeasonNode season)
//     {
//         // Could use reduce to not iterate twice
//         var entries = season.Days.Select(Visit).ToList();
//         int average = (int)Math.Round(entries.Sum(entry => entry.Item2) / (float) entries.Count);
//         var walletGolds = entries.Select(entry => entry.Item1).ToList();
//         string tooltip = $"Avg: {SproutSightViewModel.FormatGoldNumber(average)}";
//         var walletGoldSeasonEntry = new SeasonElement<List<DayElement>>(
//                 season.Season, walletGolds,
//                 season + "", null, tooltip, null, 
//                 season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);
//         return (walletGoldSeasonEntry, average);
//     }
//     public (DayElement, int) Visit(DayNode day)
//     {
//         var highest = HighestOverallWalletGold;
//         var dayWalletGold = GoldInOut.GetValueOrDefault(day.Date)?.GoldInWallet ?? 0;
//         var rowHeight = SproutSightViewModel.ZeroRowHeight;
//         if (dayWalletGold > 0)
//         {
//             var scale = (float)dayWalletGold / highest;
//             rowHeight = (int)Math.Round(Math.Max(SproutSightViewModel.MinRowHeight, scale * SproutSightViewModel.MaxRowHeight));
//         }
//         string layout = $"{SproutSightViewModel.RowWidth}px {rowHeight}px";
//         string tooltip = $"{day.Date.Season}-{day.Date.Day}: {SproutSightViewModel.FormatGoldNumber(dayWalletGold)}";
//         return (new DayElement(day.Date, "", layout, tooltip, SproutSightViewModel.GetTint(day.Date.Season)), dayWalletGold);
//     }
// }

// internal class ShippedVisitor : IDateVisitor<List<SeasonElement<List<DayElement>>>, List<DayElement>>
// {
//     public Dictionary<StardewDate, GoldInOut> GoldInOut { get; set; } = [];
//     public int HighestOverallWalletGold = 0;
//     public Dictionary<StardewDate, List<TrackedItemStack>> ShippedData { get; set; } = [];
//     public Dictionary<StardewDate, int> TotalShippedGoldByDate { get; } = [];
//     public int HighestOverallTotal { get; private set; } = 0;

//     public ShippedVisitor(Dictionary<StardewDate, List<TrackedItemStack>> shippedData)
//     {
//         ShippedData = shippedData;
//         foreach (StardewDate date in shippedData.Keys)
//         {
//             int total = shippedData[date].Sum(d => d.TotalSalePrice);
//             TotalShippedGoldByDate[date] = TotalShippedGoldByDate.GetValueOrDefault(date, 0) + total;
//             HighestOverallTotal = Math.Max(HighestOverallTotal, total);
//         }
//     }

//     public RootElement<List<YearElement<List<SeasonElement<List<DayElement>>>>>> Visit(RootNode root)
//     {
//         var entries = root.Years.Select(Visit).ToList();
//         int sum = entries.Sum(entry => entry.Item2);
//         string tooltip = $"Total: {SproutSightViewModel.FormatGoldNumber(sum)}";
//         var yearEntries = entries.Select(entry => entry.Item1).ToList();
//         var element = new RootElement<List<YearElement<List<SeasonElement<List<DayElement>>>>>>(yearEntries, null, null, tooltip);
//         return element;

//     }
//     public (YearElement<List<SeasonElement<List<DayElement>>>>, int) Visit(YearNode year)
//     {
//         var entries = year.Seasons.Select(Visit).ToList();
//         entries.Reverse();
//         int sum = entries.Sum(entry => entry.Item2);
//         string tooltip = $"Total: {SproutSightViewModel.FormatGoldNumber(sum)}";
//         var shippedSeasonsPerYear = entries.Select(entry => entry.Item1).ToList();
//         var shippedGoldYearEntry = new YearElement<List<SeasonElement<List<DayElement>>>>(
//                     year.Year, shippedSeasonsPerYear, year + "", null, tooltip);
//         return (shippedGoldYearEntry, sum);

//     }
//     public (SeasonElement<List<DayElement>>, int) Visit(SeasonNode season)
//     {
//         // Could use reduce to not iterate twice
//         var entries = season.Days.Select(Visit).ToList();
//         int sum = entries.Sum(entry => entry.Item2);
//         var shippedGolds = entries.Select(entry => entry.Item1).ToList();
//         string tooltip = $"Total: {SproutSightViewModel.FormatGoldNumber(sum)}";
//         var shippedGoldSeasonEntry = new SeasonElement<List<DayElement>>(
//                 season.Season, shippedGolds,
//                 season + "", null, tooltip, null, 
//                 season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);
//         return (shippedGoldSeasonEntry, sum);
//     }
//     public (DayElement, int) Visit(DayNode day)
//     {
//         var highest = HighestOverallTotal;
//         var dayShippedGold = TotalShippedGoldByDate.GetValueOrDefault(day.Date);
//         var rowHeight = SproutSightViewModel.ZeroRowHeight;
//         if (dayShippedGold > 0)
//         {
//             var scale = (float)dayShippedGold / highest;
//             rowHeight = (int)Math.Round(Math.Max(SproutSightViewModel.MinRowHeight, scale * SproutSightViewModel.MaxRowHeight));
//         }
//         string layout = $"{SproutSightViewModel.RowWidth}px {rowHeight}px";
//         string tooltip = $"{day.Date.Season}-{day.Date.Day}: {SproutSightViewModel.FormatGoldNumber(dayShippedGold)}";
//         return (new DayElement(day.Date, "", layout, tooltip, SproutSightViewModel.GetTint(day.Date.Season)), dayShippedGold);
//     }
// }

// internal class CashFlowVisitor : IDateVisitor<List<SeasonElement<List<InOutElement>>>, List<InOutElement>>
// {
//     public Dictionary<StardewDate, GoldInOut> CashFlowByDate { get; set; } = [];
//     public int HighestOverallCashFlow { get; private set; } = 1;

//     public CashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut)
//     {
//         CashFlowByDate = goldInOut;
//         foreach (StardewDate date in goldInOut.Keys)
//         {
//             var inValue = goldInOut[date].In;
//             var outValue = Math.Abs(goldInOut[date].Out);
//             HighestOverallCashFlow = Math.Max(HighestOverallCashFlow, Math.Max(inValue, outValue));
//         }
//     }

//     public RootElement<List<YearElement<List<SeasonElement<List<InOutElement>>>>>> Visit(RootNode root)
//     {
//         var entries = root.Years.Select(Visit).ToList();
//         int netSum = entries.Sum(entry => entry.Item2);
//         string tooltip = $"Net Total: {SproutSightViewModel.FormatGoldNumber(netSum)}";
//         var yearEntries = entries.Select(entry => entry.Item1).ToList();
//         var element = new RootElement<List<YearElement<List<SeasonElement<List<InOutElement>>>>>>(yearEntries, null, null, tooltip);
//         return element;
//     }

//     public (YearElement<List<SeasonElement<List<InOutElement>>>>, int) Visit(YearNode year)
//     {
//         var entries = year.Seasons.Select(Visit).ToList();
//         entries.Reverse();
//         int netSum = entries.Sum(entry => entry.Item2);
//         string tooltip = $"Net Total: {SproutSightViewModel.FormatGoldNumber(netSum)}";
//         var cashFlowSeasonsPerYear = entries.Select(entry => entry.Item1).ToList();
//         var cashFlowYearEntry = new YearElement<List<SeasonElement<List<InOutElement>>>>(
//                     year.Year, cashFlowSeasonsPerYear, year + "", null, tooltip);
//         return (cashFlowYearEntry, netSum);
//     }

//     public (SeasonElement<List<InOutElement>>, int) Visit(SeasonNode season)
//     {
//         var entries = season.Days.Select(Visit).ToList();
//         int netSum = entries.Sum(entry => entry.Item2);
//         var cashFlowDays = entries.Select(entry => entry.Item1).ToList();
//         string tooltip = $"Net Total: {SproutSightViewModel.FormatGoldNumber(netSum)}";
//         var cashFlowSeasonEntry = new SeasonElement<List<InOutElement>>(
//                 season.Season, cashFlowDays,
//                 null, null, tooltip, null, 
//                 season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);
//         return (cashFlowSeasonEntry, netSum);
//     }

//     public (InOutElement, int) Visit(DayNode day)
//     {
//         var highest = HighestOverallCashFlow;
        
//         // Default values if date not found
//         int dayIn = 0;
//         int dayOut = 0;
        
//         if (CashFlowByDate.TryGetValue(day.Date, out var goldInOut))
//         {
//             dayIn = goldInOut.In;
//             dayOut = goldInOut.Out;
//         }
        
//         // Calculate in bar
//         var inScale = (float)dayIn / highest;
//         var inRowHeight = (int)Math.Round(Math.Max(SproutSightViewModel.MinRowHeight, inScale * SproutSightViewModel.MaxRowHeight));
//         var inLayout = $"{SproutSightViewModel.RowWidth}px {inRowHeight}px";
        
//         // Calculate out bar
//         var outScale = (float)Math.Abs(dayOut) / highest;
//         var outRowHeight = (int)Math.Round(Math.Max(SproutSightViewModel.MinRowHeight, outScale * SproutSightViewModel.MaxRowHeight));
//         var outLayout = $"{SproutSightViewModel.RowWidth}px {outRowHeight}px";
        
//         // Determine colors based on net value
//         bool positive = dayIn + dayOut >= 0;
//         var inTint = positive ? "#696969" : "#A9A9A9";  // Dark gray / Light gray
//         var outTint = positive ? "#F08080" : "#B22222";  // Light red / Dark red
        
//         // Create tooltip with all information
//         string tooltip = $"{day.Date.Season}-{day.Date.Day}\n" + 
//                 $"Net: {SproutSightViewModel.FormatGoldNumber(dayIn + dayOut)}\n" + 
//                 $"In: {SproutSightViewModel.FormatGoldNumber(dayIn)}\n" + 
//                 $"Out: {SproutSightViewModel.FormatGoldNumber(dayOut)}";
        
//         return (new InOutElement("", inLayout, tooltip, inTint, "", outLayout, tooltip, outTint), dayIn + dayOut);
//     }
// }

// If we moved the visitor to being traversed by accept instead, we could avoid multiple traversals...
// Nodes are the year traversal structure
interface IDateVisitor<YearType, SeasonType> {
//     public RootEntryElement<List<YearType>> Visit(RootNode root);
//     public (YearElement<YearType>, int) Visit(YearNode year);
//     public (SeasonElement<SeasonType>, int) Visit(SeasonNode season);
//     public (DayEntryElement, int) Visit(DayNode day);
}
internal record YearNode(int Year, List<SeasonNode> Seasons);
internal record SeasonNode(Season Season, List<DayNode> Days);
internal record DayNode(StardewDate Date);
internal record RootNode(List<YearNode> Years);

// Elements are the structure used in the view for display
internal record YearElement<T>(int Year, int Value, T SeasonElements, string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null);
internal record RootElement<T>(int Value, T YearElements, string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null);
internal record SeasonElement<T>(Season Season, int Value, T DayElements, string? Text = null, string? Layout = null, 
    string? Tooltip = null, string? Tint = null, bool IsSpring = false, bool IsSummer = false, bool IsFall = false, bool IsWinter = false);
internal record DayElement(StardewDate Date, int Value, string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null);
internal record InOutElement(string InText, string InLayout, string InTooltip, string InTint, string OutText, string OutLayout, string OutTooltip, string OutTint);

// ================================ 
// Tabs Stuff =====================
// ================================

internal enum ShipmentTab
{
    Today,
    Shipping,
    CashFlow,
    Wallet
}
internal partial class ShipmentTabViewModel : INotifyPropertyChanged
{
    [Notify]
    private bool _isActive;

    public Tuple<int, int, int, int> Margin => IsActive ? new(0, 0, -12, 0) : new(0, 0, 0, 0);

    public ShipmentTab Value { get; }

    public string Title {
        get
        {
            return Value switch 
            {
                ShipmentTab.Today => "Today",
                ShipmentTab.Shipping => "Shipping",
                ShipmentTab.CashFlow => "Cash Flow",
                ShipmentTab.Wallet => "Wallet",
                _ => Value.ToString()
            };
        }
    }

    public ShipmentTabViewModel(ShipmentTab value, bool isActive)
    {
        Value = value;
        _isActive = isActive;
    }
}
