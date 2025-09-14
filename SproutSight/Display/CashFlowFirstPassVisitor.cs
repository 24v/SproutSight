namespace SproutSight.Display;
internal class CashFlowFirstPassVisitor(Dictionary<StardewDate, GoldInOut> cashFlowByDate, Operation operation, StardewDate upToDate)
{
    // Highest values for income
    public int HighestDayInValue { get; private set; } = 1;
    public int HighestSeasonInValue { get; private set; } = 1;
    public int HighestYearInValue { get; private set; } = 1;
    
    // Highest values for expenses
    public int HighestDayOutValue { get; private set; } = 1;
    public int HighestSeasonOutValue { get; private set; } = 1;
    public int HighestYearOutValue { get; private set; } = 1;

    public AggValue Visit(DayNode day)
    {
        int dayIn = 0;
        int dayOut = 0;
        
        if (cashFlowByDate.TryGetValue(day.Date, out var goldInOut))
        {
            dayIn = goldInOut.In;
            dayOut = Math.Abs(goldInOut.Out);
            HighestDayInValue = Math.Max(HighestDayInValue, dayIn);
            HighestDayOutValue = Math.Max(HighestDayOutValue, dayOut);
        }
        
        return new AggValue(dayIn, day.Date.IsBefore(upToDate), 1, dayOut);
    }

    public AggValue Visit(SeasonNode season)
    {
        var aggValues = season.Days.Select(Visit).ToList();
        var aggValue = CalculateAggValue(aggValues);
        HighestSeasonInValue = Math.Max(HighestSeasonInValue, aggValue.Value);
        HighestSeasonOutValue = Math.Max(HighestSeasonOutValue, aggValue.Value2);
        return aggValue;
    }

    public AggValue Visit(YearNode year)
    {
        var aggValues = year.Seasons.Select(Visit).ToList();
        var aggValue = CalculateAggValue(aggValues);
        HighestYearInValue = Math.Max(HighestYearInValue, aggValue.Value);
        HighestYearOutValue = Math.Max(HighestYearOutValue, aggValue.Value2);
        return aggValue;
    }
    private AggValue CalculateAggValue(List<AggValue> aggValues)
    {
        var validAggValues = aggValues.Where(a => a.IsValid).ToList();
        var totalDaysCovered = validAggValues.Select(a => a.TotalNumberOfDaysCovered).Sum();

        var inValuesToAggregate = validAggValues.Select(a => a.Value).ToList();
        if (operation == Operation.Average)
        {
            inValuesToAggregate = validAggValues.Select(a => a.Value * a.TotalNumberOfDaysCovered).ToList();
        }
        int inAggregationValue = DoOperation(inValuesToAggregate, true, totalDaysCovered);

        var outValuesToAggregate = validAggValues.Select(a => a.Value2).ToList();
        if (operation == Operation.Average)
        {
            outValuesToAggregate = validAggValues.Select(a => a.Value2 * a.TotalNumberOfDaysCovered).ToList();
        }
        int outAggregationValue = DoOperation(outValuesToAggregate, false, totalDaysCovered);
        return new AggValue(inAggregationValue, totalDaysCovered > 0, totalDaysCovered, outAggregationValue);
    }

    public void Visit(RootNode root)
    {
        root.Years.ForEach(e => Visit(e));
    }

    public int DoOperation(List<int> entries, bool forInValues, int? countOverride = null)
    {
        if (entries.Count == 0)
        {
            return 0;
        }
        return operation switch
        {
            Operation.Min => forInValues? entries.Min() : entries.Max(),
            Operation.Max => forInValues? entries.Max() : entries.Min(),
            Operation.Sum => entries.Sum(),
            Operation.Average => (int)Math.Round(entries.Sum() / 
                (float)(countOverride != null ? countOverride : entries.Count)),
            Operation.End => entries.Last(),
            _ => 0
        };
    }
}