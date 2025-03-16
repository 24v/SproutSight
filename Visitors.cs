namespace SproutSight;
internal static class Visitors
{
    public static SingleValueVisitor CreateShippedVisitor(Dictionary<StardewDate, List<TrackedItemStack>> shippedData, Operation operation, 
                                                  int highestDay, int highestSeason, int highestYear)
    {
        return new SingleValueVisitor(operation, StardewDate.GetStardewDate(), highestDay, highestSeason, highestYear, date => 
        {
            if (shippedData.TryGetValue(date, out var items))
            {
                return items.Select(item => item.TotalSalePrice).Sum();
            }
            return 0;
        });
    }

    public static SingleValueVisitor CreateWalletGoldVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation, 
                                                    int highestDay, int highestSeason, int highestYear)
    {
        return new SingleValueVisitor(operation, StardewDate.GetStardewDate(), highestDay, highestSeason, highestYear, 
                              date => goldInOut.GetValueOrDefault(date)?.GoldInWallet ?? 0);
    }

    public static CashFlowVisitor CreateCashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation,
                                                      int highestDayIn, int highestSeasonIn, int highestYearIn,
                                                      int highestDayOut, int highestSeasonOut, int highestYearOut)
    {
        return new CashFlowVisitor(goldInOut, operation, StardewDate.GetStardewDate(), highestDayIn, highestSeasonIn, 
                highestYearIn, highestDayOut, highestSeasonOut, highestYearOut);
    }
}

internal static class FirstPassVisitors
{
    public static SingleValueFirstPassVisitor CreateShippedVisitor(Dictionary<StardewDate, List<TrackedItemStack>> shippedData, Operation operation)
    {
        return new SingleValueFirstPassVisitor(operation, date => 
        {
            if (shippedData.TryGetValue(date, out var items))
            {
                return items.Select(item => item.TotalSalePrice).Sum();
            }
            return 0;
        });
    }

    public static SingleValueFirstPassVisitor CreateWalletGoldVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation)
    {
        return new SingleValueFirstPassVisitor(operation, date => goldInOut.GetValueOrDefault(date)?.GoldInWallet ?? 0);
    }

    public static CashFlowFirstPassVisitor CreateCashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation)
    {
        return new CashFlowFirstPassVisitor(goldInOut, operation);
    }
}

internal class SingleValueFirstPassVisitor(Operation operation, Func<StardewDate, int> getDayValue) 
{
    public int HighestDayValue { get; private set; } = 0;
    public int HighestSeasonValue { get; private set; } = 0;
    public int HighestYearValue { get; private set; } = 0;

    public int Visit(DayNode day)
    {
        var dayValue = getDayValue(day.Date);
        HighestDayValue = Math.Max(HighestDayValue, dayValue);
        return dayValue;
    }

    public int Visit(SeasonNode season)
    {
        int aggregateOfDays = DoOperation(season.Days.Select(Visit).ToList());
        HighestSeasonValue = Math.Max(HighestSeasonValue, aggregateOfDays);
        return aggregateOfDays;
    }

    public int Visit(YearNode year)
    {
        int aggregateOfSeasons = DoOperation(year.Seasons.Select(Visit).ToList());
        HighestYearValue = Math.Max(HighestYearValue, aggregateOfSeasons);
        return aggregateOfSeasons;
    }

    public void Visit(RootNode root)
    {
        root.Years.ForEach(e => Visit(e));
    }

    public int DoOperation(List<int> entries)
    {
        return operation switch
        {
            Operation.Min => entries.Min(),
            Operation.Max => entries.Max(),
            Operation.Sum => entries.Sum(),
            Operation.Average => (int)Math.Round(entries.Sum() / (float)entries.Count),
            Operation.End => entries.Last(),
            _ => 0
        };
    }
}

internal class CashFlowFirstPassVisitor(Dictionary<StardewDate, GoldInOut> cashFlowByDate, Operation operation)
{
    // Highest values for income
    public int HighestDayInValue { get; private set; } = 1;
    public int HighestSeasonInValue { get; private set; } = 1;
    public int HighestYearInValue { get; private set; } = 1;
    
