using System.ComponentModel;
using StardewValley;
using StardewUI;
using StardewValley.ItemTypeDefinitions;
using PropertyChanged.SourceGenerator;
using xTile;
using StardewModdingAPI;
using System.Collections.Generic;
using StardewUI.Framework;

namespace SproutSight;


// TODO: Fix up generics --> Maybe use typedef?
// TODO: Use accept
// TODO: Create templates in view
// TODO: Diff levels of aggregation
// TODO: Clean up averages
// TODO: fix in/out tooltips

internal partial class SproutSightViewModel
{
    public const int MaxRowHeight = 128;
    public const int MinRowHeight = 2;
    public const int ZeroRowHeight = 1;
    public const int RowWidth = 20;

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
    public List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>> WalletGrid { get; set; } = [];
    public int WalletAverage { get; set; } = 0;

    public List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>> ShippedGrid { get; set; } = [];
    public int ShippedTotal { get; set; } = 0;
    
    public List<YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>> CashFlowGrid { get; set; } = [];
    public int CashFlowNetTotal { get; set; } = 0;

    public int AverageGoldInWallet { get; set; } = 0;

    public void LoadAggregationAndViewVisitor()
    {
        // Create the date structure and some required calculations ahead of time
        List<YearNode> yearNodes = [];
        RootNode rootNode = new(yearNodes);
        int highestWalletGold = 0;
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
                    var daysWalledGold = TrackedData.GoldInOut.GetValueOrDefault(date)?.GoldInWallet ?? 0;
                    highestWalletGold = Math.Max(highestWalletGold, daysWalledGold);
                }
            }
        }

        var walletGoldVisitor = new WalletGoldVisitor()
        {
            GoldInOut = TrackedData.GoldInOut,
            HighestOverallWalletGold = highestWalletGold
        };
        var walletRoot = walletGoldVisitor.Visit(rootNode);
        WalletGrid = walletRoot.Value;

        var shippedVisitor = new ShippedVisitor(TrackedData.ShippedData);
        var shippedRoot = shippedVisitor.Visit(rootNode);
        ShippedGrid = shippedRoot.Value;
        
        var cashFlowVisitor = new CashFlowVisitor(TrackedData.GoldInOut);
        var cashFlowRoot = cashFlowVisitor.Visit(rootNode);
        CashFlowGrid = cashFlowRoot.Value;

        LogGridStructures();
    }

    public void LogGridStructures() 
    {
        Logging.Monitor.Log("=== Wallet Grid Structure ===", LogLevel.Info);
        foreach (var yearEntry in WalletGrid)
        {
            Logging.Monitor.Log($"Year {yearEntry.Year}:", LogLevel.Info);
            Logging.Monitor.Log($"  Text: {yearEntry.Text}", LogLevel.Info);
            foreach (var seasonEntry in yearEntry.Value)
            {
                Logging.Monitor.Log($"  Season {seasonEntry.Season}:", LogLevel.Info);
                Logging.Monitor.Log($"    Text: {seasonEntry.Text}", LogLevel.Info);
                Logging.Monitor.Log($"    Days:", LogLevel.Info);
                foreach (var dayEntry in seasonEntry.Value)
                {
                    Logging.Monitor.Log($"      Date: {dayEntry.Date}, Display: Layout={dayEntry.Layout}, Tooltip={dayEntry.Tooltip}, Tint={dayEntry.Tint}", LogLevel.Info);
                }
            }
        }
    }


}

