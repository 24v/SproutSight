using System.Text;

namespace SproutSight;
internal class TrackingDataSerializer
{
    private const String SHIPPING_HEADER = "PlayerName,FarmName,SaveDate,Year,SeasonId,SeasonName,Day,ItemId,ItemName,QualityId,QualityName,Count,Price,CategoryId,CategoryName";
    private const String GOLD_HEADER = "PlayerName,FarmName,SaveDate,Year,SeasonId,SeasonName,Day,GoldIn,GoldOut,GoldInWallet";

    private static string GetStatsFilePath(string statsFolderPath, string playerName, ulong saveId)
    {
        return Path.Combine(statsFolderPath, $"{playerName}_{saveId}.csv");
    }

    private static string GetBackupFilePath(string statsFilePath)
    {
        return statsFilePath + ".bak";
    }

    private static string GetGoldFilePath(string statsFolderPath, string playerName, ulong saveId)
    {
        return Path.Combine(statsFolderPath, $"{playerName}_{saveId}_gold.csv");
    }

    private static string GetBackupGoldFile(string statsFilePath)
    {
        return statsFilePath + ".bak";
    }

    public TrackedData LoadTrackedData(string statsFolderPath, string playerName, ulong saveId)
    {
        var data = new TrackedData
        {
            ShippedData = ReadShippedItems(statsFolderPath, playerName, saveId),
            GoldInOut = ReadGoldData(statsFolderPath, playerName, saveId)
        };

        // Log summary
        int totalItems = data.ShippedData.Values.Sum(list => list.Count);
        Logging.Monitor.Log($"Loaded {totalItems} shipped items across {data.ShippedData.Count} days", LogLevel.Trace);
        int totalGoldEntries = data.GoldInOut.Keys.Count;
        Logging.Monitor.Log($"Loaded {totalGoldEntries} gold entries", LogLevel.Trace);

        return data;
    }

