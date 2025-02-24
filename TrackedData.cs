using System.Collections.Generic;
using StardewModdingAPI;

namespace SproutSight;
internal partial class TrackedData
{
    // List of items tracked per date
    public Dictionary<StardewDate, List<TrackedItemStack>> ShippedData { get; set; } = [];

    public Dictionary<StardewDate, CashFlowInOut> GoldInOut { get; set; } = [];

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
            Logging.Monitor.Log($"    {kv.Key.ToString()} - GoldIn: {kv.Value.In} - GoldOut: {kv.Value.Out}", LogLevel.Info);
        }
    }
}

internal record CashFlowInOut(int In, int Out);
