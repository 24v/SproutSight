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

    public static StardewDate GetTodaysDate()
    {
        return new StardewDate(Game1.year, Game1.season, Game1.dayOfMonth);
    }

    public override string ToString()
    {
        return $"{Year}-{Season}-{Day}";
    }

    /// <summary>
    /// Checks if this date is before another date
    /// </summary>
    /// <param name="other">The date to compare with</param>
    /// <returns>True if this date is before the other date</returns>
    public bool IsBefore(StardewDate other)
    {
        if (Year < other.Year) return true;
        if (Year > other.Year) return false;
        
        // Same year, check season
        if ((int)Season < (int)other.Season) return true;
        if ((int)Season > (int)other.Season) return false;
        
        // Same year and season, check day
        return Day < other.Day;
    }

    /// <summary>
    /// Checks if this date is after another date
    /// </summary>
    /// <param name="other">The date to compare with</param>
    /// <returns>True if this date is after the other date</returns>
    public bool IsAfter(StardewDate other)
    {
        if (Year > other.Year) return true;
        if (Year < other.Year) return false;
        
        // Same year, check season
        if ((int)Season > (int)other.Season) return true;
        if ((int)Season < (int)other.Season) return false;
        
        // Same year and season, check day
        return Day > other.Day;
    }

    /// <summary>
    /// Checks if this date is on or before another date
    /// </summary>
    /// <param name="other">The date to compare with</param>
    /// <returns>True if this date is on or before the other date</returns>
    public bool IsOnOrBefore(StardewDate other)
    {
        return IsBefore(other) || Equals(other);
    }

    /// <summary>
    /// Checks if this date is on or after another date
    /// </summary>
    /// <param name="other">The date to compare with</param>
    /// <returns>True if this date is on or after the other date</returns>
    public bool IsOnOrAfter(StardewDate other)
    {
        return IsAfter(other) || Equals(other);
    }
}

internal record StardewYear(int Year);
internal record StardewYearSeason(int Year, Season Season);