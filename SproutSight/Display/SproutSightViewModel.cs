using PropertyChanged.SourceGenerator;

namespace SproutSight.Display;

internal partial class SproutSightViewModel
{

    // Showing Today's Info
    public StardewDate Date = StardewDate.GetTodaysDate();
    public List<TrackedItemStack> CurrentItems { get; internal set; } = new();
    public int TodayGoldIn;
    public int TodayGoldOut;
    public bool ShippedSomething => CurrentItems.Count > 0;
    public string TotalProceeds => $"Current Shipped: {DisplayHelper.FormatGoldNumber(CurrentItems.Select(item => item.StackCount * item.SalePrice).Sum())}";

    private readonly TrackedData trackedData;

    [Notify]
    private TrackedDataAggregator _trackedDataAggregator = null!;

    private void OnParamsChange()
    {
        var agg = new TrackedDataAggregator(trackedData, SelectedOperation, GetSelectedYearsArray());
        agg.LoadAggregationAndViewVisitor();
        TrackedDataAggregator = agg;
    }

    public SproutSightViewModel(TrackedData trackedData) {
        this.trackedData = trackedData;
        _operations = GetAvailableOperations();

        _yearSelectionOptions = [new YearSelectionViewModel(YearSelectionViewModel.YEAR_ALL, false)];
        for (int i = Game1.year ; i >= 1; i-- )
        {
            bool selected = i > Game1.year - DisplayHelper.DefaultNumYearsSelected;
            _yearSelectionOptions.Add(new YearSelectionViewModel(i, selected));
        }

        OnParamsChange();
    }


    [Notify]
    public Operation[] _operations = Enum.GetValues<Operation>();
    [Notify] 
    private Operation _selectedOperation = Operation.Sum;
    private void OnSelectedOperationChanged(Operation oldValue, Operation newValue)
    {
        OnParamsChange();
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
        Operations = GetAvailableOperations();
        OnParamsChange();
    }

    public Operation[] GetAvailableOperations()
    {
        var allOperations = Enum.GetValues(typeof(Operation)).Cast<Operation>().ToArray();
        
        return SelectedTab switch
        {
            ShipmentTab.Shipping => allOperations.Where(op => op != Operation.End).ToArray(),
            ShipmentTab.Wallet => allOperations.Where(op => op != Operation.Sum).ToArray(),
            ShipmentTab.CashFlow => allOperations.Where(op => op != Operation.End).ToArray(),
            _ => allOperations
        };
    }

    [Notify] private bool isYearSelectionExpanded;

    [Notify]
    private List<YearSelectionViewModel> _yearSelectionOptions { get; set; }

    public int[] GetSelectedYearsArray()
    {
        return YearSelectionOptions.Where(y => y.IsChecked).Select(y => y.Year).ToArray();
    }

    public void SelectYear(int ClickedYear)
    {
        List<YearSelectionViewModel> newOptions = [];
        for (int i = 0; i < YearSelectionOptions.Count; i ++)
        {
            newOptions.Add(new YearSelectionViewModel(YearSelectionOptions[i].Year,  YearSelectionOptions[i].IsChecked));
        }

        if (ClickedYear == 0)
        {
            // A click on all clears all other options.
            newOptions[0].IsChecked = !newOptions[0].IsChecked;
            foreach (var option in newOptions)
            {
                if (option.Year != 0)
                {
                    option.IsChecked = false;
                }
            }
        } 
        else
        {
            // A click on any other option will uncheck "All" option.
            newOptions[0].IsChecked = false;
            foreach (var option in newOptions)
            {
                if (option.Year == ClickedYear)
                {
                    option.IsChecked = !option.IsChecked;
                }
            }
        }
        YearSelectionOptions = newOptions; 
        OnParamsChange();
    }
}