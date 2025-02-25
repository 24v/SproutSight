using System.ComponentModel;
using StardewValley;
using StardewUI;
using StardewValley.ItemTypeDefinitions;
using PropertyChanged.SourceGenerator;
using xTile;
using StardewModdingAPI;
using System.Collections.Generic;

namespace SproutSight;

internal partial class SproutSightViewModel
{

    // Showing Today's Info
    public StardewDate Date = StardewDate.GetStardewDate();
    public List<TrackedItemStack> CurrentItems { get; internal set; } = new();
    public int TodayGoldIn;
    public int TodayGoldOut;
    public bool ShippedSomething => CurrentItems.Count > 0;
    public string TotalProceeds => $"Current Shipped: {FormatGoldNumber(CurrentItems.Select(item => item.StackCount * item.SalePrice).Sum())}";


    // Historical Data
    public TrackedData TrackedData { get; internal set; } = new();
    public List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>>? DayGrid
    {
        get
        {
            return TrackedData.DayGrid;
        }
    }
    public List<YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>>? CashFlowGrid
    {
        get
        {
            return TrackedData.CashFlowGrid;
        }
    }


    // Tab Stuff
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


internal partial class TrackedData
{
    private void LoadAggregationAndView()
    {
        // Stardew UI doesnt allow indexing, so we have to create a hierarchical data model to use.
        // First we do aggregations then we create the view model.


        Dictionary<StardewYear, int> totalByYear = [];
        Dictionary<StardewYearSeason, int> totalBySeason = [];
        Dictionary<StardewDate, int> totalByDate = [];
        int highestOverallTotal = 1;

        Dictionary<StardewDate, GoldInOut> cashFlowByDate = [];
        int highestOverallCashFlow = 1;

        Dictionary<StardewDate, int> walletGoldByDate = [];
        int highestOverallWalletGold = 1;

        // Make sure we have data for every grid cell, even if no entries in file
        for (int year = 1; year <= Game1.year; year++)
        {
            totalByYear[new StardewYear(year)] = 0;
            foreach (Season season in Enum.GetValues(typeof(Season)))
            {
                totalBySeason[new StardewYearSeason(year, season)] = 0;
                for (int day = 1; day <= 28; day++)
                {
                    var date = new StardewDate(year, season, day);
                    totalByDate[date] = 0;
                    cashFlowByDate[date] = new GoldInOut(0, 0, 0);
                    walletGoldByDate[date] = 0;
                }
            }
        }

        // Aggregate Data
        foreach (StardewDate date in ShippedData.Keys)
        {
            int total = ShippedData[date].Sum(d => d.TotalSalePrice);
            totalByYear[new StardewYear(date.Year)] += total;
            totalBySeason[new StardewYearSeason(date.Year, date.Season)] += total;
            totalByDate[date] += total;
            highestOverallTotal = Math.Max(highestOverallTotal, total);
        }

        // Use this after we get rid of test data
        foreach(StardewDate date in GoldInOut.Keys)
        {
            cashFlowByDate[date] = GoldInOut[date];
            highestOverallCashFlow = Math.Max(highestOverallCashFlow, Math.Max(cashFlowByDate[date].In, Math.Abs(cashFlowByDate[date].Out)));
            walletGoldByDate[date] = GoldInOut[date].GoldInWallet;
            highestOverallWalletGold = Math.Max(highestOverallWalletGold, walletGoldByDate[date]);
        }

        // Construct grid for day view & cashflow view
        _dayGrid = [];
        _cashFlowGrid = [];
        _walletGrid = [];
        var maxRowHeight = 128;
        var minRowHeight = 2;
        var rowWidth = 20;

        // Want to display in reverse order
        Season[] seasons = (Season[])Enum.GetValues(typeof(Season));
        Array.Reverse(seasons);
        for (int year = Game1.year; year > 0; year--)
        {
            List<SeasonEntryElement<List<DayEntryElement>>> seasonsPerYear = [];
            _dayGrid.Add(new YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>(
                    year, seasonsPerYear, $"Year Total: {SproutSightViewModel.FormatGoldNumber(totalByYear[new StardewYear(year)])}"));

            List<SeasonEntryElement<List<InOutEntry>>> cashFlowSeasonsPerYear = [];
            _cashFlowGrid.Add(new YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>(
                    year, cashFlowSeasonsPerYear, $"TODO"));

            List<SeasonEntryElement<List<DayEntryElement>>> walletSeasonsPerYear = [];
            _walletGrid.Add(new YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>(
                    year, walletSeasonsPerYear, $"TODO"));

            foreach (Season season in seasons)
            {
                List<DayEntryElement> daysPerSeason = [];
                var seasonEntry = new SeasonEntryElement<List<DayEntryElement>>(
                        season, daysPerSeason, 
                        season + "", null, null, null, season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);
                seasonsPerYear.Add(seasonEntry);

                List<DayEntryElement> walletGoldPerSeason = [];
                var walletGoldSeasonEntry = new SeasonEntryElement<List<DayEntryElement>>(
                        season, walletGoldPerSeason, 
                        season + "", null, null, null, season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);
                walletSeasonsPerYear.Add(walletGoldSeasonEntry);

                List<InOutEntry> cashFlowDaysPerSeason = [];
                var cashFlowSeasonEntry = new SeasonEntryElement<List<InOutEntry>>(
                        season, cashFlowDaysPerSeason, 
                        season + "", null, null, null, season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);
                cashFlowSeasonsPerYear.Add(cashFlowSeasonEntry);

                for (int day = 1; day <= 28; day++)
                {
                    string tint = season switch
                    {
                        Season.Spring => "Green",
                        Season.Summer => "Yellow",
                        Season.Fall => "Brown",
                        Season.Winter => "Blue",
                        _ => "White"
                    };
                    var date = new StardewDate(year, season, day);

                    {
                        var highest = highestOverallTotal;
                        var dayTotal = totalByDate[date];
                        var rowHeight = 1.0;
                        if (dayTotal > 0)
                        {
                            var scale = (float)dayTotal / highest;
                            rowHeight = Math.Max(minRowHeight, scale * maxRowHeight);
                        }
                        string layout = $"{rowWidth}px {rowHeight}px";
                        string tooltip = $"{season}-{day}: {SproutSightViewModel.FormatGoldNumber(dayTotal)}";
                        daysPerSeason.Add(new DayEntryElement(date, "", layout, tooltip, tint));
                    }

                    {
                        var highest = highestOverallWalletGold;
                        var dayWalletGold = walletGoldByDate[date];
                        var rowHeight = 1.0;
                        if (dayWalletGold > 0)
                        {
                            var scale = (float)dayWalletGold / highest;
                            rowHeight = Math.Max(minRowHeight, scale * maxRowHeight);
                        }
                        string layout = $"{rowWidth}px {rowHeight}px";
                        string tooltip = $"{season}-{day}: {SproutSightViewModel.FormatGoldNumber(dayWalletGold)}";
                        walletGoldPerSeason.Add(new DayEntryElement(date, "", layout, tooltip, tint));
                    }

                    {
                        var highest = highestOverallCashFlow;
                        var dayIn = cashFlowByDate[date].In;
                        var inScale = (float) dayIn / highest;
                        var inRowHeight = Math.Max(minRowHeight, inScale * maxRowHeight);
                        var inLayout = $"{rowWidth}px {inRowHeight}px";
                        var inTooltip = $"{season}-{day}: {SproutSightViewModel.FormatGoldNumber(dayIn)}";

                        var dayOut = cashFlowByDate[date].Out;
                        var outScale = (float) Math.Abs(dayOut) / highest;
                        var outRowHeight = Math.Max(minRowHeight, outScale * maxRowHeight);
                        var outLayout = $"{rowWidth}px {outRowHeight}px";
                        var outTooltip = $"{season}-{day}: {SproutSightViewModel.FormatGoldNumber(dayOut)}";

                        bool positive = dayIn + dayOut >= 0;
                        var inTint = positive? "#696969" : "#A9A9A9";
                        var outTint = positive? "#F08080" : "#B22222";

                        string toolTip = $"{season}-{day}\n" + 
                                $"Net: {SproutSightViewModel.FormatGoldNumber(dayIn + dayOut)}\n" + 
                                $"In: {SproutSightViewModel.FormatGoldNumber(dayIn)}\n" + 
                                $"Out: {SproutSightViewModel.FormatGoldNumber(dayOut)}";
                        cashFlowDaysPerSeason.Add(new InOutEntry("", inLayout, toolTip, inTint, "", outLayout, toolTip, outTint));
                    }
                }
            }
        }

        // Log the day grid structure
        Logging.Monitor.Log("=== Day Grid Structure ===", LogLevel.Info);
        foreach (var yearEntry in _dayGrid)
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

        Logging.Monitor.Log("=== Wallet Grid Structure ===", LogLevel.Info);
        foreach (var yearEntry in _walletGrid)
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



        Logging.Monitor.Log("=== Cash Flow Grid Structure ===", LogLevel.Info);
        foreach (var yearEntry in _cashFlowGrid)
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
                    Logging.Monitor.Log($"      In: Layout={dayEntry.InLayout}, Tooltip={dayEntry.InTooltip}", LogLevel.Info);
                    Logging.Monitor.Log($"      Out: Layout={dayEntry.OutLayout}, Tooltip={dayEntry.OutTooltip}", LogLevel.Info);
                }
            }
        }
    }
    private List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>>? _walletGrid;
    public List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>> WalletGrid
    {
        get
        {
            if (_walletGrid == null)
            {
                LoadAggregationAndView();
            }
            return _walletGrid!;
        }
    }

    private List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>>? _dayGrid;
    public List<YearEntryElement<List<SeasonEntryElement<List<DayEntryElement>>>>> DayGrid
    {
        get
        {
            if (_dayGrid == null)
            {
                LoadAggregationAndView();
            }
            return _dayGrid!;
        }
    }

    private List<YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>>? _cashFlowGrid;
    public List<YearEntryElement<List<SeasonEntryElement<List<InOutEntry>>>>> CashFlowGrid
    {
        get
        {
            if (_cashFlowGrid == null)
            {
                LoadAggregationAndView();
            }
            return _cashFlowGrid!;
        }
    }
}

internal record InOutEntry(string InText, string InLayout, string InTooltip, string InTint, string OutText, string OutLayout, string OutTooltip, string OutTint);
internal record YearEntryElement<T>(int Year, T Value, string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null);
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
