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
// TODO: Should averages include the full year, or only up to certain date?

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

    private readonly TrackedData trackedData;

    [Notify]
    private TrackedDataAggregator _trackedDataAggregator;

    public SproutSightViewModel(TrackedData trackedData) {
        this.trackedData = trackedData;
        _trackedDataAggregator = new(trackedData, SelectedOperation);
        _trackedDataAggregator.LoadAggregationAndViewVisitor();
        _operations = GetAvailableOperations();
    }
    public static string FormatGoldNumber(int number)
    {
        return $"{number.ToString("N0")}g";
    }

    [Notify]
    public Operation[] _operations = Enum.GetValues<Operation>();
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
        if (SelectedTab == ShipmentTab.Wallet)
        {
            SelectedOperation = Operation.End;
        }
        else
        {
            SelectedOperation = Operation.Sum;
        }
        
        // Refresh data with the new operation
        OnSelectedOperationChanged(SelectedOperation, SelectedOperation);
        Operations = GetAvailableOperations();
    }

    public Operation[] GetAvailableOperations()
    {
        var allOperations = Enum.GetValues(typeof(Operation)).Cast<Operation>().ToArray();
        
        return SelectedTab switch
        {
            ShipmentTab.Shipping => allOperations.Where(op => op != Operation.End).ToArray(),
            ShipmentTab.Wallet => allOperations.Where(op => op != Operation.Sum).ToArray(),
            ShipmentTab.CashFlow => allOperations,
            _ => allOperations
        };
    }
}