    // Highest values for expenses
    public int HighestDayOutValue { get; private set; } = 1;
    public int HighestSeasonOutValue { get; private set; } = 1;
    public int HighestYearOutValue { get; private set; } = 1;

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

    public (int, int) Visit(DayNode day)
    {
        int dayIn = 0;
        int dayOut = 0;
        
        if (cashFlowByDate.TryGetValue(day.Date, out var goldInOut))
        {
            dayIn = goldInOut.In;
            dayOut = goldInOut.Out;
            HighestDayInValue = Math.Max(HighestDayInValue, dayIn);
            HighestDayOutValue = Math.Max(HighestDayOutValue, dayOut);
        }
        
        return (dayIn, dayOut);
    }

    public (int, int) Visit(SeasonNode season)
    {
        var dayValues = season.Days.Select(Visit).ToList();
        var inValues = dayValues.Select(v => v.Item1).ToList();
        var outValues = dayValues.Select(v => v.Item2).ToList();
        
        int aggregatedIn = DoOperation(inValues, true);
        int aggregatedOut = DoOperation(outValues, false);
        
        HighestSeasonInValue = Math.Max(HighestSeasonInValue, aggregatedIn);
        HighestSeasonOutValue = Math.Max(HighestSeasonOutValue, aggregatedOut);
        
        return (aggregatedIn, aggregatedOut);
    }

    public (int, int) Visit(YearNode year)
    {
        var seasonValues = year.Seasons.Select(Visit).ToList();
        var inValues = seasonValues.Select(v => v.Item1).ToList();
        var outValues = seasonValues.Select(v => v.Item2).ToList();
        
        int aggregatedIn = DoOperation(inValues, true);
        int aggregatedOut = DoOperation(outValues, false);
        
        HighestYearInValue = Math.Max(HighestYearInValue, aggregatedIn);
        HighestYearOutValue = Math.Max(HighestYearOutValue, aggregatedOut);
        
        return (aggregatedIn, aggregatedOut);
    }

    public void Visit(RootNode root)
    {
        root.Years.ForEach(e => Visit(e));
    }
}

internal static class DisplayHelper
{
    public const int RowWidth = 20;
    public const int MaxRowHeight = 128;
    public const int MinRowHeight = 3;
    public const int ZeroDataRowHeight = 2;
    public const int DefaultNumYearsSelected = 5;
    public const string TodayTint = "#000000";
    public const string FutureTint = "#959595";
    public const string YearTint = "#40FC05";
    public const string CashFlowOutTint = "#B22222"; 
    public const string CashFlowInTint = "#696969";
    public static string FormatGoldNumber(int number) => $"{number:N0}g";

    public static string GetTint(Season season) 
    {
        return season switch
        {
            Season.Spring => "#2CA014",
            Season.Summer => "#FEFF17",
            Season.Fall => "#D13400",
            Season.Winter => "#A9F0FF",
            _ => "White"
        };
    }
    
    public static int CalculateRowHeight(int value, int highest)
    {
        var rowHeight = ZeroDataRowHeight;
        if (value > 0)
        {
            var scale = (float)value / highest;
            rowHeight = (int)Math.Round(Math.Max(MinRowHeight, scale * MaxRowHeight));
        }
        return rowHeight;
    }
    