// internal partial class TrackedData
// {


    // private void LoadAggregationAndView()
    // {
    //     // Stardew UI doesnt allow indexing, so we have to create a hierarchical data model to use.
    //     // First we do aggregations then we create the view model.

    //     Dictionary<StardewYear, int> totalShippedGoldByYear = [];
    //     Dictionary<StardewYearSeason, int> totalShippedGoldBySeason = [];
    //     Dictionary<StardewDate, int> totalShippedGoldByDate = [];
    //     int highestOverallTotal = 1;

    //     Dictionary<StardewDate, GoldInOut> cashFlowByDate = [];
    //     int highestOverallCashFlow = 1;

    //     Dictionary<StardewDate, int> walletGoldByDate = [];
    //     int highestOverallWalletGold = 1;

    //     // Aggregate Data
    //     foreach (StardewDate date in ShippedData.Keys)
    //     {
    //         int total = ShippedData[date].Sum(d => d.TotalSalePrice);
    //         totalShippedGoldByYear[new StardewYear(date.Year)] += total;
    //         totalShippedGoldBySeason[new StardewYearSeason(date.Year, date.Season)] += total;
    //         totalShippedGoldByDate[date] += total;
    //         highestOverallTotal = Math.Max(highestOverallTotal, total);
    //     }

    //     // Create a lookup table of 
    //     for (int year = 1; year <= Game1.year; year++)
    //     {
    //         totalShippedGoldByYear[new StardewYear(year)] = 0;
    //         foreach (Season season in Enum.GetValues(typeof(Season)))
    //         {
    //             totalShippedGoldBySeason[new StardewYearSeason(year, season)] = 0;
    //             for (int day = 1; day <= 28; day++)
    //             {
    //                 var date = new StardewDate(year, season, day);
    //                 totalShippedGoldByDate[date] = 0;
    //                 cashFlowByDate[date] = new GoldInOut(0, 0, 0);
    //                 walletGoldByDate[date] = 0;
    //             }
    //         }
    //     }

    //     // Populate every grid cell where we have data
    //     foreach(StardewDate date in GoldInOut.Keys)
    //     {
    //         cashFlowByDate[date] = GoldInOut[date];
    //         highestOverallCashFlow = Math.Max(highestOverallCashFlow, Math.Max(cashFlowByDate[date].In, Math.Abs(cashFlowByDate[date].Out)));
    //         walletGoldByDate[date] = GoldInOut[date].GoldInWallet;
    //         highestOverallWalletGold = Math.Max(highestOverallWalletGold, walletGoldByDate[date]);
    //     }

    //     // Construct grid for day view & cashflow view
    //     _dayGrid = [];
    //     _cashFlowGrid = [];
    //     _walletGrid = [];
    //     var maxRowHeight = 128;
    //     var minRowHeight = 2;
    //     var rowWidth = 20;

    //     // Want to display in reverse order
    //     Season[] seasons = (Season[])Enum.GetValues(typeof(Season));
    //     Array.Reverse(seasons);
    //     for (int year = Game1.year; year > 0; year--)
    //     {
    //         List<SeasonEntryElement<List<DayEntryElement>>> seasonsPerYear = [];
    //         _dayGrid.Add(new YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>(
    //                 year, seasonsPerYear, $"Year Total: {SproutSightViewModel.FormatGoldNumber(totalShippedGoldByYear[new StardewYear(year)])}"));

    //         List<SeasonEntryElement<List<InOutEntry>>> cashFlowSeasonsPerYear = [];
    //         _cashFlowGrid.Add(new YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>(
    //                 year, cashFlowSeasonsPerYear, $"TODO"));

    //         List<SeasonEntryElement<List<DayEntryElement>>> walletSeasonsPerYear = [];
    //         _walletGrid.Add(new YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>(
    //                 year, walletSeasonsPerYear, $"TODO"));

    //         foreach (Season season in seasons)
    //         {
    //             List<DayEntryElement> daysPerSeason = [];
    //             var seasonEntry = new SeasonEntryElement<List<DayEntryElement>>(
    //                     season, daysPerSeason, 
    //                     season + "", null, null, null, season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);
    //             seasonsPerYear.Add(seasonEntry);

    //             List<DayEntryElement> walletGoldPerSeason = [];
    //             var walletGoldSeasonEntry = new SeasonEntryElement<List<DayEntryElement>>(
    //                     season, walletGoldPerSeason, 
    //                     season + "", null, null, null, season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);
    //             walletSeasonsPerYear.Add(walletGoldSeasonEntry);

    //             List<InOutEntry> cashFlowDaysPerSeason = [];
    //             var cashFlowSeasonEntry = new SeasonEntryElement<List<InOutEntry>>(
    //                     season, cashFlowDaysPerSeason, 
    //                     season + "", null, null, null, season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);
    //             cashFlowSeasonsPerYear.Add(cashFlowSeasonEntry);

    //             for (int day = 1; day <= 28; day++)
    //             {
    //                 string tint = season switch
    //                 {
    //                     Season.Spring => "Green",
    //                     Season.Summer => "Yellow",
    //                     Season.Fall => "Brown",
    //                     Season.Winter => "Blue",
    //                     _ => "White"
    //                 };
    //                 var date = new StardewDate(year, season, day);

    //                 {
    //                     var highest = highestOverallTotal;
    //                     var dayTotal = totalShippedGoldByDate[date];
    //                     var rowHeight = 1.0;
    //                     if (dayTotal > 0)
    //                     {
    //                         var scale = (float)dayTotal / highest;
    //                         rowHeight = Math.Max(minRowHeight, scale * maxRowHeight);
    //                     }
    //                     string layout = $"{rowWidth}px {rowHeight}px";
    //                     string tooltip = $"{season}-{day}: {SproutSightViewModel.FormatGoldNumber(dayTotal)}";
    //                     daysPerSeason.Add(new DayEntryElement(date, "", layout, tooltip, tint));
    //                 }

    //                 {
    //                     var highest = highestOverallWalletGold;
    //                     var dayWalletGold = walletGoldByDate[date];
    //                     var rowHeight = 1.0;
    //                     if (dayWalletGold > 0)
    //                     {
    //                         var scale = (float)dayWalletGold / highest;
    //                         rowHeight = Math.Max(minRowHeight, scale * maxRowHeight);
    //                     }
    //                     string layout = $"{rowWidth}px {rowHeight}px";
    //                     string tooltip = $"{season}-{day}: {SproutSightViewModel.FormatGoldNumber(dayWalletGold)}";
    //                     walletGoldPerSeason.Add(new DayEntryElement(date, "", layout, tooltip, tint));
    //                 }

    //                 {
    //                     var highest = highestOverallCashFlow;
    //                     var dayIn = cashFlowByDate[date].In;
    //                     var inScale = (float) dayIn / highest;
    //                     var inRowHeight = Math.Max(minRowHeight, inScale * maxRowHeight);
    //                     var inLayout = $"{rowWidth}px {inRowHeight}px";
    //                     var inTooltip = $"{season}-{day}: {SproutSightViewModel.FormatGoldNumber(dayIn)}";

    //                     var dayOut = cashFlowByDate[date].Out;
    //                     var outScale = (float) Math.Abs(dayOut) / highest;
    //                     var outRowHeight = Math.Max(minRowHeight, outScale * maxRowHeight);
    //                     var outLayout = $"{rowWidth}px {outRowHeight}px";
    //                     var outTooltip = $"{season}-{day}: {SproutSightViewModel.FormatGoldNumber(dayOut)}";

    //                     bool positive = dayIn + dayOut >= 0;
    //                     var inTint = positive? "#696969" : "#A9A9A9";
    //                     var outTint = positive? "#F08080" : "#B22222";

    //                     string toolTip = $"{season}-{day}\n" + 
    //                             $"Net: {SproutSightViewModel.FormatGoldNumber(dayIn + dayOut)}\n" + 
    //                             $"In: {SproutSightViewModel.FormatGoldNumber(dayIn)}\n" + 
    //                             $"Out: {SproutSightViewModel.FormatGoldNumber(dayOut)}";
    //                     cashFlowDaysPerSeason.Add(new InOutEntry("", inLayout, toolTip, inTint, "", outLayout, toolTip, outTint));
    //                 }
    //             }
    //         }
    //     }

    //     LogGridStructures();
    // }

