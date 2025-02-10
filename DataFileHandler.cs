using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewModdingAPI;
using xTile;

namespace SproutSight;
internal class DataFileHandler
{
    // Col Format
    private const String HEADER = "PlayerName,FarmName,SaveDate,Year,SeasonId,SeasonName,Day,ItemId,ItemName,QualityId,QualityName,Count,Price,CategoryId,CategoryName";
    private readonly IMonitor _monitor;

    public DataFileHandler(IMonitor monitor)
    {
        _monitor = monitor;
    }

    private string GetStatsFilePath(string statsFolderPath, string playerName, ulong saveId)
    {
        return Path.Combine(statsFolderPath, $"{playerName}_{saveId}.csv");
    }

    private string GetBackupFilePath(string statsFilePath)
    {
        return statsFilePath + ".bak";
    }

    public void SaveShippedItems(
        string statsFolderPath,
        string playerName,
        string farmName,
        List<TrackedItemStack> items,
        StardewDate date,
        ulong saveId)
    {
        string statsFilePath = GetStatsFilePath(statsFolderPath, playerName, saveId);

        try
        {
            // Create backup of existing file
            if (File.Exists(statsFilePath))
            {
                string backupPath = GetBackupFilePath(statsFilePath);
                File.Copy(statsFilePath, backupPath, true);
                _monitor.Log($"Created backup at {backupPath}", LogLevel.Trace);
            }

            var csv = new StringBuilder();
            if (!File.Exists(statsFilePath))
            {
                csv.AppendLine(HEADER);
            }

            foreach (var item in items)
            {
                // Order must match HEADER
                string[] fields = new[]
                {
                    playerName,                    // PlayerName
                    farmName,                      // FarmName
                    DateTime.Now.ToString("yyyyMMddHHmmss"), // SaveDate
                    date.Year.ToString(),          // Year
                    ((int)date.Season).ToString(), // SeasonId
                    date.Season.ToString(),        // SeasonName
                    date.Day.ToString(),           // Day
                    item.Id,                       // ItemId
                    item.Name,                     // ItemName
                    item.Quality.ToString(),       // QualityId
                    item.QualityName,              // QualityName
                    item.StackCount.ToString(),    // Count
                    item.SalePrice.ToString(),     // Price
                    item.Category.ToString(),      // CategoryId
                    item.CategoryName              // CategoryName
                };

                csv.AppendLine(string.Join(",", fields.Select(EscapeCsvField)));
            }

            File.AppendAllText(statsFilePath, csv.ToString());
            _monitor.Log($"Saved {items.Count} items to {statsFilePath}", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            _monitor.Log($"Error saving items: {ex.Message}", LogLevel.Error);
            throw;
        }
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private Dictionary<StardewDate, List<TrackedItemStack>> ReadShippedItems(
        string statsFolderPath,
        string playerName,
        ulong saveId)
    {
        string statsFilePath = GetStatsFilePath(statsFolderPath, playerName, saveId);
        var result = new Dictionary<StardewDate, List<TrackedItemStack>>();
        _monitor.Log($"Reading stats from {statsFilePath}", LogLevel.Trace);

        if (!File.Exists(statsFilePath))
        {
            _monitor.Log("Stats file not found", LogLevel.Trace);
            return result;
        }

        try
        {
            string[] lines = File.ReadAllLines(statsFilePath);
            if (lines.Length == 0)
            {
                _monitor.Log("Stats file empty", LogLevel.Trace);
                return result;
            }

            // Validate header
            var headerFields = lines[0].Split(',');
            if (!headerFields.SequenceEqual(HEADER.Split(',')))
            {
                _monitor.Log("CSV file has an invalid header format", LogLevel.Error);
                return result;
            }
            
            _monitor.Log($"Found {lines.Length} lines in save file", LogLevel.Trace);

            // Process data rows
            for (int lineNum = 1; lineNum < lines.Length; lineNum++)
            {
                try
                {
                    var fields = ParseCsvLine(lines[lineNum]);
                    if (fields.Length != headerFields.Length)
                {
                        _monitor.Log($"Line {lineNum + 1}: Invalid number of columns, skipping", LogLevel.Warn);
                    continue;
                }

                    // Parse date fields (Year, SeasonId, Day)
                    if (!int.TryParse(fields[3], out int year) ||
                        !int.TryParse(fields[4], out int seasonId) ||
                        !int.TryParse(fields[6], out int day))
                    {
                        _monitor.Log($"Line {lineNum + 1}: Invalid date values, skipping", LogLevel.Warn);
                        continue;
                    }

                    // Validate season
                    if (!Enum.IsDefined(typeof(StardewValley.Season), seasonId))
                    {
                        _monitor.Log($"Line {lineNum + 1}: Invalid season ID: {seasonId}, skipping", LogLevel.Warn);
                        continue;
                    }

                    var date = new StardewDate(year, (StardewValley.Season)seasonId, day);

                    // Parse item fields
                    if (!int.TryParse(fields[11], out int stackCount) ||
                        !int.TryParse(fields[12], out int salePrice) ||
                        !int.TryParse(fields[13], out int category))
                    {
                        _monitor.Log($"Line {lineNum + 1}: Invalid numeric values, skipping", LogLevel.Warn);
                        continue;
                    }

                    var stack = new TrackedItemStack
                    {
                        Id = fields[7],
                        Quality = int.TryParse(fields[9], out int quality) ? quality : 0,
                        StackCount = stackCount,
                        SalePrice = salePrice,
                        Category = category
                    };

                    if (!result.TryGetValue(date, out var items))
                    {
                        items = new List<TrackedItemStack>();
                        result[date] = items;
                    }
                    items.Add(stack);
                    _monitor.Log($"Added item: {stack}", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    _monitor.Log($"Line {lineNum + 1}: Failed to parse line: {ex.Message}, skipping", LogLevel.Warn);
                }
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"Failed to read shipped items: {ex.Message}", LogLevel.Error);
            throw;
        }

        return result;
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Double quotes inside quotes = escaped quote
                    currentField.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        fields.Add(currentField.ToString());
        return fields.ToArray();
    }

    public TrackedData LoadTrackedData(string statsFolderPath, string playerName, ulong saveId)
    {
        var data = new TrackedData() {
            Monitor = _monitor
        };

        // Load tracked data
        data.AllData = ReadShippedItems(statsFolderPath, playerName, saveId);

        // Log summary
        int totalItems = data.AllData.Values.Sum(list => list.Count);
        _monitor.Log($"Loaded {totalItems} items across {data.AllData.Count} days", LogLevel.Trace);

        return data;
    }
}

internal partial class TrackedData
{
    // List of items tracked per date
    public Dictionary<StardewDate, List<TrackedItemStack>> AllData { get; set; } = new();
}

enum ItemQuality { Normal, Silver, Gold, Iridium }

internal class StardewDate
{
    public int Year { get; set; }
    public StardewValley.Season Season { get; set; }
    public int Day { get; set; }

    public StardewDate(int year, StardewValley.Season season, int day)
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

internal partial class TrackedItemStack : IComparable<TrackedItemStack>
{
    public string Id { get; set; } = "";
    public int StackCount { get; set; }
    public int SalePrice { get; set; }
    public int TotalSalePrice => SalePrice * StackCount;
    public int Quality { get; set; }
    public string QualityName => ((ItemQuality)Quality).ToString();

    public int Category { get; set; }
    public string CategoryName => GetCategoryName(Category);

    private ParsedItemData? _parsedItemData;
    public ParsedItemData ParsedItemData => _parsedItemData ??= ItemRegistry.GetDataOrErrorItem(Id);

    private string? _name;
    public string Name => _name ??= ParsedItemData.DisplayName;

    public ParsedItemData Sprite => ParsedItemData;

    public TrackedItemStack()
    {
    }

    public TrackedItemStack(Item item)
    {
        this.Id = item.ItemId;
        this.StackCount = item.stack.Value;
        this.SalePrice = item.sellToStorePrice();
        this.Quality = item.Quality;
        this.Category = item.Category;
    }

    public int CompareTo(TrackedItemStack? other)
    {
        if (other == null) return 1;
        // Sort by TotalSalePrice in descending order (highest first)
        return other.TotalSalePrice.CompareTo(this.TotalSalePrice);
    }

    public static string GetCategoryName(int category)
    {
        return category switch
        {
            -2 => "Mineral",           // Gem Category (affected by Gemologist)
            -4 => "Fish",              // Fish Category (affected by Fisher/Angler)
            -5 => "Animal Product",     // Egg Category (affected by Rancher)
            -6 => "Animal Product",     // Milk Category (affected by Rancher)
            -7 => "Cooking",           // Cooking Category
            -8 => "Crafting",          // Crafting Category
            -9 => "Big Craftable",     // Big Craftable Category
            -12 => "Mineral",          // Minerals Category (affected by Gemologist)
            -14 => "Animal Product",    // Meat Category
            -15 => "Resource",         // Metal Resources
            -16 => "Resource",         // Building Resources
            -17 => "Pierre's Store",   // Sell at Pierre's
            -18 => "Animal Product",    // Sell at Pierre's and Marnie's (affected by Rancher)
            -19 => "Fertilizer",       // Fertilizer Category
            -20 => "Trash",            // Junk Category
            -21 => "Bait",            // Bait Category
            -22 => "Fishing Tackle",   // Tackle Category
            -23 => "Fish Shop",        // Sell at Fish Shop
            -24 => "Decor",           // Furniture Category
            -25 => "Cooking",         // Ingredients Category
            -26 => "Artisan Goods",    // Artisan Goods Category (affected by Artisan)
            -27 => "Artisan Goods",    // Syrup Category (affected by Tapper)
            -28 => "Monster Loot",     // Monster Loot Category
            -29 => "Equipment",        // Equipment Category
            -74 => "Seed",            // Seeds Category
            -75 => "Vegetable",        // Vegetable Category (affected by Tiller)
            -79 => "Fruit",           // Fruits Category (affected by Tiller if not foraged)
            -80 => "Flower",          // Flowers Category (affected by Tiller)
            -81 => "Forage",          // Greens Category
            -95 => "Hat",             // Hat Category
            -96 => "Ring",            // Ring Category
            -97 => "Boots",           // Boots Category
            -98 => "Weapon",          // Weapon Category
            -99 => "Tool",            // Tool Category
            -100 => "Clothing",        // Clothing Category
            -101 => "Trinket",         // Trinket Category
            -102 => "Book",            // Books Category
            -103 => "Skill Book",      // Skill Books Category
            -999 => "Litter",          // Litter Category
            0 => "Uncategorized",
            _ => $"Unknown ({category})"
        };
    }

    public override string ToString()
    {
        return $"{Name} (Id: {Id}, Stack: {StackCount}, Price: {SalePrice}g, Quality: {QualityName}, Category: {CategoryName})";
    }
}