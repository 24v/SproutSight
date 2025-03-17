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

        var aggregationValuesForDays = elements
            .Select(e => e.Value)
            .Where(e => e.IsValid)
            .Select(e => e.Value)
            .ToList();
        
        var newAggregated = DoOperation(aggregationValuesForDays, aggregationValuesForDays.Count);
        var aggValue = new AggValue(newAggregated, aggregationValuesForDays.Count > 0, aggregationValuesForDays.Count);
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

        int rowHeight = DisplayHelper.CalculateRowHeight(newAggregated, HighestOverallSeasonTotal);
        string layout = DisplayHelper.FormatLayout(rowHeight);
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
        var totalNumberOfDaysCoveredForAllSeasonInYear = elements
            .Select(e => e.Value)
            .Where(e => e.IsValid)
            .Select(e => e.TotalNumberOfDaysCovered)
            .Sum();

        int newAggregated;
        AggValue aggValue;
        if (operation == Operation.Average)
        {
            // Average of averages magic
            var aggregationValuesforSeasonsNormalized = elements
                .Select(e => e.Value)
                .Where(e => e.IsValid)
                .Select(e => e.Value * e.TotalNumberOfDaysCovered)
                .ToList();

            newAggregated = DoOperation(aggregationValuesforSeasonsNormalized, totalNumberOfDaysCoveredForAllSeasonInYear);
            aggValue = new AggValue(newAggregated, aggregationValuesforSeasonsNormalized.Count > 0, totalNumberOfDaysCoveredForAllSeasonInYear);
        }
        else
        {
            var aggregationValuesForSeasons = elements
                .Select(e => e.Value)
                .Where(e => e.IsValid)
                .Select(e => e.Value)
                .ToList();
            newAggregated = DoOperation(aggregationValuesForSeasons);
            aggValue = new AggValue(newAggregated, aggregationValuesForSeasons.Count > 0, totalNumberOfDaysCoveredForAllSeasonInYear);
        }

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

        int rowHeight = DisplayHelper.CalculateRowHeight(newAggregated, HighestOverallYearTotal);
        string layout = DisplayHelper.FormatLayout(rowHeight);
        var reversedElements = elements.ToList();
        reversedElements.Reverse();
        var yearElement = new YearElement(year.Year, aggValue, elements, 
                reversedElements, year.Year + "", layout, tooltip, tint);
        return yearElement;
    }
    
    public virtual RootElement Visit(RootNode root)
    {
        var elements = root.Years.Select(Visit).ToList();
        var totalNumberofDaysCoveredForAllYears = elements
            .Select(e => e.Value)
            .Where(e => e.IsValid)
            .Select(e => e.TotalNumberOfDaysCovered)
            .Sum();

        int aggregated;
        AggValue aggValue;
        if (operation == Operation.Average)
        {
            var aggregationValuesForYearsNormalized = elements
                .Select(e => e.Value)
                .Where(e => e.IsValid)
                .Select(e => e.Value * e.TotalNumberOfDaysCovered)
                .ToList();
            aggregated = DoOperation(aggregationValuesForYearsNormalized, totalNumberofDaysCoveredForAllYears);
            aggValue = new AggValue(aggregated, aggregationValuesForYearsNormalized.Count > 0, totalNumberofDaysCoveredForAllYears);
        }
        else
        {
            var aggregationValuesForYears = elements
                .Select(e => e.Value)
                .Where(e => e.IsValid)
                .Select(e => e.Value)
                .ToList();
            aggregated = DoOperation(aggregationValuesForYears);
            aggValue = new AggValue(aggregated, aggregationValuesForYears.Count > 0, totalNumberofDaysCoveredForAllYears);
        }

        string text = $"Overall {operation}: {DisplayHelper.FormatGoldNumber(aggregated)}";
        var reversedElements = elements.ToList();
        reversedElements.Reverse();
        var element = new RootElement(aggregated, elements, reversedElements, text);
        return element;
    }
}