//     private void LogGridStructures()
//     {
//         // Log the day grid structure
//         // Logging.Monitor.Log("=== Day Grid Structure ===", LogLevel.Info);
//         // foreach (var yearEntry in _dayGrid)
//         // {
//         //     Logging.Monitor.Log($"Year {yearEntry.Year}:", LogLevel.Info);
//         //     Logging.Monitor.Log($"  Text: {yearEntry.Text}", LogLevel.Info);
//         //     foreach (var seasonEntry in yearEntry.Value)
//         //     {
//         //         Logging.Monitor.Log($"  Season {seasonEntry.Season}:", LogLevel.Info);
//         //         Logging.Monitor.Log($"    Text: {seasonEntry.Text}", LogLevel.Info);
//         //         Logging.Monitor.Log($"    Days:", LogLevel.Info);
//         //         foreach (var dayEntry in seasonEntry.Value)
//         //         {
//         //             Logging.Monitor.Log($"      Date: {dayEntry.Date}, Display: Layout={dayEntry.Layout}, Tooltip={dayEntry.Tooltip}, Tint={dayEntry.Tint}", LogLevel.Info);
//         //         }
//         //     }
//         // }

//         Logging.Monitor.Log("=== Wallet Grid Structure ===", LogLevel.Info);
//         foreach (var yearEntry in WalletGrid)
//         {
//             Logging.Monitor.Log($"Year {yearEntry.Year}:", LogLevel.Info);
//             Logging.Monitor.Log($"  Text: {yearEntry.Text}", LogLevel.Info);
//             foreach (var seasonEntry in yearEntry.Value)
//             {
//                 Logging.Monitor.Log($"  Season {seasonEntry.Season}:", LogLevel.Info);
//                 Logging.Monitor.Log($"    Text: {seasonEntry.Text}", LogLevel.Info);
//                 Logging.Monitor.Log($"    Days:", LogLevel.Info);
//                 foreach (var dayEntry in seasonEntry.Value)
//                 {
//                     Logging.Monitor.Log($"      Date: {dayEntry.Date}, Display: Layout={dayEntry.Layout}, Tooltip={dayEntry.Tooltip}, Tint={dayEntry.Tint}", LogLevel.Info);
//                 }
//             }
//         }

