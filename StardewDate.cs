namespace SproutSight;
internal class StardewDate
{
    public int Year { get; set; }
    public Season Season { get; set; }
    public int Day { get; set; }

    public StardewDate(int year, Season season, int day)
    {
        Year = year;
        Season = season;
        Day = day;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as StardewDate);
    }

    public bool Equals(StardewDate? other)
    {
        if (other is null) return false;
        return Year == other.Year && Season == other.Season && Day == other.Day;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Year, Season, Day);
    }

    public static StardewDate GetStardewDate()
    {
        return new StardewDate(Game1.year, Game1.season, Game1.dayOfMonth);
    }

    public override string ToString()
    {
        return $"{Year}-{Season}-{Day}";
    }

}

internal record StardewYear(int Year);
internal record StardewYearSeason(int Year, Season Season);