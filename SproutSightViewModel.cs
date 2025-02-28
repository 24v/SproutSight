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
using Microsoft.VisualBasic;

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
// Update docs in sml
// Two scrollables in sml??
// Constants not used everyhwere (what is teh convention?)
// Only load for current page?
// TODO: fix tense in FirstPass
// TODO: HighestOverallDayTotal naming

internal partial class SproutSightViewModel
{
    public const int RowWidth = 20;
    public const int MaxRowHeight = 128;
    public const int MinRowHeight = 2;
    public const int ZeroDataRowHeight = 1;


    // Showing Today's Info
    public StardewDate Date = StardewDate.GetStardewDate();
    public List<TrackedItemStack> CurrentItems { get; internal set; } = new();
    public int TodayGoldIn;
    public int TodayGoldOut;
    public bool ShippedSomething => CurrentItems.Count > 0;
    public string TotalProceeds => $"Current Shipped: {FormatGoldNumber(CurrentItems.Select(item => item.StackCount * item.SalePrice).Sum())}";

    private TrackedData trackedData;

    [Notify]
    private TrackedDataAggregator _trackedDataAggregator;

    public SproutSightViewModel(TrackedData trackedData) {
        this.trackedData = trackedData;
        _trackedDataAggregator = new(trackedData, SelectedOperation);
        _trackedDataAggregator.LoadAggregationAndViewVisitor();
    }

    // ================================ 
    // UI Controls ====================
    // ================================

    public Operation[] Operations { get; } = Enum.GetValues<Operation>();
    [Notify] 
    private Operation _selectedOperation = Operation.Sum;
    private void OnSelectedOperationChanged(Operation oldValue, Operation newValue)
    {
        var newAggregator = new TrackedDataAggregator(trackedData, newValue);
        newAggregator.LoadAggregationAndViewVisitor();
        TrackedDataAggregator = newAggregator;
    }
    
    [Notify] private Period selectedPeriod = Period.Day;
    public Period[] Periods { get; } = Enum.GetValues<Period>();

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

internal class TrackedDataAggregator(TrackedData TrackedData, Operation Operation)
{
    // We decompose the Elements here to make it easier to bind in the sml since dot operations are not allowed.

    public List<YearElement<List<SeasonElement<List<DayElement>>>>> WalletGrid { get; set; } = [];
    public string? WalletText { get; private set; } = "";
    public string? WalletTooltip { get; private set; } = "";

    public List<YearElement<List<SeasonElement<List<DayElement>>>>> ShippedGrid { get; set; } = [];
    public int ShippedTotal { get; private set; } = 0;
    public string? ShippedText { get; private set; } = "";
    public string? ShippedTooltip { get; private set; } = "";

    public List<YearElement<List<SeasonElement<List<InOutElement>>>>> CashFlowGrid { get; set; } = [];
    public int CashFlowNetTotal { get; private set; } = 0;
    public string? CashFlowText { get; private set; } = "";
    public string? CashFlowTooltip { get; private set; } = "";