//         // Logging.Monitor.Log("=== Cash Flow Grid Structure ===", LogLevel.Info);
//         // foreach (var yearEntry in _cashFlowGrid)
//         // {
//         //     Logging.Monitor.Log($"Year {yearEntry.Year}:", LogLevel.Info);
//         //     Logging.Monitor.Log($"  Text: {yearEntry.Text}", LogLevel.Info);
//         //     foreach (var seasonEntry in yearEntry.Value)
//         //     {
//         //         Logging.Monitor.Log($"  Season {seasonEntry.Season}:", LogLevel.Info);
//         //         Logging.Monitor.Log($"    Text: {seasonEntry.Text}", LogLevel.Info);
//         //         Logging.Monitor.Log($"    Days:", LogLevel.Info);
//         //         foreach (var dayEntry in seasonEntry.Value)
//         //         {
//         //             Logging.Monitor.Log($"      In: Layout={dayEntry.InLayout}, Tooltip={dayEntry.InTooltip}", LogLevel.Info);
//         //             Logging.Monitor.Log($"      Out: Layout={dayEntry.OutLayout}, Tooltip={dayEntry.OutTooltip}", LogLevel.Info);
//         //         }
//         //     }
//         // }
//     }


//     // private List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>>? _dayGrid;
//     // public List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>> DayGrid
//     // {
//     //     get
//     //     {
//     //         if (_dayGrid == null)
//     //         {
//     //             LoadAggregationAndView();
//     //         }
//     //         return _dayGrid!;
//     //     }
//     // }

//     // private List<YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>>? _cashFlowGrid;
//     // public List<YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>> CashFlowGrid
//     // {
//     //     get
//     //     {
//     //         if (_cashFlowGrid == null)
//     //         {
//     //             LoadAggregationAndView();
//     //         }
//     //         return _cashFlowGrid!;
//     //     }
//     // }
// }

