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
using StardewValley.GameData.Pets;

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
// TODO: Min per day / max per day / hover
// TODO: Log Levels
// TODO: Autoformat linter
// TODO: Better colors
// TODO: Fix cursor colors
// TODO: Use lambad instead of inherited classes
// TODO: Why does it not use the more specific icon?
// TODO: fix BaseFirstPassVisitor ai trash
// TODO: Memoize

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
    public static string FormatGoldNumber(int number)
    {
        return $"{number.ToString("N0")}g";
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
    
    [Notify] private Period selectedPeriod = Period.All;
    public Period[] Periods { get; } = Enum.GetValues<Period>();

    // ================================ 
    // Tabs Stuff =====================
    // ================================

    public IReadOnlyList<ShipmentTabViewModel> AllTabs { get; } =
        Enum.GetValues<ShipmentTab>()
            .Select(tab => new ShipmentTabViewModel(tab, tab == ShipmentTab.Today))
            .ToArray();

    [Notify]
    private ShipmentTab _selectedTab;

    public void SelectTab(ShipmentTab tab)
    {
        SelectedTab = tab;
        foreach (var tabViewModel in AllTabs)
        {
            tabViewModel.IsActive = tabViewModel.Value == tab;
        }
    }

    private void OnSelectedTabChanged(ShipmentTab oldValue, ShipmentTab newValue)
    {
        // Set appropriate default operation based on tab
        if (newValue == ShipmentTab.Wallet)
        {
            SelectedOperation = Operation.End;
        }
        else
        {
            SelectedOperation = Operation.Sum;
        }
        
        // Refresh data with the new operation
        OnSelectedOperationChanged(SelectedOperation, SelectedOperation);
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

        var shippedFirstPassVisitor = FirstPassVisitors.CreateShippedVisitor(
                TrackedData.ShippedData, 
                Operation);
        Logging.Monitor.Log($"Starting FirstPassVisitor with operation: {Operation}, ShippedData count: {TrackedData.ShippedData.Count}", LogLevel.Debug);
        shippedFirstPassVisitor.Visit(rootNode);
        Logging.Monitor.Log($"FirstPassVisitor results - Day: {shippedFirstPassVisitor.HighestDayValue}, Season: {shippedFirstPassVisitor.HighestSeasonValue}, Year: {shippedFirstPassVisitor.HighestYearValue}", LogLevel.Debug);
        var shippedVisitor = Visitors.CreateShippedVisitor(
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

        var walletGoldFirstPassVisitor = FirstPassVisitors.CreateWalletGoldVisitor(
                TrackedData.GoldInOut, 
                Operation);
        walletGoldFirstPassVisitor.Visit(rootNode);
        var walletGoldVisitor = Visitors.CreateWalletGoldVisitor(
                TrackedData.GoldInOut, 
                Operation,
                walletGoldFirstPassVisitor.HighestDayValue, 
                walletGoldFirstPassVisitor.HighestSeasonValue, 
                walletGoldFirstPassVisitor.HighestYearValue);
        var walletRoot = walletGoldVisitor.Visit(rootNode);
        WalletGrid = walletRoot.YearElements;
        WalletText = walletRoot.Text;
        WalletTooltip = walletRoot.Tooltip;

        var cashFlowFirstPassVisitor = FirstPassVisitors.CreateCashFlowVisitor(
                TrackedData.GoldInOut, 
                Operation);
        cashFlowFirstPassVisitor.Visit(rootNode);
        var cashFlowVisitor = Visitors.CreateCashFlowVisitor(
                TrackedData.GoldInOut, 
                Operation,
                cashFlowFirstPassVisitor.HighestDayValue, 
                cashFlowFirstPassVisitor.HighestSeasonValue, 
                cashFlowFirstPassVisitor.HighestYearValue,
                cashFlowFirstPassVisitor.HighestDayInValue,
                cashFlowFirstPassVisitor.HighestSeasonInValue,
                cashFlowFirstPassVisitor.HighestYearInValue,
                cashFlowFirstPassVisitor.HighestDayOutValue,
                cashFlowFirstPassVisitor.HighestSeasonOutValue,
                cashFlowFirstPassVisitor.HighestYearOutValue);
        var cashFlowRoot = cashFlowVisitor.VisitCashFlow(rootNode);
        CashFlowGrid = cashFlowRoot.YearElements;
        CashFlowNetTotal = cashFlowRoot.Value;
        CashFlowText = cashFlowRoot.Text;
        CashFlowTooltip = cashFlowRoot.Tooltip;

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
    End  // New operation to get the last value in a period
}

internal enum Period
{
    All,
    Season,
    Year,
}

/// <summary>
/// For getting the highest values which will be used in the second pass for creating bar scales
/// </summary>
/// <param name="Operation">Which operation to perform</param>
internal class BaseFirstPassVisitor(Operation Operation, Func<StardewDate, int> getDayValue) 
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
        var dayValue = getDayValue(day.Date);
        HighestDayValue = Math.Max(HighestDayValue, dayValue);
        return dayValue;
    }

    public int DoOperation(List<int> entries)
    {
        return Operation switch
        {
            Operation.Min => entries.Min(),
            Operation.Max => entries.Max(),
            Operation.Sum => entries.Sum(),
            Operation.Average => (int)Math.Round(entries.Sum() / (float)entries.Count),
            Operation.End => entries.Last(),
            _ => 0
        };
    }
}