    public void LoadAggregationAndViewVisitor()
    {
        Logging.Monitor.Log("Starting LoadAggregationAndViewVisitor", LogLevel.Debug);
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

        // var walletGoldVisitor = new WalletGoldVisitor(TrackedData.GoldInOut, Operation);
        // var walletRoot = walletGoldVisitor.Visit(rootNode);
        // WalletGrid = walletRoot.YearElements;
        // WalletText = walletRoot.Text;
        // WalletTooltip = walletRoot.Tooltip;

        var shippedFirstPassVisitor = new FirstPassVisitor(
                TrackedData.ShippedData, 
                Operation);
        Logging.Monitor.Log($"Starting FirstPassVisitor with operation: {Operation}, ShippedData count: {TrackedData.ShippedData.Count}", LogLevel.Debug);
        shippedFirstPassVisitor.Visit(rootNode);
        Logging.Monitor.Log($"FirstPassVisitor results - Day: {shippedFirstPassVisitor.HighestDayValue}, Season: {shippedFirstPassVisitor.HighestSeasonValue}, Year: {shippedFirstPassVisitor.HighestYearValue}", LogLevel.Debug);

        var shippedVisitor = new ShippedVisitor(
                TrackedData.ShippedData, 
                Operation, 
                shippedFirstPassVisitor.HighestDayValue, 
                shippedFirstPassVisitor.HighestSeasonValue, 
                shippedFirstPassVisitor.HighestYearValue);

        var shippedRoot = shippedVisitor.Visit(rootNode);
        Logging.Monitor.Log($"ShippedVisitor.Visit returned root with value={shippedRoot.Value}, entries count={shippedRoot.YearElements.Count}", LogLevel.Debug);

        ShippedGrid = shippedRoot.YearElements;
        ShippedTotal = shippedRoot.Value;
        ShippedText = shippedRoot.Text;
        ShippedTooltip = shippedRoot.Tooltip;
        Logging.Monitor.Log($"ShippedVisitor results - Total: {ShippedTotal}, Text: {ShippedText}", LogLevel.Debug);

        // var cashFlowVisitor = new CashFlowVisitor(TrackedData.GoldInOut, Operation);
        // var cashFlowRoot = cashFlowVisitor.Visit(rootNode);
        // CashFlowGrid = cashFlowRoot.YearElements;
        // CashFlowNetTotal = cashFlowRoot.Value;
        // CashFlowText = cashFlowRoot.Text;
        // CashFlowTooltip = cashFlowRoot.Tooltip;

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

internal enum Period
{
    Day,
    Season,
    Year,
}

internal abstract class BaseVisitor
{
    public Operation Operation;

    public int DoOperation(List<int> entries)
    {
        return Operation switch
        {
            Operation.Min => entries.Min(),
            Operation.Max => entries.Max(),
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
    
    // Helper methods for scale calculations
    protected static int CalculateRowHeight(int value, int highest)
    {
        var rowHeight = SproutSightViewModel.ZeroDataRowHeight;
        if (value > 0)
        {
            var scale = (float)value / highest;
            rowHeight = (int)Math.Round(Math.Max(SproutSightViewModel.MinRowHeight, scale * SproutSightViewModel.MaxRowHeight));
        }
        return rowHeight;
    }
    
    protected static string FormatLayout(int rowHeight)
    {
        return $"{SproutSightViewModel.RowWidth}px {rowHeight}px";
    }
}


internal class FirstPassVisitor(Dictionary<StardewDate, List<TrackedItemStack>> ShippedData, Operation Operation) 
{
    public int HighestDayValue { get; private set; } = 0;
    public int HighestSeasonValue { get; private set; } = 0;
    public int HighestYearValue { get; private set; } = 0;

    public void Visit(RootNode root)
    {
        root.Years.ForEach(e => Visit(e));
    }

    public int Visit(YearNode year)
    {
        int aggregateOfSeasons = DoOperation(year.Seasons.Select(Visit).ToList());
        HighestYearValue = Math.Max(HighestYearValue, aggregateOfSeasons);
        return aggregateOfSeasons;
    }

    public int Visit(SeasonNode season)
    {
        int aggregateOfDays = DoOperation(season.Days.Select(Visit).ToList());
        HighestSeasonValue = Math.Max(HighestSeasonValue, aggregateOfDays);
        return aggregateOfDays;
    }

    public int Visit(DayNode day)
    {
        if (ShippedData.TryGetValue(day.Date, out var items))
        {
            var summed = items.Select(item => item.TotalSalePrice).Sum();
            HighestDayValue = Math.Max(HighestDayValue, summed);
            return summed;
        }
        return 0;
    }

    public int DoOperation(List<int> entries)
    {
        return Operation switch
        {
            Operation.Min => entries.Min(),
            Operation.Max => entries.Max(),
            Operation.Sum => entries.Sum(),
            Operation.Average => (int)Math.Round(entries.Sum() / (float)entries.Count),
            _ => 0
        };
    }
}

internal class ShippedVisitor : BaseVisitor
{
    public Dictionary<StardewDate, List<TrackedItemStack>> ShippedData { get; set; } = [];
    public Dictionary<StardewDate, int> TotalShippedGoldByDate { get; } = [];
    
    public int HighestOverallDayTotal { get; private set; }
    public int HighestOverallSeasonTotal { get; private set; }
    public int HighestOverallYearTotal { get; private set; }

    public ShippedVisitor(Dictionary<StardewDate, List<TrackedItemStack>> shippedData, Operation operation, 
                          int highestDay, int highestSeason, int highestYear)
    {
        Operation = operation;
        ShippedData = shippedData;
        HighestOverallDayTotal = Math.Max(1, highestDay);
        HighestOverallSeasonTotal = Math.Max(1, highestSeason);
        HighestOverallYearTotal = Math.Max(1, highestYear);
        TotalShippedGoldByDate = ShippedData.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Sum(item => item.TotalSalePrice)
        );
        Logging.Monitor.Log($"Populated TotalShippedGoldByDate with {TotalShippedGoldByDate.Count} entries", LogLevel.Debug);
    }

    public RootElement<List<YearElement<List<SeasonElement<List<DayElement>>>>>> Visit(RootNode root)
    {
        var entries = root.Years.Select(Visit).ToList();
        entries.Reverse();
        var shippedGolds = entries.Select(entry => entry.Value).ToList();
        int aggregated = DoOperation(shippedGolds);
        string tooltip = $"Shipped Overall {Operation}: {SproutSightViewModel.FormatGoldNumber(aggregated)}";
        string text = tooltip;
        var element = new RootElement<List<YearElement<List<SeasonElement<List<DayElement>>>>>>(
                aggregated, entries, $"Overall {Operation} Shipped Gold: {SproutSightViewModel.FormatGoldNumber(aggregated)}", null, tooltip);

        return element;
    }
    
    public YearElement<List<SeasonElement<List<DayElement>>>> Visit(YearNode year)
    {
        var entries = year.Seasons.Select(Visit).ToList();
        entries.Reverse();
        var shippedGolds = entries.Select(entry => entry.Value).ToList();
        int aggregated = DoOperation(shippedGolds);
        string tooltip = $"{Operation}: {SproutSightViewModel.FormatGoldNumber(aggregated)}";
        var highest = HighestOverallYearTotal;
        int rowHeight = CalculateRowHeight(aggregated, highest);
        string layout = FormatLayout(rowHeight);
        var shippedGoldYearEntry = new YearElement<List<SeasonElement<List<DayElement>>>>(
                    year.Year, aggregated, entries, year + "", layout, tooltip);
        return shippedGoldYearEntry;
    }
    
    public SeasonElement<List<DayElement>> Visit(SeasonNode season)
    {
        var entries = season.Days.Select(Visit).ToList();
        var shippedGolds = entries.Select(entry => entry.Value).ToList();
        int aggregated = DoOperation(shippedGolds);
        string tooltip = $"{Operation}: {SproutSightViewModel.FormatGoldNumber(aggregated)}";
        var highest = HighestOverallSeasonTotal;
        int rowHeight = CalculateRowHeight(aggregated, highest);
        string layout = FormatLayout(rowHeight);
        var shippedGoldSeasonEntry = new SeasonElement<List<DayElement>>(
                season.Season, aggregated, entries,
                season + "", layout, tooltip, GetTint(season.Season), 
                season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);
        return shippedGoldSeasonEntry;
    }
    
    public DayElement Visit(DayNode day)
    {
        Logging.Monitor.Log($"Visit: {day.Date}, highest={HighestOverallDayTotal}, dayShippedGold={TotalShippedGoldByDate.GetValueOrDefault(day.Date)}, rowHeight={CalculateRowHeight(TotalShippedGoldByDate.GetValueOrDefault(day.Date), HighestOverallDayTotal)}", LogLevel.Debug);
        var highest = HighestOverallDayTotal;
        var dayShippedGold = TotalShippedGoldByDate.GetValueOrDefault(day.Date);
        int rowHeight = CalculateRowHeight(dayShippedGold, highest);
        string layout = FormatLayout(rowHeight);
        string tooltip = $"{day.Date.Season}-{day.Date.Day}: {SproutSightViewModel.FormatGoldNumber(dayShippedGold)}";
        return new DayElement(day.Date, dayShippedGold, "", layout, tooltip, GetTint(day.Date.Season));
    }
}

internal class WalletGoldVisitor : BaseVisitor
{
    public Dictionary<StardewDate, GoldInOut> GoldInOut { get; set; } = [];
    public int HighestOverallWalletGold = 1;

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
        entries.Reverse();
        var walletGolds = entries.Select(entry => entry.Value).ToList();
        int aggregated = DoOperation(walletGolds);
        string tooltip = $"Overall {Operation}: {SproutSightViewModel.FormatGoldNumber(aggregated)}";
        string text = $"Wallet {Operation}";
        var element = new RootElement<List<YearElement<List<SeasonElement<List<DayElement>>>>>>(
                aggregated, entries, $"Overall {Operation} Gold in Wallet: {SproutSightViewModel.FormatGoldNumber(aggregated)}", null, tooltip);

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
        int rowHeight = CalculateRowHeight(dayWalletGold, highest);
        string layout = FormatLayout(rowHeight);
        string tooltip = $"{day.Date.Season}-{day.Date.Day}: {SproutSightViewModel.FormatGoldNumber(dayWalletGold)}";
        return new DayElement(day.Date, dayWalletGold, "", layout, tooltip, GetTint(day.Date.Season));
    }
}



internal class CashFlowVisitor : BaseVisitor
{
    public Dictionary<StardewDate, GoldInOut> CashFlowByDate { get; set; } = [];
    public int HighestOverallCashFlow { get; private set; } = 1;

    public CashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation)
    {
        Operation = operation;
        CashFlowByDate = goldInOut;
        foreach (StardewDate date in goldInOut.Keys)
        {
            if (goldInOut.TryGetValue(date, out var flow))
            {
                var inValue = flow.In;
                var outValue = Math.Abs(flow.Out);
                HighestOverallCashFlow = Math.Max(HighestOverallCashFlow, Math.Max(inValue, outValue));
            }
        }
    }

    public RootElement<List<YearElement<List<SeasonElement<List<InOutElement>>>>>> Visit(RootNode root)
    {
        var entriesWithValues = root.Years.Select(Visit).ToList();
        var entries = entriesWithValues.Select(e => e.Item1).ToList();
        entries.Reverse();
        var cashFlowInValues = entriesWithValues.Select(e => e.Item2.Item1).ToList();
        var cashFlowOutValues = entriesWithValues.Select(e => e.Item2.Item2).ToList();
        
        int aggregatedIn = DoOperation(cashFlowInValues);
        int aggregatedOut = DoOperation(cashFlowOutValues);
        int netValue = aggregatedIn + aggregatedOut;
        
        string tooltip = $"Overall {Operation}:\n" + 
                         $"Net: {SproutSightViewModel.FormatGoldNumber(netValue)}\n" + 
                         $"In: {SproutSightViewModel.FormatGoldNumber(aggregatedIn)}\n" + 
                         $"Out: {SproutSightViewModel.FormatGoldNumber(aggregatedOut)}";
        
        string text = $"Cash Flow {Operation}";
        var element = new RootElement<List<YearElement<List<SeasonElement<List<InOutElement>>>>>>(
                netValue, entries, $"Overall {Operation} Cash Flow", null, tooltip);

        return element;
    }
    
    public (YearElement<List<SeasonElement<List<InOutElement>>>>, (int, int)) Visit(YearNode year)
    {
        var entriesWithValues = year.Seasons.Select(Visit).ToList();
        var entries = entriesWithValues.Select(e => e.Item1).ToList();
        entries.Reverse();
        var cashFlowInValues = entriesWithValues.Select(e => e.Item2.Item1).ToList();
        var cashFlowOutValues = entriesWithValues.Select(e => e.Item2.Item2).ToList();
        
        int aggregatedIn = DoOperation(cashFlowInValues);
        int aggregatedOut = DoOperation(cashFlowOutValues);
        int netValue = aggregatedIn + aggregatedOut;
        
        string tooltip = $"{Operation}:\n" + 
                         $"Net: {SproutSightViewModel.FormatGoldNumber(netValue)}\n" + 
                         $"In: {SproutSightViewModel.FormatGoldNumber(aggregatedIn)}\n" + 
                         $"Out: {SproutSightViewModel.FormatGoldNumber(aggregatedOut)}";
        
        var cashFlowYearEntry = new YearElement<List<SeasonElement<List<InOutElement>>>>(
                    year.Year, netValue, entries, year + "", null, tooltip);
        return (cashFlowYearEntry, (aggregatedIn, aggregatedOut));
    }
    
    public (SeasonElement<List<InOutElement>>, (int, int)) Visit(SeasonNode season)
    {
        var entriesWithValues = season.Days.Select(Visit).ToList();
        var entries = entriesWithValues.Select(e => e.Item1).ToList();
        var cashFlowInValues = entriesWithValues.Select(e => e.Item2.Item1).ToList();
        var cashFlowOutValues = entriesWithValues.Select(e => e.Item2.Item2).ToList();
        
        int aggregatedIn = DoOperation(cashFlowInValues);
        int aggregatedOut = DoOperation(cashFlowOutValues);
        int netValue = aggregatedIn + aggregatedOut;
        
        string tooltip = $"{Operation}:\n" + 
                         $"Net: {SproutSightViewModel.FormatGoldNumber(netValue)}\n" + 
                         $"In: {SproutSightViewModel.FormatGoldNumber(aggregatedIn)}\n" + 
                         $"Out: {SproutSightViewModel.FormatGoldNumber(aggregatedOut)}";
        
        var cashFlowSeasonEntry = new SeasonElement<List<InOutElement>>(
                season.Season, netValue, entries,
                season + "", null, tooltip, null, 
                season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);
        return (cashFlowSeasonEntry, (aggregatedIn, aggregatedOut));
    }
    
    public (InOutElement, (int, int)) Visit(DayNode day)
    {
        var highest = HighestOverallCashFlow;
        
        // Default values if date not found
        int dayIn = 0;
        int dayOut = 0;
        
        if (CashFlowByDate.TryGetValue(day.Date, out var goldInOut))
        {
            dayIn = goldInOut.In;
            dayOut = goldInOut.Out;
        }
        
        // Calculate in bar
        int inRowHeight = CalculateRowHeight(dayIn, highest);
        string inLayout = FormatLayout(inRowHeight);
        
        // Calculate out bar
        int outRowHeight = CalculateRowHeight(Math.Abs(dayOut), highest);
        string outLayout = FormatLayout(outRowHeight);
        
        // Determine colors based on net value
        bool positive = dayIn + dayOut >= 0;
        var inTint = positive ? "#696969" : "#A9A9A9";  // Dark gray / Light gray
        var outTint = positive ? "#F08080" : "#B22222";  // Light red / Dark red
        
        // Create tooltip with all information
        string tooltip = $"{day.Date.Season}-{day.Date.Day}\n" + 
                $"Net: {SproutSightViewModel.FormatGoldNumber(dayIn + dayOut)}\n" + 
                $"In: {SproutSightViewModel.FormatGoldNumber(dayIn)}\n" + 
                $"Out: {SproutSightViewModel.FormatGoldNumber(dayOut)}";
        
        return (new InOutElement("", inLayout, tooltip, inTint, "", outLayout, tooltip, outTint), (dayIn, dayOut));
    }
}

// If we moved the visitor to being traversed by accept instead, we could avoid multiple traversals...
// Nodes are the year traversal structure
// interface IDateVisitor<YearType, SeasonType> {
//     public RootEntryElement<List<YearType>> Visit(RootNode root);
//     public (YearElement<YearType>, int) Visit(YearNode year);
//     public (SeasonElement<SeasonType>, int) Visit(SeasonNode season);
//     public (DayEntryElement, int) Visit(DayNode day);
// }
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
