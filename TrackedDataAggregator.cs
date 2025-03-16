namespace SproutSight;

internal class TrackedDataAggregator(TrackedData TrackedData, Operation Operation, int[] SelectedYears)
{
    // We decompose the RootElements here to make it easier to bind in the sml since dot operations are not allowed.

    public List<YearElement> WalletYears { get; set; } = [];
    public List<YearElement> WalletYearsReversed { get; set; } = [];
    public string? WalletText { get; private set; } = "";

    public List<YearElement> ShippedYears { get; set; } = [];
    public List<YearElement> ShippedYearsReversed { get; set; } = [];
    public int ShippedTotal { get; private set; } = 0;
    public string? ShippedText { get; private set; } = "";

    public List<YearElement> CashFlowYears { get; set; } = [];
    public List<YearElement> CashFlowYearsReversed { get; set; } = [];
    public int CashFlowNetTotal { get; private set; } = 0;
    public string? CashFlowText { get; private set; } = "";
    public string? CashFlowTooltip { get; private set; } = "";

    public void LoadAggregationAndViewVisitor()
    {

        // Create the year data structure
        List<YearNode> yearNodes = [];
        RootNode rootNode = new(yearNodes);

        int[] yearsToFill;
        if (SelectedYears.Contains(0))
        {
            yearsToFill = new int[Game1.year];
            for (int i = 0; i < Game1.year ; i++)
            {
                yearsToFill[i] = i + 1;
            }
        } 
        else 
        {
            yearsToFill = SelectedYears.Reverse().ToArray();
        }
        

        foreach (var year in yearsToFill)
        {
            List<SeasonNode> seasonNodes = [];
            yearNodes.Add(new YearNode(year, seasonNodes));
            foreach (Season season in Enum.GetValues(typeof(Season)))
            {
                List<DayNode> dayNodes = [];
                seasonNodes.Add(new SeasonNode(season, year, dayNodes));
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
        shippedFirstPassVisitor.Visit(rootNode);
        var shippedVisitor = Visitors.CreateShippedVisitor(
                TrackedData.ShippedData, 
                Operation, 
                shippedFirstPassVisitor.HighestDayValue, 
                shippedFirstPassVisitor.HighestSeasonValue, 
                shippedFirstPassVisitor.HighestYearValue);
        var shippedRoot = shippedVisitor.Visit(rootNode);
        ShippedYears = shippedRoot.YearElements;
        ShippedYearsReversed = shippedRoot.YearElementsReversed;
        ShippedTotal = shippedRoot.Value;
        ShippedText = shippedRoot.Text;

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
        WalletYears = walletRoot.YearElements;
        WalletYearsReversed = walletRoot.YearElementsReversed;
        WalletText = walletRoot.Text;

        var cashFlowFirstPassVisitor = FirstPassVisitors.CreateCashFlowVisitor(
                TrackedData.GoldInOut, 
                Operation);
        cashFlowFirstPassVisitor.Visit(rootNode);
        var cashFlowVisitor = Visitors.CreateCashFlowVisitor(
                TrackedData.GoldInOut, 
                Operation,
                cashFlowFirstPassVisitor.HighestDayInValue,
                cashFlowFirstPassVisitor.HighestSeasonInValue,
                cashFlowFirstPassVisitor.HighestYearInValue,
                cashFlowFirstPassVisitor.HighestDayOutValue,
                cashFlowFirstPassVisitor.HighestSeasonOutValue,
                cashFlowFirstPassVisitor.HighestYearOutValue);
        var cashFlowRoot = cashFlowVisitor.VisitCashFlow(rootNode);
        CashFlowYears = cashFlowRoot.YearElements;
        CashFlowYearsReversed = cashFlowRoot.YearElementsReversed;
        CashFlowNetTotal = cashFlowRoot.Value;
        CashFlowText = cashFlowRoot.Text;
        CashFlowTooltip = cashFlowRoot.Tooltip;
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

// The tree that we will visit
internal record YearNode(int Year, List<SeasonNode> Seasons);
internal record SeasonNode(Season Season, int Year, List<DayNode> Days);
internal record DayNode(StardewDate Date);
internal record RootNode(List<YearNode> Years);

internal record AggValue(int Value, bool IsValid, int TotalNumberOfDaysCovered, int? Value2 = null);

internal record RootElement(int Value, List<YearElement> YearElements, List<YearElement> YearElementsReversed, 
    string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null,
    string? Text2 = null, string? Layout2 = null, string? Tooltip2 = null, string? Tint2 = null
);
internal record YearElement(int Year, AggValue Value, List<SeasonElement> SeasonElements, List<SeasonElement> SeasonElementsReversed, 
    string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null,
    string? Text2 = null, string? Layout2 = null, string? Tooltip2 = null, string? Tint2 = null
);

// TODO: Fix this trash
internal record SeasonElement(Season Season, AggValue Value, List<DayElement> DayElements, List<DayElement> DayElementsReversed, 
    string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null, 
    bool IsSpring = false, bool IsSummer = false, bool IsFall = false, bool IsWinter = false,
    string? Text2 = null, string? Layout2 = null, string? Tooltip2 = null, string? Tint2 = null
);
internal record DayElement(StardewDate Date, AggValue Value, 
        string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null,
        string? Text2 = null, string? Layout2 = null, string? Tooltip2 = null, string? Tint2 = null

);