// Moving traversal to these would reduce reiteration

internal record YearNode(int Year, List<SeasonNode> Seasons)
{
    // public void Accept<T,V>(IDateVisitor<T,V> visitor) 
    // {
    //     foreach (SeasonNode season in Seasons)
    //     {
    //         season.Accept(visitor);
    //     }
    //     visitor.Visit(this);
    // }
}

internal record SeasonNode(Season Season, List<DayNode> Days)
{
    // public void Accept<T,V>(IDateVisitor<T,V> visitor) 
    // {
    //     visitor.Visit(this);
    // }

}

internal record DayNode(StardewDate Date)
{
    // public void Accept<T,V>(IDateVisitor<T,V> visitor) 
    // {
    //     visitor.Visit(this);
    // }
}

internal record RootNode(List<YearNode> Years)
{
    // public void Accept<T,V>(IDateVisitor<T,V> visitor) 
    // {
    //     visitor.Visit(this);
    // }
}

// TODO: Nested type parameters?

interface IDateVisitor<YearType, SeasonType> {
    // public RootEntryElement<List<YearType>> Visit(RootNode root);
    public (YearEntryElement<YearType>, int) Visit(YearNode year);
    public (SeasonEntryElement<SeasonType>, int) Visit(SeasonNode season);
    // public (DayEntryElement, int) Visit(DayNode day);
}

internal class WalletGoldVisitor : IDateVisitor<List<SeasonEntryElement<List<DayEntryElement>>>, List<DayEntryElement>>
{
    public Dictionary<StardewDate, GoldInOut> GoldInOut { get; set; } = [];
    public int HighestOverallWalletGold = 0;

    public RootEntryElement<List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>>> Visit(RootNode root)
    {
        var entries = root.Years.Select(Visit).ToList();
        int average = (int)Math.Round(entries.Sum(entry => entry.Item2) / (float) entries.Count);
        var yearEntries = entries.Select(entry => entry.Item1).ToList();
        string tooltip = $"Avg: {SproutSightViewModel.FormatGoldNumber(average)}";
        var element = new RootEntryElement<List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>>>(yearEntries, null, null, tooltip);
        return element;

    }
    public (YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>, int) Visit(YearNode year)
    {
        var entries = year.Seasons.Select(Visit).ToList();
        entries.Reverse();
        int average = (int)Math.Round(entries.Sum(entry => entry.Item2) / (float) entries.Count);
        string tooltip = $"Avg: {SproutSightViewModel.FormatGoldNumber(average)}";
        var walletSeasonsPerYear = entries.Select(entry => entry.Item1).ToList();
        var walletGoldYearEntry = new YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>(
                    year.Year, walletSeasonsPerYear, year + "", null, tooltip);
        return (walletGoldYearEntry, average);

    }
    public (SeasonEntryElement<List<DayEntryElement>>, int) Visit(SeasonNode season)
    {
        // Could use reduce to not iterate twice
        var entries = season.Days.Select(Visit).ToList();
        int average = (int)Math.Round(entries.Sum(entry => entry.Item2) / (float) entries.Count);
        var walletGolds = entries.Select(entry => entry.Item1).ToList();
        string tooltip = $"Avg: {SproutSightViewModel.FormatGoldNumber(average)}";
        var walletGoldSeasonEntry = new SeasonEntryElement<List<DayEntryElement>>(
                season.Season, walletGolds,
                season + "", null, tooltip, null, 
                season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);
        return (walletGoldSeasonEntry, average);
    }
    public (DayEntryElement, int) Visit(DayNode day)
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
        return (new DayEntryElement(day.Date, "", layout, tooltip, SproutSightViewModel.GetTint(day.Date.Season)), dayWalletGold);
    }
}

