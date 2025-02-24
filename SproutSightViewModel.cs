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
    public string TotalProceeds => $"Current Shipped: {CurrentItems.Select(item => item.StackCount * item.SalePrice).Sum()}g";


    // Historical Data
    public TrackedData TrackedData { get; internal set; } = new();

    // Tabs Stuff
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
}

internal partial class TrackedItemStack
{
    public string FormattedSale => $"({StackCount}x{SalePrice}g)";
}


internal partial class TrackedData
{
    // private void LoadAll()
    // {

    //     Dictionary<int, int> yearTotals = [];
    //     Dictionary<(int, Season), int> seasonTotals = [];
    //     Dictionary<StardewDate, int> dayTotals = [];
    //     Dictionary<(int, Season), int> highestTotalPerSeason = [];
    //     int highestOverallTotal = 1;
    //     // Make sure we have data for every grid cell, even if no entries in file
    //     for (int year = 1; year <= Game1.year; year++)
    //     {
    //         yearTotals[year] = 0;
    //         foreach (Season season in Enum.GetValues(typeof(Season)))
    //         {
    //             seasonTotals[(year, season)] = 0;
    //             highestTotalPerSeason[(year, season)] = 0;
    //             for (int day = 1; day <= 28; day++)
    //             {
    //                 dayTotals[new StardewDate(year, season, day)] = 0;
    //             }
    //         }
    //     }

    //     // Run the aggregations
    //     foreach (StardewDate date in ShippedData.Keys)
    //     {
    //         int total = ShippedData[date].Sum(d => d.TotalSalePrice);
    //         yearTotals[date.Year] += total;
    //         seasonTotals[(date.Year, date.Season)] += total;
    //         dayTotals[date] += total;
    //         if (total > highestTotalPerSeason[(date.Year, date.Season)])
    //         {
    //             highestTotalPerSeason[(date.Year, date.Season)] = total;
    //         }
    //         highestOverallTotal = Math.Max(highestOverallTotal, total);
    //     }

    //     _seasonGrid = new();
    //     for (int i = Game1.year; i > 0; i--)
    //     {
    //         _seasonGrid.Add(i + "");
    //         foreach (Season season in Enum.GetValues(typeof(Season)))
    //         {
    //             _seasonGrid.Add(seasonTotals[(i, season)] + "");
    //         }
    //         _seasonGrid.Add(yearTotals[i] + "");
    //     }

    //     _dayGrid = new();
    //     var maxRowHeight = 128;
    //     var minRowHeight = 1;
    //     var rowWidth = 20;
    //     Season[] seasons = (Season[])Enum.GetValues(typeof(Season));
    //     Array.Reverse(seasons);
    //     for (int year = Game1.year; year > 0; year--)
    //     {
    //         List<(ChartElement, List<ChartElement>)> seasonsPerYear = new();
    //         _dayGrid.Add((year, seasonsPerYear));
    //         foreach (Season season in seasons)
    //         {
    //             List<ChartElement> daysPerSeason = new();
    //             ChartElement seasonElement = new(season.ToString(), "", "",
    //                 season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);

    //             seasonsPerYear.Add((seasonElement, daysPerSeason));
    //             for (int day = 1; day <= 28; day++)
    //             {
    //                 var highest = Math.Max(1, highestOverallTotal);
    //                 var dayTotal = dayTotals[new StardewDate(year, season, day)];
    //                 var rowHeight = 1.0;
    //                 if (dayTotal > 0)
    //                 {
    //                     var scale = (float)dayTotal / highest;
    //                     rowHeight = Math.Max(minRowHeight, scale * maxRowHeight);
    //                 }
    //                 string layout = $"{rowWidth}px {rowHeight}px";
    //                 string tooltip = $"{season}-{day}: {dayTotal}g";
    //                 ChartElement dayGridElement = new("", layout, tooltip,
    //                     season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);

