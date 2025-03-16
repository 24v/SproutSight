using Microsoft.Xna.Framework.Graphics;

namespace SproutSight;

internal static class StarControlIntegration
{
    public static void Register(IStarControlApi starControl, IManifest mod, Action toggleDisplay)
    {
        Texture2D texture = Game1.content.Load<Texture2D>("Buildings/Shipping Bin");
        Rectangle sourceRectangle = new(0, 0, 32, 32);
        starControl.RegisterItems(
            mod,
            [
                new RadialMenuItem(
                    $"{mod.UniqueID}.ToggleDisplay",
                    toggleDisplay,
                    texture,
                    sourceRectangle
                ),
            ]
        );
    }

    private class RadialMenuItem(string id, Action activate, Texture2D texture, Rectangle sourceRectangle) : IRadialMenuItem
    {
        public string Id => id;

        public string Title => "SproutSight";

        public string Description => "Toggle SproutSight Pro";

        public Texture2D Texture => texture;

        public Rectangle SourceRectangle => sourceRectangle;

        public ItemActivationResult Activate(
            Farmer who,
            DelayedActions delayedActions,
            ItemActivationType activationType = ItemActivationType.Primary
        )
        {
            if (delayedActions != DelayedActions.None)
            {
                return ItemActivationResult.Delayed;
            }
            activate();
            return ItemActivationResult.Custom;
        }
    }
}