internal class ShippedVisitor : IDateVisitor<List<SeasonEntryElement<List<DayEntryElement>>>, List<DayEntryElement>>
{
    public Dictionary<StardewDate, GoldInOut> GoldInOut { get; set; } = [];
    public int HighestOverallWalletGold = 0;
    public Dictionary<StardewDate, List<TrackedItemStack>> ShippedData { get; set; } = [];
    public Dictionary<StardewDate, int> TotalShippedGoldByDate { get; } = [];
    public int HighestOverallTotal { get; private set; } = 0;

    public ShippedVisitor(Dictionary<StardewDate, List<TrackedItemStack>> shippedData)
    {
        ShippedData = shippedData;
        foreach (StardewDate date in shippedData.Keys)
        {
            int total = shippedData[date].Sum(d => d.TotalSalePrice);
            TotalShippedGoldByDate[date] = TotalShippedGoldByDate.GetValueOrDefault(date, 0) + total;
            HighestOverallTotal = Math.Max(HighestOverallTotal, total);
        }
    }

    public RootEntryElement<List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>>> Visit(RootNode root)
    {
        var entries = root.Years.Select(Visit).ToList();
        int sum = entries.Sum(entry => entry.Item2);
        string tooltip = $"Total: {SproutSightViewModel.FormatGoldNumber(sum)}";
        var yearEntries = entries.Select(entry => entry.Item1).ToList();
        var element = new RootEntryElement<List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>>>(yearEntries, null, null, tooltip);
        return element;

    }
    public (YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>, int) Visit(YearNode year)
    {
        var entries = year.Seasons.Select(Visit).ToList();
        entries.Reverse();
        int sum = entries.Sum(entry => entry.Item2);
        string tooltip = $"Total: {SproutSightViewModel.FormatGoldNumber(sum)}";
        var shippedSeasonsPerYear = entries.Select(entry => entry.Item1).ToList();
        var shippedGoldYearEntry = new YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>(
                    year.Year, shippedSeasonsPerYear, year + "", null, tooltip);
        return (shippedGoldYearEntry, sum);

    }
    public (SeasonEntryElement<List<DayEntryElement>>, int) Visit(SeasonNode season)
    {
        // Could use reduce to not iterate twice
        var entries = season.Days.Select(Visit).ToList();
        int sum = entries.Sum(entry => entry.Item2);
        var shippedGolds = entries.Select(entry => entry.Item1).ToList();
        string tooltip = $"Total: {SproutSightViewModel.FormatGoldNumber(sum)}";
        var shippedGoldSeasonEntry = new SeasonEntryElement<List<DayEntryElement>>(
                season.Season, shippedGolds,
                season + "", null, tooltip, null, 
                season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);
        return (shippedGoldSeasonEntry, sum);
    }
    public (DayEntryElement, int) Visit(DayNode day)
    {
        var highest = HighestOverallTotal;
        var dayShippedGold = TotalShippedGoldByDate.GetValueOrDefault(day.Date);
        var rowHeight = SproutSightViewModel.ZeroRowHeight;
        if (dayShippedGold > 0)
        {
            var scale = (float)dayShippedGold / highest;
            rowHeight = (int)Math.Round(Math.Max(SproutSightViewModel.MinRowHeight, scale * SproutSightViewModel.MaxRowHeight));
        }
        string layout = $"{SproutSightViewModel.RowWidth}px {rowHeight}px";
        string tooltip = $"{day.Date.Season}-{day.Date.Day}: {SproutSightViewModel.FormatGoldNumber(dayShippedGold)}";
        return (new DayEntryElement(day.Date, "", layout, tooltip, SproutSightViewModel.GetTint(day.Date.Season)), dayShippedGold);
    }
}

internal class CashFlowVisitor : IDateVisitor<List<SeasonEntryElement<List<InOutEntry>>>, List<InOutEntry>>
{
    public Dictionary<StardewDate, GoldInOut> CashFlowByDate { get; set; } = [];
    public int HighestOverallCashFlow { get; private set; } = 1;