    public static string FormatLayout(int rowHeight)
    {
        return $"{RowWidth}px {rowHeight}px";
    }
}

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
            .Where(e => e.Valid)
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
        else if (aggValue.Valid)
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
            .Where(e => e.Valid)
            .Select(e => e.TotalNumberOfDaysCovered)
            .Sum();

        int newAggregated;
        AggValue aggValue;
        if (operation == Operation.Average)
        {
            // Average of averages magic
            var aggregationValuesforSeasonsNormalized = elements
                .Select(e => e.Value)
                .Where(e => e.Valid)
                .Select(e => e.Value * e.TotalNumberOfDaysCovered)
                .ToList();

            newAggregated = DoOperation(aggregationValuesforSeasonsNormalized, totalNumberOfDaysCoveredForAllSeasonInYear);
            aggValue = new AggValue(newAggregated, aggregationValuesforSeasonsNormalized.Count > 0, totalNumberOfDaysCoveredForAllSeasonInYear);
        }
        else
        {
            var aggregationValuesForSeasons = elements
                .Select(e => e.Value)
                .Where(e => e.Valid)
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
        else if (aggValue.Valid)
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
            .Where(e => e.Valid)
            .Select(e => e.TotalNumberOfDaysCovered)
            .Sum();

        int aggregated;
        AggValue aggValue;
        if (operation == Operation.Average)
        {
            var aggregationValuesForYearsNormalized = elements
                .Select(e => e.Value)
                .Where(e => e.Valid)
                .Select(e => e.Value * e.TotalNumberOfDaysCovered)
                .ToList();
            aggregated = DoOperation(aggregationValuesForYearsNormalized, totalNumberofDaysCoveredForAllYears);
            aggValue = new AggValue(aggregated, aggregationValuesForYearsNormalized.Count > 0, totalNumberofDaysCoveredForAllYears);
        }
        else
        {
            var aggregationValuesForYears = elements
                .Select(e => e.Value)
                .Where(e => e.Valid)
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

internal class CashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation, StardewDate upToDate,
                      int highestDayIn, int highestSeasonIn, int highestYearIn,
                      int highestDayOut, int highestSeasonOut, int highestYearOut)
{
    public Dictionary<StardewDate, GoldInOut> CashFlowByDate { get; } = goldInOut;

    // Highest values for income at each level
    public int HighestDayInValue { get; } = Math.Max(1, highestDayIn);
    public int HighestSeasonInValue { get; } = Math.Max(1, highestSeasonIn);
    public int HighestYearInValue { get; } = Math.Max(1, highestYearIn);

    // Highest values for expenses at each level
    public int HighestDayOutValue { get; } = Math.Max(1, highestDayOut);
    public int HighestSeasonOutValue { get; } = Math.Max(1, highestSeasonOut);
    public int HighestYearOutValue { get; } = Math.Max(1, highestYearOut);

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

    public (DayElement,(int,int)) VisitCashFlow(DayNode day)
    {
        int dayIn = 0;
        int dayOut = 0;
        
        if (CashFlowByDate.TryGetValue(day.Date, out var goldInOut))
        {
            dayIn = goldInOut.In;
            dayOut = goldInOut.Out;
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
                    $"Net: {DisplayHelper.FormatGoldNumber(dayIn + dayOut)}\n" +
                    $"In: {DisplayHelper.FormatGoldNumber(dayIn)}\n" +
                    $"Out: {DisplayHelper.FormatGoldNumber(dayOut)}";
            inTint = DisplayHelper.CashFlowInTint;
            outTint = DisplayHelper.CashFlowOutTint;
            int inRowHeight = DisplayHelper.CalculateRowHeight(dayIn, HighestDayInValue);
            inLayout = DisplayHelper.FormatLayout(inRowHeight);
            int outRowHeight = DisplayHelper.CalculateRowHeight(dayOut, HighestDayOutValue);
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

        return (new DayElement(day.Date, returnValue, "", inLayout, tooltip, inTint, "", outLayout, tooltip, outTint), (dayIn, dayOut));
    }

    public (SeasonElement, (int, int)) VisitCashFlow(SeasonNode season)
    {
        var elementsWithValues = season.Days.Select(VisitCashFlow).ToList();
        var elements = elementsWithValues.Select(e => e.Item1).ToList();

        var cashFlowInValues = elementsWithValues.Select(e => e.Item2.Item1).ToList();
        var cashFlowOutValues = elementsWithValues.Select(e => e.Item2.Item2).ToList();
        
        int aggregatedIn = DoOperation(cashFlowInValues, true);
        int aggregatedOut = DoOperation(cashFlowOutValues, false);
        int netValue = aggregatedIn - aggregatedOut;
        
        // Calculate in bar using season-specific highest value
        int inRowHeight = DisplayHelper.CalculateRowHeight(aggregatedIn, HighestSeasonInValue);
        string inLayout = DisplayHelper.FormatLayout(inRowHeight);
        
        // Calculate out bar using season-specific highest value
        int outRowHeight = DisplayHelper.CalculateRowHeight(aggregatedOut, HighestSeasonOutValue);
        string outLayout = DisplayHelper.FormatLayout(outRowHeight);
        
        // Determine colors based on net value
        var inTint = "#696969"; 
        var outTint = "#B22222";
        
        string tooltip;
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

        var reversedElements = elements.ToList();
        reversedElements.Reverse();
        var cashFlowSeasonEntry = new SeasonElement(
                season.Season,  new AggValue(0, false, 1), elements, reversedElements, 
                season.Season.ToString(), inLayout, tooltip, inTint, 
                season.Season == Season.Spring, season.Season == Season.Summer, season.Season == Season.Fall, season.Season == Season.Winter,
                "", outLayout, tooltip, outTint);
        return (cashFlowSeasonEntry, (aggregatedIn, aggregatedOut));
    }
    
    public (YearElement, (int, int)) VisitCashFlow(YearNode year)
    {
        var elementsWithValues = year.Seasons.Select(VisitCashFlow).ToList();
        var elements = elementsWithValues.Select(e => e.Item1).ToList();
        var reversedElements = elements.ToList();
        reversedElements.Reverse();
        var cashFlowInValues = elementsWithValues.Select(e => e.Item2.Item1).ToList();
        var cashFlowOutValues = elementsWithValues.Select(e => e.Item2.Item2).ToList();
        
        int aggregatedIn = DoOperation(cashFlowInValues, true);
        int aggregatedOut = DoOperation(cashFlowOutValues, false);
        int netValue = aggregatedIn - aggregatedOut;
        
        // Calculate in bar using year-specific highest value
        int inRowHeight = DisplayHelper.CalculateRowHeight(aggregatedIn, HighestYearInValue);
        string inLayout = DisplayHelper.FormatLayout(inRowHeight);
        
        // Calculate out bar using year-specific highest value
        int outRowHeight = DisplayHelper.CalculateRowHeight(aggregatedOut, HighestYearOutValue);
        string outLayout = DisplayHelper.FormatLayout(outRowHeight);

        // Determine colors based on net value
        var inTint = "#696969"; 
        var outTint = "#B22222";
        
        string tooltip;
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
        
        var cashFlowYearEntry = new YearElement(
                year.Year, new AggValue(1, true, 1), elements, reversedElements, 
                year.Year.ToString(), inLayout, tooltip, inTint,
                "", outLayout, tooltip, outTint);
        return (cashFlowYearEntry, (aggregatedIn, aggregatedOut));
    }
    
    public RootElement VisitCashFlow(RootNode root)
    {
        var elementsWithValues = root.Years.Select(VisitCashFlow).ToList();
        var elements = elementsWithValues.Select(e => e.Item1).ToList();
        var reversedElements = elements.ToList();
        reversedElements.Reverse();
        var cashFlowInValues = elementsWithValues.Select(e => e.Item2.Item1).ToList();
        var cashFlowOutValues = elementsWithValues.Select(e => e.Item2.Item2).ToList();
        
        int aggregatedIn = DoOperation(cashFlowInValues, true);
        int aggregatedOut = DoOperation(cashFlowOutValues, false);
        int netValue = aggregatedIn - aggregatedOut;
        
        // Calculate in/out layouts for the overall root
        int inRowHeight = DisplayHelper.CalculateRowHeight(aggregatedIn, HighestYearInValue);
        string inLayout = DisplayHelper.FormatLayout(inRowHeight);
        
        int outRowHeight = DisplayHelper.CalculateRowHeight(aggregatedOut, HighestYearOutValue);
        string outLayout = DisplayHelper.FormatLayout(outRowHeight);

        // Determine colors
        var inTint = "#696969"; 
        var outTint = "#B22222";
        
        string tooltip = $"Overall {operation}:\n" + 
                         $"Net: {DisplayHelper.FormatGoldNumber(netValue)}\n" + 
                         $"In: {DisplayHelper.FormatGoldNumber(aggregatedIn)}\n" + 
                         $"Out: {DisplayHelper.FormatGoldNumber(aggregatedOut)}";
        
        string text = $"Cash Flow {operation}";
        var element = new RootElement(
                netValue, elements, reversedElements, 
                $"Overall {operation} Cash Flow", inLayout, tooltip, inTint,
                $"", outLayout, tooltip, outTint);

        return element;
    }
}
