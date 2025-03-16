using StardewValley;

namespace SproutSight;

// I stole this from UIInfoSuite2. I hope it does what I want.
public static class UIElementUtils
{
  public static bool IsRenderingNormally()
  {
    bool[] conditions =
    {
      !Game1.game1.takingMapScreenshot,
      !Game1.eventUp,
      !Game1.viewportFreeze,
      !Game1.freezeControls,
      Game1.viewportHold <= 0,
      Game1.displayHUD
    };

    return conditions.All(condition => condition);
  }
}
