namespace SproutSight.Display;
internal class SingleValueVisitor(Operation operation, StardewDate upToDate, 
        int highestOverallDayTotal, int highestOverallSeasonTotal, int highestOverallYearTotal, Func<StardewDate, int> getDayValue)
{
    public int HighestOverallDayTotal { get; protected set; } = Math.Max(1, highestOverallDayTotal);
    public int HighestOverallSeasonTotal { get; protected set; } = Math.Max(1, highestOverallSeasonTotal);
    public int HighestOverallYearTotal { get; protected set; } = Math.Max(1, highestOverallYearTotal);
    public int GetDayValue(StardewDate date) => getDayValue(date);

    public int DoOperation(List<int> entries, int? countOverride = null)
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

    private AggValue DoAggregation(List<AggValue> aggValues)
    {
        var totalDaysCovered = aggValues
            .Where(e => e.IsValid)
            .Select(e => e.TotalNumberOfDaysCovered)
            .Sum();

        List<int> values;
        if (operation == Operation.Average)
        {
            values = aggValues
                .Where(e => e.IsValid)
                .Select(e => e.Value * e.TotalNumberOfDaysCovered)
                .ToList();
        }
        else 
        {
            values = aggValues
                .Where(e => e.IsValid)
                .Select(e => e.Value)
                .ToList();
        }

        int aggregated = DoOperation(values,  totalDaysCovered);
        return new AggValue(aggregated, totalDaysCovered > 0, totalDaysCovered);
    }
    public virtual DayElement Visit(DayNode day)
    {
        var dayGold = GetDayValue(day.Date);
        int rowHeight = DisplayHelper.CalculateRowHeight(dayGold, HighestOverallDayTotal);

        string tooltip;
        string tint;
        string layout;
        bool valid;
        if (day.Date.IsBefore(upToDate))
        {
            tooltip = $"{day.Date.Season}-{day.Date.Day}: {DisplayHelper.FormatGoldNumber(dayGold)}";
            tint = DisplayHelper.GetTint(day.Date.Season);
            layout = DisplayHelper.FormatLayout(rowHeight);
            valid = true;
        }
        else if (day.Date.Equals(upToDate))
        {
            tooltip = $"{day.Date.Season}-{day.Date.Day}: Today! Check back tomorrow. (Data is saved at the end of the day.)";
            tint = DisplayHelper.TodayTint;
            // Make today a little larger than 0
            layout = DisplayHelper.FormatLayout(DisplayHelper.MinRowHeight + 5);
            valid = false;
        }
        else 
        {
            tooltip = $"{day.Date.Season}-{day.Date.Day}: The Future! No data yet!";
            tint = DisplayHelper.FutureTint;
            layout = DisplayHelper.FormatLayout(DisplayHelper.MinRowHeight);
            valid = false;
        } 

        AggValue aggValue = new(dayGold, valid, 1);
        return new DayElement(day.Date, aggValue, "<Not Used>", layout, tooltip, tint);
    }

    public virtual SeasonElement Visit(SeasonNode season)
    {
        var elements = season.Days.Select(Visit).ToList();
        var aggValue = DoAggregation(elements.Select(e => e.Value).ToList());
        var newAggregated = aggValue.Value;

        int rowHeight = DisplayHelper.CalculateRowHeight(newAggregated, HighestOverallSeasonTotal);
        string layout = DisplayHelper.FormatLayout(rowHeight);

        string tooltip;
        string tint;
        if (upToDate.Year == season.Year && upToDate.Season == season.Season)
        {
            tooltip = $"{season.Season} Y-{season.Year} {operation}: {DisplayHelper.FormatGoldNumber(newAggregated)}\n(Season in progress)";
            tint = DisplayHelper.TodayTint; 
        }
        else if (aggValue.IsValid)
        {
            tooltip = $"{season.Season} Y-{season.Year} {operation}: {DisplayHelper.FormatGoldNumber(newAggregated)}";
            tint = DisplayHelper.GetTint(season.Season);
        }
        else 
        {
            tooltip = $"{season.Season} Y-{season.Year} {operation}: Season in the future!";
            tint = DisplayHelper.FutureTint;
        }

        var reversedElements = elements.ToList();
        reversedElements.Reverse();
        var seasonElement = new SeasonElement(
                season.Season, aggValue, elements, reversedElements,
                season + "", layout, tooltip, tint, 
                season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter);

        return seasonElement;
    }

    public virtual YearElement Visit(YearNode year)
    {
        var elements = year.Seasons.Select(Visit).ToList();
        var aggValue = DoAggregation(elements.Select(e => e.Value).ToList());
        var newAggregated = aggValue.Value;

        int rowHeight = DisplayHelper.CalculateRowHeight(newAggregated, HighestOverallYearTotal);
        string layout = DisplayHelper.FormatLayout(rowHeight);

        string tooltip;
        string tint;
        if (upToDate.Year == year.Year) 
        {
            tooltip = $"Y-{year.Year} {operation}: {DisplayHelper.FormatGoldNumber(newAggregated)}\n(Year in progress)";
            tint = DisplayHelper.TodayTint;
        }
        else if (aggValue.IsValid)
        {
            tooltip = $"Y-{year.Year} {operation}: {DisplayHelper.FormatGoldNumber(newAggregated)}";        
            tint = DisplayHelper.YearTint;
        }
        else 
        {
            tooltip = $"Y-{year.Year} {operation}: Year is in the future! This should not happen!";        
            tint = DisplayHelper.FutureTint;
        }

        var reversedElements = elements.ToList();
        reversedElements.Reverse();
        var yearElement = new YearElement(year.Year, aggValue, elements, 
                reversedElements, year.Year + "", layout, tooltip, tint);
        return yearElement;
    }
    
    public virtual RootElement Visit(RootNode root)
    {
        var elements = root.Years.Select(Visit).ToList();
        var aggValue = DoAggregation(elements.Select(e => e.Value).ToList());
        var newAggregated = aggValue.Value;

        string text = $"Overall {operation}: {DisplayHelper.FormatGoldNumber(newAggregated)}";
        var reversedElements = elements.ToList();
        reversedElements.Reverse();
        var element = new RootElement(newAggregated, elements, reversedElements, text);
        return element;
    }
}