    //                 daysPerSeason.Add(dayGridElement);
    //             }
    //         }
    //     }
    // }
    private void LoadAggregationAndView()
    {
        // Stardew UI doesnt allow indexing, so we have to create a hierarchical data model to use.
        // First we do aggregations then we create the view model.
        // Make types work for you!

        Dictionary<StardewYear, int> totalByYear = [];
        Dictionary<StardewYearSeason, int> totalBySeason = [];
        Dictionary<StardewDate, int> totalByDate = [];
        int highestOverallTotal = 1;

        // Make sure we have data for every grid cell, even if no entries in file
        for (int year = 1; year <= Game1.year; year++)
        {
            totalByYear[new StardewYear(year)] = 0;
            foreach (Season season in Enum.GetValues(typeof(Season)))
            {
                totalBySeason[new StardewYearSeason(year, season)] = 0;
                for (int day = 1; day <= 28; day++)
                {
                    totalByDate[new StardewDate(year, season, day)] = 0;
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

        // Construct grid for Totals View
        _totalsGrid = [];
        for (int i = Game1.year; i > 0; i--)
        {
            
            var yearDisplayElement = new ChartElement(totalByYear[new StardewYear(i)] + "");
            var seasonsForYear = new List<SeasonEntry<ChartElement, ChartElement>>();
            var yearEntry = new YearEntry<ChartElement,List<SeasonEntry<ChartElement, ChartElement>>>(i, yearDisplayElement, seasonsForYear);
            foreach (Season season in Enum.GetValues(typeof(Season)))
            {
                seasonsForYear.Add(
                    new SeasonEntry<ChartElement, ChartElement>(
                        season, 
                        new ChartElement(season + ""), 
                        new ChartElement(totalBySeason[new StardewYearSeason(i, season)] + "")));
            }
        }

        // Construct grid for day view
        _dayGrid = [];
        // _cashFlowGrid = [];
        var maxRowHeight = 128;
        var minRowHeight = 1;
        var rowWidth = 20;

        // Want to display in reverse order
        Season[] seasons = (Season[])Enum.GetValues(typeof(Season));
        Array.Reverse(seasons);
        for (int year = Game1.year; year > 0; year--)
        {
            List<SeasonEntry<ChartElement, List<DayEntry<ChartElement, object?>>>> seasonsPerYear = [];
            _dayGrid.Add(new YearEntry<ChartElement,List<SeasonEntry<ChartElement, List<DayEntry<ChartElement, object?>>>>>(year, new ChartElement(year + ""), seasonsPerYear));
            foreach (Season season in seasons)
            {
                List<DayEntry<ChartElement, object?>> daysPerSeason = [];
                ChartElement seasonElement = new(season.ToString(), "", "",
                    season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);
                var seasonEntry = new SeasonEntry<ChartElement, List<DayEntry<ChartElement, object?>>>(season, seasonElement, daysPerSeason);
                seasonsPerYear.Add(seasonEntry);
                for (int day = 1; day <= 28; day++)
                {
                    var date = new StardewDate(year, season, day);
                    var highest = Math.Max(1, highestOverallTotal);
                    var dayTotal = totalByDate[date];
                    var rowHeight = 1.0;
                    if (dayTotal > 0)
                    {
                        var scale = (float)dayTotal / highest;
                        rowHeight = Math.Max(minRowHeight, scale * maxRowHeight);
                    }
                    string layout = $"{rowWidth}px {rowHeight}px";
                    string tooltip = $"{season}-{day}: {dayTotal}g";
                    ChartElement dayGridElement = new("", layout, tooltip,
                        season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);

                    daysPerSeason.Add(new DayEntry<ChartElement, object?>(date, dayGridElement, null));
                }
            }
        }
    }

    // Abomination
    private List<YearEntry<ChartElement, List<SeasonEntry<ChartElement, ChartElement>>>>? _totalsGrid;
    public List<YearEntry<ChartElement, List<SeasonEntry<ChartElement, ChartElement>>>>? TotalsGrid
    {
        get
        {
            if (_totalsGrid == null)
            {
                LoadAggregationAndView();
            }
            return TotalsGrid;
        }
    }

    // Abomination
    private List<YearEntry<ChartElement,List<SeasonEntry<ChartElement, List<DayEntry<ChartElement, object?>>>>>>? _dayGrid;
    public List<YearEntry<ChartElement,List<SeasonEntry<ChartElement, List<DayEntry<ChartElement, object?>>>>>>? DayGrid
    {
        get
        {
            if (_dayGrid == null)
            {
                LoadAggregationAndView();
            }
            return _dayGrid;
        }
    }

    // Abomination
    // private List<YearEntry<SeasonEntry<DayEntry<InOutEntry>>>>? _cashFlowGrid;
    // public List<YearEntry<SeasonEntry<DayEntry<InOutEntry>>>>? CashFlowGrid
    // {
    //     get
    //     {
    //         if (_cashFlowGrid == null)
    //         {
    //             LoadAggregationAndView();
    //         }
    //         return _cashFlowGrid;
    //     }
    // }
}

internal record YearEntry<T,V>(int Year, T Display, V Value);
internal record SeasonEntry<T,V>(Season Season, T Display, V Value);
internal record DayEntry<T,V>(StardewDate Date, T Display, V Value);
internal record InOutEntry(ChartElement In, ChartElement Out);

// internal record YearToSeasons(int Year, List<SeasonToDays> Seasons);
// internal record SeasonToDays(ChartElement Season, List<ChartElement> Days);
internal record ChartElement(
        string Text = "", 
        string Layout = "", 
        string Tooltip = "", 
        bool IsSpring = false, 
        bool IsSummer = false, 
        bool IsFall = false, 
        bool IsWinter = false);

// ================================ 
// Tabs Stuff =====================
// ================================

internal enum ShipmentTab
{
    Today,
    Day,
    CashFlow,
    Totals
}

internal partial class ShipmentTabViewModel : INotifyPropertyChanged
{
    [Notify]
    private bool _isActive;

    public Tuple<int, int, int, int> Margin => IsActive ? new(0, 0, -12, 0) : new(0, 0, 0, 0);

    public ShipmentTab Value { get; }

    public ShipmentTabViewModel(ShipmentTab value, bool isActive)
    {
        Value = value;
        _isActive = isActive;
    }
}