internal static class FirstPassVisitors
{
    public static BaseFirstPassVisitor CreateShippedVisitor(Dictionary<StardewDate, List<TrackedItemStack>> shippedData, Operation operation)
    {
        return new BaseFirstPassVisitor(operation, date => 
        {
            if (shippedData.TryGetValue(date, out var items))
            {
                return items.Select(item => item.TotalSalePrice).Sum();
            }
            return 0;
        });
    }

    public static BaseFirstPassVisitor CreateWalletGoldVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation)
    {
        return new BaseFirstPassVisitor(operation, date => goldInOut.GetValueOrDefault(date)?.GoldInWallet ?? 0);
    }

    public static CashFlowFirstPassVisitor CreateCashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation)
    {
        return new CashFlowFirstPassVisitor(goldInOut, operation);
    }
}

internal class CashFlowFirstPassVisitor : BaseFirstPassVisitor
{
    public Dictionary<StardewDate, GoldInOut> CashFlowByDate { get; set; } = [];
    
    // Highest values for income
    public int HighestDayInValue { get; private set; } = 1;
    public int HighestSeasonInValue { get; private set; } = 1;
    public int HighestYearInValue { get; private set; } = 1;
    
    // Highest values for expenses
    public int HighestDayOutValue { get; private set; } = 1;
    public int HighestSeasonOutValue { get; private set; } = 1;
    public int HighestYearOutValue { get; private set; } = 1;
    
    public CashFlowFirstPassVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation)
        : base(operation, date => 0) // The getDayValue lambda is not used in this class
    {
        CashFlowByDate = goldInOut;
        foreach (StardewDate date in goldInOut.Keys)
        {
            if (goldInOut.TryGetValue(date, out var flow))
            {
                var inValue = flow.In;
                var outValue = Math.Abs(flow.Out);
                HighestDayInValue = Math.Max(HighestDayInValue, inValue);
                HighestDayOutValue = Math.Max(HighestDayOutValue, outValue);
            }
        }
    }

    // Override Visit methods to handle the tuple return values
    public new (int, int) Visit(DayNode day)
    {
        int dayIn = 0;
        int dayOut = 0;
        
        if (CashFlowByDate.TryGetValue(day.Date, out var goldInOut))
        {
            dayIn = goldInOut.In;
            dayOut = Math.Abs(goldInOut.Out);
            HighestDayInValue = Math.Max(HighestDayInValue, dayIn);
            HighestDayOutValue = Math.Max(HighestDayOutValue, dayOut);
        }
        
        return (dayIn, dayOut);
    }

    public new (int, int) Visit(SeasonNode season)
    {
        var dayValues = season.Days.Select(Visit).ToList();
        var inValues = dayValues.Select(v => v.Item1).ToList();
        var outValues = dayValues.Select(v => v.Item2).ToList();
        
        int aggregatedIn = DoOperation(inValues);
        int aggregatedOut = DoOperation(outValues);
        
        HighestSeasonInValue = Math.Max(HighestSeasonInValue, aggregatedIn);
        HighestSeasonOutValue = Math.Max(HighestSeasonOutValue, Math.Abs(aggregatedOut));
        
        return (aggregatedIn, aggregatedOut);
    }

    public new (int, int) Visit(YearNode year)
    {
        var seasonValues = year.Seasons.Select(Visit).ToList();
        var inValues = seasonValues.Select(v => v.Item1).ToList();
        var outValues = seasonValues.Select(v => v.Item2).ToList();
        
        int aggregatedIn = DoOperation(inValues);
        int aggregatedOut = DoOperation(outValues);
        
        HighestYearInValue = Math.Max(HighestYearInValue, aggregatedIn);
        HighestYearOutValue = Math.Max(HighestYearOutValue, Math.Abs(aggregatedOut));
        
        return (aggregatedIn, aggregatedOut);
    }

    public new void Visit(RootNode root)
    {
        root.Years.ForEach(e => Visit(e));
    }
}

