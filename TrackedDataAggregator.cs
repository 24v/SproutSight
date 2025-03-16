namespace SproutSight;

internal class TrackedDataAggregator(TrackedData TrackedData, Operation Operation, int[] SelectedYears)
{
    // We decompose the Elements here to make it easier to bind in the sml since dot operations are not allowed.

    public List<YearElement> WalletGrid { get; set; } = [];
    public List<YearElement> WalletGridReversed { get; set; } = [];
    public string? WalletText { get; private set; } = "";
    public string? WalletTooltip { get; private set; } = "";

    public List<YearElement> ShippedGrid { get; set; } = [];
    public List<YearElement> ShippedGridReversed { get; set; } = [];
    public int ShippedTotal { get; private set; } = 0;
    public string? ShippedText { get; private set; } = "";
    public string? ShippedTooltip { get; private set; } = "";

    public List<YearElement> CashFlowGrid { get; set; } = [];
    public List<YearElement> CashFlowGridReversed { get; set; } = [];
    public int CashFlowNetTotal { get; private set; } = 0;
    public string? CashFlowText { get; private set; } = "";
    public string? CashFlowTooltip { get; private set; } = "";

    public void LoadAggregationAndViewVisitor()
    {
        Logging.Monitor.Log("Starting LoadAggregationAndViewVisitor", LogLevel.Debug);

        // Create the year data structure
        List<YearNode> yearNodes = [];
        RootNode rootNode = new(yearNodes);
        if (SelectedYears.Length == 0)
        {
            return;
        }
        int[] yearsToFill;
        if (SelectedYears[0] == 0)
        {
            yearsToFill = [];
            for (int i = 1; i <= Game1.year; i++)
            {
                yearsToFill[i] = i;
            }
        } 
        else 
        {
            yearsToFill = SelectedYears;
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
        ShippedGridReversed = shippedRoot.YearElementsReversed;
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
        WalletGridReversed = walletRoot.YearElementsReversed;
        WalletText = walletRoot.Text;
        WalletTooltip = walletRoot.Tooltip;

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
        CashFlowGrid = cashFlowRoot.YearElements;
        CashFlowGridReversed = cashFlowRoot.YearElementsReversed;
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

// The tree that we will visit
internal record YearNode(int Year, List<SeasonNode> Seasons);
internal record SeasonNode(Season Season, int Year, List<DayNode> Days);
internal record DayNode(StardewDate Date);
internal record RootNode(List<YearNode> Years);
internal record RootElement(int Value, List<YearElement> YearElements, List<YearElement> YearElementsReversed, 
    string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null,
    string? Text2 = null, string? Layout2 = null, string? Tooltip2 = null, string? Tint2 = null
);
internal record YearElement(int Year, int Value, List<SeasonElement> SeasonElements, List<SeasonElement> SeasonElementsReversed, 
    string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null,
    string? Text2 = null, string? Layout2 = null, string? Tooltip2 = null, string? Tint2 = null
);

// TODO: Fix this trash
internal record SeasonElement(Season Season, int Value, List<DayElement> DayElements, List<DayElement> DayElementsReversed, 
    string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null, 
    bool IsSpring = false, bool IsSummer = false, bool IsFall = false, bool IsWinter = false,
    string? Text2 = null, string? Layout2 = null, string? Tooltip2 = null, string? Tint2 = null
);
internal record DayElement(StardewDate Date, int Value, 
        string? Text = null, string? Layout = null, string? Tooltip = null, string? Tint = null,
        string? Text2 = null, string? Layout2 = null, string? Tooltip2 = null, string? Tint2 = null

);
