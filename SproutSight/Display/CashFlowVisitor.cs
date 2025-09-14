namespace SproutSight.Display;
internal class CashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation, StardewDate upToDate,
                      int highestDayIn, int highestSeasonIn, int highestYearIn,
                      int highestDayOut, int highestSeasonOut, int highestYearOut)
{
    public Dictionary<StardewDate, GoldInOut> CashFlowByDate { get; } = goldInOut;

    public int HighestDayValue => Math.Max(1, Math.Max(highestDayIn, highestDayOut));
    public int HighestSeasonValue => Math.Max(1, Math.Max(highestSeasonIn, highestSeasonOut));
    public int HighestYearValue => Math.Max(1, Math.Max(highestYearIn, highestYearOut));

    public int DoOperation(List<int> entries, bool forInValues, int? countOverride = null)
    {
        if (entries.Count == 0)
        {
            return 0;
        }
        return operation switch
        {
            Operation.Min => forInValues ? entries.Min() : entries.Max(),
            Operation.Max => forInValues ? entries.Max() : entries.Min(),
            Operation.Sum => entries.Sum(),
            Operation.Average => (int)Math.Round(entries.Sum() /
                (float)(countOverride != null ? countOverride : entries.Count)),
            Operation.End => entries.Last(),
            _ => 0
        };
    }

    private AggValue DoAggregation(List<AggValue> aggValues)
    {
        var totalDaysCovered = aggValues
            .Where(e => e.IsValid)
            .Select(e => e.TotalNumberOfDaysCovered)
            .Sum();

        List<int> cashFlowInValues;
        List<int> cashFlowOutValues;
        if (operation == Operation.Average)
        {
            cashFlowInValues = aggValues
                .Where(e => e.IsValid)
                .Select(e => e.Value * e.TotalNumberOfDaysCovered)
                .ToList();
            cashFlowOutValues = aggValues
                .Where(e => e.IsValid)
                .Select(e => e.Value2 * e.TotalNumberOfDaysCovered)
                .ToList();
        }
        else 
        {
            cashFlowInValues = aggValues
                .Where(e => e.IsValid)
                .Select(e => e.Value)
                .ToList();
            cashFlowOutValues = aggValues
                .Where(e => e.IsValid)
                .Select(e => e.Value2)
                .ToList();
        }

        int aggregatedIn = DoOperation(cashFlowInValues, true, totalDaysCovered);
        int aggregatedOut = DoOperation(cashFlowOutValues, false,  totalDaysCovered);
        int netValue = aggregatedIn - aggregatedOut;
        return new AggValue(aggregatedIn, totalDaysCovered > 0, totalDaysCovered, aggregatedOut, netValue);
    }

    public DayElement VisitCashFlow(DayNode day)
    {
        int dayIn = 0;
        int dayOut = 0;
        
        if (CashFlowByDate.TryGetValue(day.Date, out var goldInOut))
        {
            dayIn = goldInOut.In;
            dayOut = Math.Abs(goldInOut.Out);
        }

        string tooltip;
        bool valid;
        string inTint;
        string outTint;
        string inLayout;
        string outLayout;
        if (day.Date.IsBefore(upToDate))
        {
            tooltip = $"{day.Date.Season}-{day.Date.Day}\n" +
                    $"Net: {DisplayHelper.FormatGoldNumber(dayIn - dayOut)}\n" +
                    $"In: {DisplayHelper.FormatGoldNumber(dayIn)}\n" +
                    $"Out: {DisplayHelper.FormatGoldNumber(dayOut)}";
            inTint = DisplayHelper.CashFlowInTint;
            outTint = DisplayHelper.CashFlowOutTint;
            int inRowHeight = DisplayHelper.CalculateRowHeight(dayIn, HighestDayValue);
            inLayout = DisplayHelper.FormatLayout(inRowHeight);
            int outRowHeight = DisplayHelper.CalculateRowHeight(dayOut, HighestDayValue);
            outLayout = DisplayHelper.FormatLayout(outRowHeight);
            valid = true;
        }
        else if (day.Date.Equals(upToDate))
        {
            tooltip = $"{day.Date.Season}-{day.Date.Day}: Today! Check back tomorrow. (Data is saved at the end of the day.)";
            inTint = DisplayHelper.TodayTint;
            outTint = DisplayHelper.TodayTint;
            inLayout = DisplayHelper.FormatLayout(DisplayHelper.MinRowHeight + 5);
            outLayout = DisplayHelper.FormatLayout(DisplayHelper.MinRowHeight + 5);
            valid = false;
        }
        else 
        {
            tooltip = $"{day.Date.Season}-{day.Date.Day}: The Future! No data yet!";
            inTint = DisplayHelper.FutureTint;
            outTint = DisplayHelper.FutureTint;
            inLayout = DisplayHelper.FormatLayout(DisplayHelper.MinRowHeight);
            outLayout = DisplayHelper.FormatLayout(0);
            valid = false;
        } 

        var returnValue = new AggValue(dayIn, valid, 1, dayOut);

        return new DayElement(day.Date, returnValue, "", inLayout, tooltip, inTint, "", outLayout, tooltip, outTint);
    }



    public SeasonElement VisitCashFlow(SeasonNode season)
    {
        var elements = season.Days.Select(VisitCashFlow).ToList();
        var aggValue = DoAggregation(elements.Select(e => e.Value).ToList());

        var aggregatedIn = aggValue.Value;
        var aggregatedOut = aggValue.Value2;
        var netValue = aggValue.Value3;

        int inRowHeight = DisplayHelper.CalculateRowHeight(aggregatedIn, HighestSeasonValue);
        string inLayout = DisplayHelper.FormatLayout(inRowHeight);
        int outRowHeight = DisplayHelper.CalculateRowHeight(aggregatedOut, HighestSeasonValue);
        string outLayout = DisplayHelper.FormatLayout(outRowHeight);

        string inTint;
        string outTint;
        string tooltip;
        if (upToDate.Year == season.Year && upToDate.Season == season.Season)
        {
            if (operation == Operation.Min || operation == Operation.Max)
            {
                tooltip = $"{season.Season} Y-{season.Year} {operation}:\n" +
                        $"In: {DisplayHelper.FormatGoldNumber(aggregatedIn)}\n" +
                        $"Out: {DisplayHelper.FormatGoldNumber(aggregatedOut)}\n" +
                        "(Season in Progress)";
            }
            else 
            {
                tooltip = $"{season.Season} Y-{season.Year} {operation}:\n" +
                        $"Net: {DisplayHelper.FormatGoldNumber(netValue)}\n" +
                        $"In: {DisplayHelper.FormatGoldNumber(aggregatedIn)}\n" +
                        $"Out: {DisplayHelper.FormatGoldNumber(aggregatedOut)}\n" +
                        "(Season in Progress)";
            }
            inTint = DisplayHelper.TodayTint; 
            outTint = DisplayHelper.TodayTint;
        } 
        else if (aggValue.IsValid)
        {
            if (operation == Operation.Min || operation == Operation.Max)
            {
                tooltip = $"{season.Season} Y-{season.Year} {operation}:\n" +
                        $"In: {DisplayHelper.FormatGoldNumber(aggregatedIn)}\n" +
                        $"Out: {DisplayHelper.FormatGoldNumber(aggregatedOut)}";
            
            }
            else 
            {
                tooltip = $"{season.Season} Y-{season.Year} {operation}:\n" +
                        $"Net: {DisplayHelper.FormatGoldNumber(netValue)}\n" +
                        $"In: {DisplayHelper.FormatGoldNumber(aggregatedIn)}\n" +
                        $"Out: {DisplayHelper.FormatGoldNumber(aggregatedOut)}";
            }
            inTint = DisplayHelper.CashFlowInTint; 
            outTint = DisplayHelper.CashFlowOutTint;
        }
        else 
        {

            tooltip = $"{season.Season} Y-{season.Year} {operation}: Season in the future!";
            inTint = DisplayHelper.FutureTint;
            outTint = DisplayHelper.FutureTint;
        }

        var reversedElements = elements.ToList();
        reversedElements.Reverse();
        var cashFlowSeasonEntry = new SeasonElement(
                season.Season,  aggValue, elements, reversedElements, 
                season.Season.ToString(), inLayout, tooltip, inTint, 
                season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter,
                "", outLayout, tooltip, outTint);
        return cashFlowSeasonEntry;
    }
    
    public YearElement VisitCashFlow(YearNode year)
    {
        var elements = year.Seasons.Select(VisitCashFlow).ToList();
        var aggValue = DoAggregation(elements.Select(e => e.Value).ToList());

        var aggregatedIn = aggValue.Value;
        var aggregatedOut = aggValue.Value2;
        var netValue = aggValue.Value3;

        int inRowHeight = DisplayHelper.CalculateRowHeight(aggregatedIn, HighestYearValue);
        string inLayout = DisplayHelper.FormatLayout(inRowHeight);
        int outRowHeight = DisplayHelper.CalculateRowHeight(aggregatedOut, HighestYearValue);
        string outLayout = DisplayHelper.FormatLayout(outRowHeight);

        string inTint;
        string outTint;
        string tooltip;
        if (upToDate.Year == year.Year)
        {
            if (operation == Operation.Min || operation == Operation.Max)
            {
                tooltip = $"Y-{year.Year} {operation}:\n" +
                        $"In: {DisplayHelper.FormatGoldNumber(aggregatedIn)}\n" +
                        $"Out: {DisplayHelper.FormatGoldNumber(aggregatedOut)}\n" +
                        "(Year in Progress)";
            }
            else 
            {
                tooltip = $"Y-{year.Year} {operation}:\n" +
                        $"Net: {DisplayHelper.FormatGoldNumber(netValue)}\n" +
                        $"In: {DisplayHelper.FormatGoldNumber(aggregatedIn)}\n" +
                        $"Out: {DisplayHelper.FormatGoldNumber(aggregatedOut)}\n" +
                        "(Year in Progress)";
            }
            inTint = DisplayHelper.TodayTint; 
            outTint = DisplayHelper.TodayTint;
        } 
        else if (aggValue.IsValid)
        {
            if (operation == Operation.Min || operation == Operation.Max)
            {
                tooltip = $"Y-{year.Year} {operation}:\n" +
                        $"In: {DisplayHelper.FormatGoldNumber(aggregatedIn)}\n" +
                        $"Out: {DisplayHelper.FormatGoldNumber(aggregatedOut)}";
            
            }
            else 
            {
                tooltip = $"Y-{year.Year} {operation}:\n" +
                        $"Net: {DisplayHelper.FormatGoldNumber(netValue)}\n" +
                        $"In: {DisplayHelper.FormatGoldNumber(aggregatedIn)}\n" +
                        $"Out: {DisplayHelper.FormatGoldNumber(aggregatedOut)}";
            }
            inTint = DisplayHelper.CashFlowInTint; 
            outTint = DisplayHelper.CashFlowOutTint;
        }
        else 
        {

            tooltip = $"Y-{year.Year} {operation}: Season in the future!";
            inTint = DisplayHelper.FutureTint;
            outTint = DisplayHelper.FutureTint;
        }

        var reversedElements = elements.ToList();
        reversedElements.Reverse();
        var cashFlowYearEntry = new YearElement(
                year.Year, aggValue, elements, reversedElements, 
                year.Year.ToString(), inLayout, tooltip, inTint,
                "", outLayout, tooltip, outTint);
        return cashFlowYearEntry;
    }
    
    public RootElement VisitCashFlow(RootNode root)
    {
        var elements = root.Years.Select(VisitCashFlow).ToList();
        var aggValue = DoAggregation(elements.Select(e => e.Value).ToList());

        var aggregatedIn = aggValue.Value;
        var aggregatedOut = aggValue.Value2;
        var netValue = aggValue.Value3;

        string tooltip = $"Overall {operation}:\n" + 
                         $"Net: {DisplayHelper.FormatGoldNumber(netValue)}\n" + 
                         $"In: {DisplayHelper.FormatGoldNumber(aggregatedIn)}\n" + 
                         $"Out: {DisplayHelper.FormatGoldNumber(aggregatedOut)}";
        
        string text = $"Cash Flow {operation}";
        var reversedElements = elements.ToList();
        reversedElements.Reverse();
        var element = new RootElement(netValue, elements, reversedElements, 
                $"{operation} Cash Flow", null, tooltip, null, "", null, tooltip);

        return element;
    }
}