internal abstract class VisitorBase(Operation operation)
{
    public Operation Operation { get; } = operation;

    public int DoOperation(List<int> entries)
    {
        return Operation switch
        {
            Operation.Min => entries.Min(),
            Operation.Max => entries.Max(),
            Operation.Sum => entries.Sum(),
            Operation.Average => (int)Math.Round(entries.Sum() / (float)entries.Count),
            Operation.End => entries.Last(),
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

internal class BaseVisitor : VisitorBase
{
    public int HighestOverallDayTotal { get; protected set; }
    public int HighestOverallSeasonTotal { get; protected set; }
    public int HighestOverallYearTotal { get; protected set; }
    private readonly Func<StardewDate, int> _getDayValue;

    public BaseVisitor(Operation operation, int highestOverallDayTotal, int highestOverallSeasonTotal, int highestOverallYearTotal, Func<StardewDate, int> getDayValue)
        : base(operation)
    {
        HighestOverallDayTotal = Math.Max(1, highestOverallDayTotal);
        HighestOverallSeasonTotal = Math.Max(1, highestOverallSeasonTotal);
        HighestOverallYearTotal = Math.Max(1, highestOverallYearTotal);
        _getDayValue = getDayValue;
    }
    
    public int GetDayValue(StardewDate date) => _getDayValue(date);

    public virtual DayElement Visit(DayNode day)
    {
        var highest = HighestOverallDayTotal;
        var dayGold = GetDayValue(day.Date);
        int rowHeight = CalculateRowHeight(dayGold, highest);
        string layout = FormatLayout(rowHeight);
        Logging.Monitor.Log($"Visit: {day.Date}, highest={highest}, dayGold={dayGold}, rowHeight={rowHeight}", LogLevel.Debug);
        string tooltip = $"{day.Date.Season}-{day.Date.Day}: {SproutSightViewModel.FormatGoldNumber(dayGold)}";
        return new DayElement(day.Date, dayGold, "", layout, tooltip, GetTint(day.Date.Season));
    }

    public virtual SeasonElement<List<DayElement>> Visit(SeasonNode season)
    {
        var entries = season.Days.Select(Visit).ToList();
        var golds = entries.Select(entry => entry.Value).ToList();
        int aggregated = DoOperation(golds);
        string tooltip = $"{Operation}: {SproutSightViewModel.FormatGoldNumber(aggregated)}";
        var highest = HighestOverallSeasonTotal;
        int rowHeight = CalculateRowHeight(aggregated, highest);
        string layout = FormatLayout(rowHeight);
        var seasonEntry = new SeasonElement<List<DayElement>>(
                season.Season, aggregated, entries,
                season + "", layout, tooltip, GetTint(season.Season), 
                season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);
        return seasonEntry;
    }

    public virtual YearElement<List<SeasonElement<List<DayElement>>>> Visit(YearNode year)
    {
        var entries = year.Seasons.Select(Visit).ToList();
        entries.Reverse();
        var golds = entries.Select(entry => entry.Value).ToList();
        int aggregated = DoOperation(golds);
        string tooltip = $"{Operation}: {SproutSightViewModel.FormatGoldNumber(aggregated)}";
        var highest = HighestOverallYearTotal;
        int rowHeight = CalculateRowHeight(aggregated, highest);
        string layout = FormatLayout(rowHeight);
        var yearEntry = new YearElement<List<SeasonElement<List<DayElement>>>>(
                    year.Year, aggregated, entries, year + "", layout, tooltip);
        return yearEntry;
    }
    
    public virtual RootElement<List<YearElement<List<SeasonElement<List<DayElement>>>>>> Visit(RootNode root)
    {
        var entries = root.Years.Select(Visit).ToList();
        entries.Reverse();
        var golds = entries.Select(entry => entry.Value).ToList();
        int aggregated = DoOperation(golds);
        string tooltip = $"Overall {Operation}: {SproutSightViewModel.FormatGoldNumber(aggregated)}";
        string text = tooltip;
        var element = new RootElement<List<YearElement<List<SeasonElement<List<DayElement>>>>>>(
                aggregated, entries, $"Overall {Operation}: {SproutSightViewModel.FormatGoldNumber(aggregated)}", null, tooltip);

        return element;
    }
}

internal class CashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation,
                      int highestDay, int highestSeason, int highestYear,
                      int highestDayIn, int highestSeasonIn, int highestYearIn,
                      int highestDayOut, int highestSeasonOut, int highestYearOut) : VisitorBase(operation)
{
    public Dictionary<StardewDate, GoldInOut> CashFlowByDate { get; } = goldInOut;

    // Highest values for income at each level
    public int HighestDayInValue { get; } = Math.Max(1, highestDayIn);
    public int HighestSeasonInValue { get; } = Math.Max(1, highestSeasonIn);
    public int HighestYearInValue { get; } = Math.Max(1, highestYearIn);

    // Highest values for expenses at each level
    public int HighestDayOutValue { get; } = Math.Max(1, highestDayOut);
    public int HighestSeasonOutValue { get; } = Math.Max(1, highestSeasonOut);
    public int HighestYearOutValue { get; } = Math.Max(1, highestYearOut);

    // Overall highest values for each level
    public int HighestOverallDayTotal { get; } = Math.Max(1, highestDay);
    public int HighestOverallSeasonTotal { get; } = Math.Max(1, highestSeason);
    public int HighestOverallYearTotal { get; } = Math.Max(1, highestYear);

    public (InOutElement, (int, int)) VisitCashFlow(DayNode day)
    {
        // Default values if date not found
        int dayIn = 0;
        int dayOut = 0;
        
        if (CashFlowByDate.TryGetValue(day.Date, out var goldInOut))
        {
            dayIn = goldInOut.In;
            dayOut = goldInOut.Out;
        }
        
        // Calculate in bar using day-specific highest value
        int inRowHeight = CalculateRowHeight(dayIn, HighestDayInValue);
        string inLayout = FormatLayout(inRowHeight);
        
        // Calculate out bar using day-specific highest value
        int outRowHeight = CalculateRowHeight(Math.Abs(dayOut), HighestDayOutValue);
        string outLayout = FormatLayout(outRowHeight);
        
        // Determine colors based on net value
        bool positive = dayIn + dayOut >= 0;
        var inTint = "#696969"; 
        var outTint = "#B22222"; 
        // var inTint = positive ? "#696969" : "#A9A9A9";  // Dark gray / Light gray
        // var outTint = positive ? "#F08080" : "#B22222";  // Light red / Dark red
        
        // Create tooltip with all information
        string tooltip = $"{day.Date.Season}-{day.Date.Day}\n" + 
                $"Net: {SproutSightViewModel.FormatGoldNumber(dayIn + dayOut)}\n" + 
                $"In: {SproutSightViewModel.FormatGoldNumber(dayIn)}\n" + 
                $"Out: {SproutSightViewModel.FormatGoldNumber(dayOut)}";
        
        return (new InOutElement("", inLayout, tooltip, inTint, "", outLayout, tooltip, outTint), (dayIn, dayOut));
    }

    public (SeasonElement<List<InOutElement>>, (int, int)) VisitCashFlow(SeasonNode season)
    {
        var entriesWithValues = season.Days.Select(VisitCashFlow).ToList();
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
                season + "", null, tooltip, GetTint(season.Season), 
                season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);
        return (cashFlowSeasonEntry, (aggregatedIn, aggregatedOut));
    }
    
    public (YearElement<List<SeasonElement<List<InOutElement>>>>, (int, int)) VisitCashFlow(YearNode year)
    {
        var entriesWithValues = year.Seasons.Select(VisitCashFlow).ToList();
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
    
    public RootElement<List<YearElement<List<SeasonElement<List<InOutElement>>>>>> VisitCashFlow(RootNode root)
    {
        var entriesWithValues = root.Years.Select(VisitCashFlow).ToList();
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
}

internal static class Visitors
{
    public static BaseVisitor CreateShippedVisitor(Dictionary<StardewDate, List<TrackedItemStack>> shippedData, Operation operation, 
                                                  int highestDay, int highestSeason, int highestYear)
    {
        return new BaseVisitor(operation, highestDay, highestSeason, highestYear, date => 
        {
            if (shippedData.TryGetValue(date, out var items))
            {
                return items.Select(item => item.TotalSalePrice).Sum();
            }
            return 0;
        });
    }

    public static BaseVisitor CreateWalletGoldVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation, 
                                                    int highestDay, int highestSeason, int highestYear)
    {
        return new BaseVisitor(operation, highestDay, highestSeason, highestYear, 
                              date => goldInOut.GetValueOrDefault(date)?.GoldInWallet ?? 0);
    }

    public static CashFlowVisitor CreateCashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation,
                                                      int highestDay, int highestSeason, int highestYear,
                                                      int highestDayIn, int highestSeasonIn, int highestYearIn,
                                                      int highestDayOut, int highestSeasonOut, int highestYearOut)
    {
        return new CashFlowVisitor(goldInOut, operation, highestDay, highestSeason, highestYear, highestDayIn, highestSeasonIn, highestYearIn, highestDayOut, highestSeasonOut, highestYearOut);
    }
}

// The tree that we will visit
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
    Wallet,
    CashFlow
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
