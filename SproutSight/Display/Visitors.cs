namespace SproutSight.Display;
internal static class Visitors
{
    public static SingleValueVisitor CreateShippedVisitor(Dictionary<StardewDate, List<TrackedItemStack>> shippedData, Operation operation, 
                                                  int highestDay, int highestSeason, int highestYear)
    {
        return new SingleValueVisitor(operation, StardewDate.GetTodaysDate(), highestDay, highestSeason, highestYear, date => 
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
        return new SingleValueVisitor(operation, StardewDate.GetTodaysDate(), highestDay, highestSeason, highestYear, 
                              date => goldInOut.GetValueOrDefault(date)?.GoldInWallet ?? 0);
    }

    public static CashFlowVisitor CreateCashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation,
                                                      int highestDayIn, int highestSeasonIn, int highestYearIn,
                                                      int highestDayOut, int highestSeasonOut, int highestYearOut)
    {
        return new CashFlowVisitor(goldInOut, operation, StardewDate.GetTodaysDate(), highestDayIn, highestSeasonIn, 
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
        }, StardewDate.GetTodaysDate());
    }

    public static SingleValueFirstPassVisitor CreateWalletGoldVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation)
    {
        return new SingleValueFirstPassVisitor(operation, date => goldInOut.GetValueOrDefault(date)?.GoldInWallet ?? 0, StardewDate.GetTodaysDate());
    }

    public static CashFlowFirstPassVisitor CreateCashFlowVisitor(Dictionary<StardewDate, GoldInOut> goldInOut, Operation operation)
    {
        return new CashFlowFirstPassVisitor(goldInOut, operation, StardewDate.GetTodaysDate());
    }
}