    private Dictionary<StardewDate, List<TrackedItemStack>> ReadShippedItems(
        string statsFolderPath,
        string playerName,
        ulong saveId)
    {
        string statsFilePath = GetStatsFilePath(statsFolderPath, playerName, saveId);
        var result = new Dictionary<StardewDate, List<TrackedItemStack>>();
        Logging.Monitor.Log($"Reading stats from {statsFilePath}", LogLevel.Trace);

        if (!File.Exists(statsFilePath))
        {
            Logging.Monitor.Log("Stats file not found", LogLevel.Trace);
            return result;
        }

        try
        {
            string[] lines = File.ReadAllLines(statsFilePath);
            if (lines.Length == 0)
            {
                Logging.Monitor.Log("Stats file empty", LogLevel.Trace);
                return result;
            }

            // Validate header
            var headerFields = lines[0].Split(',');
            if (!headerFields.SequenceEqual(SHIPPING_HEADER.Split(',')))
            {
                Logging.Monitor.Log("CSV file has an invalid header format", LogLevel.Error);
                return result;
            }
            
            Logging.Monitor.Log($"Found {lines.Length} lines in save file", LogLevel.Trace);

            // Process data rows
            for (int lineNum = 1; lineNum < lines.Length; lineNum++)
            {
                try
                {
                    var fields = ParseCsvLine(lines[lineNum]);
                    if (fields.Length != headerFields.Length)
                {
                        Logging.Monitor.Log($"Line {lineNum + 1}: Invalid number of columns, skipping", LogLevel.Warn);
                    continue;
                }

                    // Parse date fields (Year, SeasonId, Day)
                    if (!int.TryParse(fields[3], out int year) ||
                        !int.TryParse(fields[4], out int seasonId) ||
                        !int.TryParse(fields[6], out int day))
                    {
                        Logging.Monitor.Log($"Line {lineNum + 1}: Invalid date values, skipping", LogLevel.Warn);
                        continue;
                    }

                    // Validate season
                    if (!Enum.IsDefined(typeof(StardewValley.Season), seasonId))
                    {
                        Logging.Monitor.Log($"Line {lineNum + 1}: Invalid season ID: {seasonId}, skipping", LogLevel.Warn);
                        continue;
                    }

                    var date = new StardewDate(year, (StardewValley.Season)seasonId, day);

                    // Parse item fields
                    if (!int.TryParse(fields[11], out int stackCount) ||
                        !int.TryParse(fields[12], out int salePrice) ||
                        !int.TryParse(fields[13], out int category))
                    {
                        Logging.Monitor.Log($"Line {lineNum + 1}: Invalid numeric values, skipping", LogLevel.Warn);
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
                    Logging.Monitor.Log($"Added item: {stack}", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    Logging.Monitor.Log($"Line {lineNum + 1}: Failed to parse line: {ex.Message}, skipping", LogLevel.Warn);
                }
            }
        }
        catch (Exception ex)
        {
            Logging.Monitor.Log($"Failed to read shipped items: {ex.Message}", LogLevel.Error);
            throw;
        }

        return result;
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
                Logging.Monitor.Log($"Created backup at {backupPath}", LogLevel.Trace);
            }

            var csv = new StringBuilder();
            if (!File.Exists(statsFilePath))
            {
                csv.AppendLine(SHIPPING_HEADER);
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
            Logging.Monitor.Log($"Saved {items.Count} items to {statsFilePath}", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            Logging.Monitor.Log($"Error saving items: {ex.Message}", LogLevel.Error);
            throw;
        }
    }

    public void SaveGoldData(
        string statsFolderPath,
        string playerName,
        string farmName,
        ulong saveId,
        StardewDate date,
        int goldIn,
        int goldOut,
        int goldInWallet)
    {
        try
        {
            string goldFilePath = GetGoldFilePath(statsFolderPath, playerName, saveId);

            // Create backup if file exists
            if (File.Exists(goldFilePath))
            {
                string backupPath = GetBackupGoldFile(goldFilePath);
                File.Copy(goldFilePath, backupPath, true);
                Logging.Monitor.Log($"Created gold data backup at {backupPath}", LogLevel.Trace);
            }

            var csv = new StringBuilder();
            if (!File.Exists(goldFilePath))
            {
                csv.AppendLine(GOLD_HEADER);
            }

            // Format: PlayerName,FarmName,SaveDate,Year,SeasonId,SeasonName,Day,GoldIn,GoldOut,GoldInWallet
            string line = string.Join(",",
                playerName,
                farmName,
                DateTime.Now.ToString("yyyy-MM-dd"),
                date.Year,
                (int)date.Season,
                date.Season.ToString(),
                date.Day,
                goldIn,
                goldOut,
                goldInWallet
            );

            csv.AppendLine(line);
            File.AppendAllText(goldFilePath, csv.ToString());
            Logging.Monitor.Log($"Saved gold data to {goldFilePath}", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            Logging.Monitor.Log($"Error saving gold data: {ex.Message}", LogLevel.Error);
            throw;
        }
    }

    public Dictionary<StardewDate, GoldInOut> ReadGoldData(string statsFolderPath, string playerName, ulong saveId)
    {
        string goldFilePath = GetGoldFilePath(statsFolderPath, playerName, saveId);
        var result = new Dictionary<StardewDate, GoldInOut>();
        
        Logging.Monitor.Log($"Reading gold data from {goldFilePath}", LogLevel.Trace);

        if (!File.Exists(goldFilePath))
        {
            Logging.Monitor.Log("Gold data file not found", LogLevel.Trace);
            return result;
        }

        try
        {
            string[] lines = File.ReadAllLines(goldFilePath);
            if (lines.Length == 0)
            {
                Logging.Monitor.Log("Gold data file empty", LogLevel.Trace);
                return result;
            }

            // Validate header
            var headerFields = lines[0].Split(',');
            if (!headerFields.SequenceEqual(GOLD_HEADER.Split(',')))
            {
                Logging.Monitor.Log("Gold CSV file has an invalid header format", LogLevel.Error);
                return result;
            }

            Logging.Monitor.Log($"Found {lines.Length} lines in gold data file", LogLevel.Trace);

            // Process data rows
            for (int lineNum = 1; lineNum < lines.Length; lineNum++)
            {
                try
                {
                    var fields = ParseCsvLine(lines[lineNum]);
                    if (fields.Length != headerFields.Length)
                    {
                        Logging.Monitor.Log($"Line {lineNum + 1}: Invalid number of columns, skipping", LogLevel.Warn);
                        continue;
                    }

                    // Parse date fields
                    if (!int.TryParse(fields[3], out int year) ||
                        !int.TryParse(fields[4], out int seasonId) ||
                        !int.TryParse(fields[6], out int day))
                    {
                        Logging.Monitor.Log($"Line {lineNum + 1}: Invalid date values, skipping", LogLevel.Warn);
                        continue;
                    }

                    // Validate season
                    if (!Enum.IsDefined(typeof(StardewValley.Season), seasonId))
                    {
                        Logging.Monitor.Log($"Line {lineNum + 1}: Invalid season ID: {seasonId}, skipping", LogLevel.Warn);
                        continue;
                    }

                    // Parse gold values
                    if (!int.TryParse(fields[7], out int goldIn) ||
                        !int.TryParse(fields[8], out int goldOut) ||
                        !int.TryParse(fields[9], out int goldInWallet))
                    {
                        Logging.Monitor.Log($"Line {lineNum + 1}: Invalid gold values, skipping", LogLevel.Warn);
                        continue;
                    }

                    var date = new StardewDate(year, (Season)seasonId, day);
                    result[date] = new GoldInOut(goldIn, goldOut, goldInWallet);
                    Logging.Monitor.Log($"Added gold data: {date} In:{goldIn} Out:{goldOut} Wallet:{goldInWallet}", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    Logging.Monitor.Log($"Line {lineNum + 1}: Failed to parse line: {ex.Message}, skipping", LogLevel.Warn);
                }
            }
        }
        catch (Exception ex)
        {
            Logging.Monitor.Log($"Failed to read gold data: {ex.Message}", LogLevel.Error);
            throw;
        }

        return result;
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
}