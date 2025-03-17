namespace SproutSight.Display;

internal class SingleValueFirstPassVisitor(Operation operation, Func<StardewDate, int> getDayValue, StardewDate upToDate) 
{
    public int HighestDayValue { get; private set; } = 0;
    public int HighestSeasonValue { get; private set; } = 0;
    public int HighestYearValue { get; private set; } = 0;

    public AggValue Visit(DayNode day)
    {
        var dayValue = getDayValue(day.Date);
        HighestDayValue = Math.Max(HighestDayValue, dayValue);
        var valid = day.Date.IsBefore(upToDate);
        return new AggValue(dayValue, valid, 1);
    }

    public AggValue Visit(SeasonNode season)
    {
        var dayAggValues = season.Days
            .Select(Visit)
            .Where(a => a.IsValid)
            .ToList(); 
        var aggValue = CalculateAggValue(dayAggValues);
        HighestSeasonValue = Math.Max(HighestSeasonValue, aggValue.Value);
        return aggValue;
    }

    public AggValue Visit(YearNode year)
    {
        var seasonAggValues = year.Seasons
            .Select(Visit)
            .Where(a => a.IsValid)
            .ToList(); 
        var aggValue = CalculateAggValue(seasonAggValues);
        HighestYearValue = Math.Max(HighestYearValue, aggValue.Value);
        return aggValue;
    }

    public void Visit(RootNode root)
    {
        root.Years.ForEach(e => Visit(e));
    }

    private AggValue CalculateAggValue(List<AggValue> aggValues)
    {
        var validAggValues = aggValues.Where(a => a.IsValid).ToList();
        var totalDaysCovered = validAggValues.Select(a => a.TotalNumberOfDaysCovered).Sum();
        var valuesToAggregate = validAggValues.Select(a => a.Value).ToList();
        if (operation == Operation.Average)
        {
            valuesToAggregate = validAggValues.Select(a => a.Value * a.TotalNumberOfDaysCovered).ToList();
        }
        int aggregationValue = DoOperation(valuesToAggregate, totalDaysCovered);
        return new AggValue(aggregationValue, totalDaysCovered > 0, totalDaysCovered);
    }

    private int DoOperation(List<int> entries, int? countOverride = null)
    {
        if (entries.Count == 0)
        {
            return 0;
        }
        return operation switch
        {
            Operation.Min => entries.Min(),
            Operation.Max => entries.Max(),
            Operation.Sum => entries.Sum(),
            Operation.Average => (int)Math.Round(entries.Sum() / 
                (float)(countOverride != null ? countOverride : entries.Count)),
            Operation.End => entries.Last(),
            _ => 0
        };
    }
}

