
namespace SproutSight;

public static class Logging 
{
    private static IMonitor? _monitor;

    public static IMonitor Monitor
    {
        get { return _monitor!; }
        set { _monitor = value; }
    }

}
