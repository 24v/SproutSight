namespace SproutSight;

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