    public CashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut)
    {
        CashFlowByDate = goldInOut;
        foreach (StardewDate date in goldInOut.Keys)
        {
            var inValue = goldInOut[date].In;
            var outValue = Math.Abs(goldInOut[date].Out);
            HighestOverallCashFlow = Math.Max(HighestOverallCashFlow, Math.Max(inValue, outValue));
        }
    }

    public RootEntryElement<List<YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>>> Visit(RootNode root)
    {
        var entries = root.Years.Select(Visit).ToList();
        int netSum = entries.Sum(entry => entry.Item2);
        string tooltip = $"Net Total: {SproutSightViewModel.FormatGoldNumber(netSum)}";
        var yearEntries = entries.Select(entry => entry.Item1).ToList();
        var element = new RootEntryElement<List<YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>>>(yearEntries, null, null, tooltip);
        return element;
    }

    public (YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>, int) Visit(YearNode year)
    {
        var entries = year.Seasons.Select(Visit).ToList();
        entries.Reverse();
        int netSum = entries.Sum(entry => entry.Item2);
        string tooltip = $"Net Total: {SproutSightViewModel.FormatGoldNumber(netSum)}";
        var cashFlowSeasonsPerYear = entries.Select(entry => entry.Item1).ToList();
        var cashFlowYearEntry = new YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>(
                    year.Year, cashFlowSeasonsPerYear, year + "", null, tooltip);
        return (cashFlowYearEntry, netSum);
    }

    public (SeasonEntryElement<List<InOutEntry>>, int) Visit(SeasonNode season)
    {
        var entries = season.Days.Select(Visit).ToList();
        int netSum = entries.Sum(entry => entry.Item2);
        var cashFlowDays = entries.Select(entry => entry.Item1).ToList();
        string tooltip = $"Net Total: {SproutSightViewModel.FormatGoldNumber(netSum)}";
        var cashFlowSeasonEntry = new SeasonEntryElement<List<InOutEntry>>(
                season.Season, cashFlowDays,
                null, null, tooltip, null, 
                season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);
        return (cashFlowSeasonEntry, netSum);
    }

    public (InOutEntry, int) Visit(DayNode day)
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
        var inScale = (float)dayIn / highest;
        var inRowHeight = (int)Math.Round(Math.Max(SproutSightViewModel.MinRowHeight, inScale * SproutSightViewModel.MaxRowHeight));
        var inLayout = $"{SproutSightViewModel.RowWidth}px {inRowHeight}px";
        
        // Calculate out bar
        var outScale = (float)Math.Abs(dayOut) / highest;
        var outRowHeight = (int)Math.Round(Math.Max(SproutSightViewModel.MinRowHeight, outScale * SproutSightViewModel.MaxRowHeight));
        var outLayout = $"{SproutSightViewModel.RowWidth}px {outRowHeight}px";
        
        // Determine colors based on net value
        bool positive = dayIn + dayOut >= 0;
        var inTint = positive ? "#696969" : "#A9A9A9";  // Dark gray / Light gray
        var outTint = positive ? "#F08080" : "#B22222";  // Light red / Dark red
        
        // Create tooltip with all information
        string tooltip = $"{day.Date.Season}-{day.Date.Day}\n" + 
                $"Net: {SproutSightViewModel.FormatGoldNumber(dayIn + dayOut)}\n" + 
                $"In: {SproutSightViewModel.FormatGoldNumber(dayIn)}\n" + 
                $"Out: {SproutSightViewModel.FormatGoldNumber(dayOut)}";
        
        return (new InOutEntry("", inLayout, tooltip, inTint, "", outLayout, tooltip, outTint), dayIn + dayOut);
    }
}

internal record InOutEntry(string InText, string InLayout, string InTooltip, string InTint, string OutText, string OutLayout, string OutTooltip, string OutTint);
internal record YearEntryElement<T>(int Year, T Value, string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null);
internal record RootEntryElement<T>(T Value, string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null);
internal record SeasonEntryElement<T>(Season Season, T Value, string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null, bool IsSpring = false, bool IsSummer = false, bool IsFall = false, bool IsWinter = false);
internal record DayEntryElement(StardewDate Date, string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null);

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
