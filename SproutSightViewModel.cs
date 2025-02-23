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
    public StardewDate Date = StardewDate.GetStardewDate();
    public List<TrackedItemStack> CurrentItems { get; internal set; } = new();
    public int TodayGoldIn;
    public int TodayGoldOut;
    public bool ShippedSomething => CurrentItems.Count > 0;
    public string TotalProceeds => $"Current Shipped: {CurrentItems.Select(item => item.StackCount * item.SalePrice).Sum()}g";
    public TrackedData TrackedData { get; internal set; } = new();

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

/// <summary>
///  Extend main class to add some view only helpers
/// </summary>
internal partial class TrackedItemStack
{
    public string FormattedSale => $"({StackCount}x{SalePrice}g)";
}

/// <summary>
/// Extend main class to add some view only helpers
/// </summary>
internal partial class TrackedData
{
    private void LoadAll()
    {
        // We need:
        // season totals [year, spring, summer, fall, winter, year, ...]
        // day totals [year, ]

        // TODO: I think we can dedup this 
        // TODO: Make all grid elements, get rid of tuples, see if this can be simplified

        Dictionary<int, int> yearTotals = new();
        Dictionary<(int, Season), int> seasonTotals = new();
        Dictionary<StardewDate, int> dayTotals = new();
        Dictionary<(int, Season), int> highestTotalPerSeason = new();
        int highestOverallTotal = 1;
        // Make sure we have data for every grid cell, even if no entries in file
        for (int year = 1; year <= Game1.year; year++)
        {
            yearTotals[year] = 0;
            foreach (Season season in Enum.GetValues(typeof(Season)))
            {
                seasonTotals[(year, season)] = 0;
                highestTotalPerSeason[(year, season)] = 0;
                for (int day = 1; day <= 28; day++)
                {
                    dayTotals[new StardewDate(year, season, day)] = 0;
                }
            }
        }

        foreach (StardewDate date in ShippedData.Keys)
        {
            int total = ShippedData[date].Sum(d => d.TotalSalePrice);
            yearTotals[date.Year] += total;
            seasonTotals[(date.Year, date.Season)] += total;
            dayTotals[date] += total;
            if (total > highestTotalPerSeason[(date.Year, date.Season)])
            {
                highestTotalPerSeason[(date.Year, date.Season)] = total;
            }
            highestOverallTotal = Math.Max(highestOverallTotal, total);
        }

        _seasonGrid = new();
        for (int i = Game1.year; i > 0; i--)
        {
            _seasonGrid.Add(i + "");
            foreach (Season season in Enum.GetValues(typeof(Season)))
            {
                _seasonGrid.Add(seasonTotals[(i, season)] + "");
            }
            _seasonGrid.Add(yearTotals[i] + "");
        }

        _dayGrid = new();
        var maxRowHeight = 128;
        var minRowHeight = 1;
        Season[] seasons = (Season[])Enum.GetValues(typeof(Season));
        Array.Reverse(seasons); 
        for (int year = Game1.year; year > 0; year--)
        {
            List<(DayGridElement, List<DayGridElement>)> seasonsPerYear = new();
            _dayGrid.Add((year, seasonsPerYear));
            foreach (Season season in seasons)
            {
                List<DayGridElement> daysPerSeason = new();
                DayGridElement seasonElement = new(season.ToString(), "", "",
                    season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);

                seasonsPerYear.Add((seasonElement, daysPerSeason));
                for (int day = 1; day <= 28; day++)
                {
                    var highest = Math.Max(1, highestOverallTotal);
                    var dayTotal = dayTotals[new StardewDate(year, season, day)];
                    var rowHeight = 1.0;
                    if (dayTotal > 0)
                    {
                        var scale = (float)dayTotal / highest;
                        rowHeight = Math.Max(minRowHeight, scale * maxRowHeight);
                    }
                    string layout = $"20px {rowHeight}px";
                    string tooltip = $"{season}-{day}: {dayTotal}g";
                    DayGridElement dayGridElement = new("", layout, tooltip,
                        season == Season.Spring, season == Season.Summer, season == Season.Fall, season == Season.Winter);

                    daysPerSeason.Add(dayGridElement);
                }
            }
        }
    }

    private List<string>? _seasonGrid;

    public List<string>? SeasonGrid
    {
        get
        {
            if (_seasonGrid == null)
            {
                LoadAll();
            }
            return _seasonGrid;
        }
    }

    private List<(int, List<(DayGridElement, List<DayGridElement>)>)>? _dayGrid;

    public List<(int, List<(DayGridElement, List<DayGridElement>)>)>? DayGrid
    {
        get
        {
            if (_dayGrid == null)
            {
                LoadAll();
            }
            return _dayGrid;
        }
    }

}

internal record DayGridElement(string Text, string Layout, string Tooltip, bool isSpring, bool isSummer, bool isFall, bool isWinter);

// ================================ 
// Tabs Stuff =====================
// ================================

internal enum ShipmentTab
{
    Today,
    Day,
    Year,
    CashFlow,
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
