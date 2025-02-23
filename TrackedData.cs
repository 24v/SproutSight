using System.Collections.Generic;
using StardewModdingAPI;

namespace SproutSight;
internal partial class TrackedData
{
    // List of items tracked per date
    public Dictionary<StardewDate, List<TrackedItemStack>> ShippedData { get; set; } = new();

    public Dictionary<StardewDate, (int, int)> GoldInOut { get; set; } = new();

    internal void PrintTrackedData()
    {
        Logging.Monitor.Log("=== Shipped data ===");
        foreach (var kv in ShippedData)
        {
            foreach (var item in kv.Value)
            {
                Logging.Monitor.Log($"    {kv.Key.ToString()} - {item.ToString()}", LogLevel.Info);
            }
        }

        Logging.Monitor.Log("=== Gold In/Out ===");
        foreach (var kv in GoldInOut)
        {
            Logging.Monitor.Log($"    {kv.Key.ToString()} - GoldIn: {kv.Value.Item1} - GoldOut: {kv.Value.Item2}", LogLevel.Info);
        }
    }
}
