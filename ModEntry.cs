using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;
using System.Diagnostics;
using StardewUI.Framework;
using StardewValley.ItemTypeDefinitions;
using System.Diagnostics.CodeAnalysis;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;
using StardewValley.GameData.Shops;

/*

// Better format numbers
// Better indication of current day 

// Issues
    // TODO: Scrollable has issues
    // TODO: Those binding warnings in log (use ':')

// Adv
    // TODO: MultiPlayers multiFarms
    // TODO: Create a custom converter
    // TODO: I think i can do some nested repeats instead of what I currently have?
    // TODO: wiki

// Ideas
    - Format the gold ammounts (e.g. add columns)
    - Make a game element
    - Animate the on hover on hud icon
    - Track gold in wallet per day
    - Track expenses
        Gold in wallet per Day
        Purchases
        Sales
        Ships
        Harvests
        Expenses in vs out
    - Track item shipments 
    - UI Updates
        - Show where items are being shipped from
        - Use some season sprites
    - Full Olap Cube and SQL support
*/

namespace SproutSight;
internal sealed class ModEntry : Mod
{
    private string statsFilePath = "";
    private string viewAssetPrefix = "";

    private IViewEngine? stardewUIViewEngine;
    private IStarControlApi? starControl;
    private IGenericModConfigMenuApi? genericModConfig;
    private ModConfig Config = new();

    // In-Memory Repo of Tracked Data. This is always loaded from file OnDayStart.
    private TrackedData trackedData = new();
    // Pending items to be saved after the game saves
    private List<TrackedItemStack>? _pendingItems;
    private StardewDate? _pendingDate;
    private Dictionary<StardewDate, (int, int)> goldInOut = new();
    // The Shipping icon in the hud

    private IViewDrawable? hud;
    // Needed to respond to click / hover events
    private Rectangle hudClickableArea;

    // For tracking total gold in and out
    private int todayGoldIn = 0;
    private int todayGoldOut = 0;
    private int? lastGoldAmount = null;


    public override void Entry(IModHelper helper)
    {
        statsFilePath = helper.DirectoryPath + Path.DirectorySeparatorChar;
        viewAssetPrefix = $"Mods/{ModManifest.UniqueID}/Views";

        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.Saved += OnSaved;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Display.RenderedHud += OnRenderedHud;

        helper.ConsoleCommands.Add("sstm_current", "Shows the current items in shipping bins for all farmers.", ShowCurrentItems);
        helper.ConsoleCommands.Add("sstm_show", "Shows the tracker display.", ShowStatsMenu);
        helper.ConsoleCommands.Add("sstm_print_data", "Shows the tracked data.", ShowTrackedData);
        helper.ConsoleCommands.Add("sstm_save", "Saves the tracked data.", SaveTrackedData);
        helper.ConsoleCommands.Add("sstm_load", "Loads the tracked data from file.", LoadTrackedData);

        Logging.Monitor = Monitor;
        Monitor.Log("SproutSight => Initialized!", LogLevel.Info);
    }

    // ================================ 
    // Game Events ====================
    // ================================

    private void OnUpdateTicked(object? sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
    {
        // Day hasnt started
        if (lastGoldAmount == null) 
        {
            return;
        }
        int currentGold = Game1.player.Money;
        int goldAdded = currentGold - (int)lastGoldAmount;
        if (goldAdded > 0) 
        {
            todayGoldIn += goldAdded;
            Monitor.Log($"Player added gold: {goldAdded}. Current Out: {todayGoldOut}. Current In: {todayGoldIn}.", LogLevel.Trace);

        } else if (goldAdded < 0) 
        {
            todayGoldOut += goldAdded;
            Monitor.Log($"Player lost gold: {goldAdded}. Current Out: {todayGoldOut}. Current In: {todayGoldIn}.", LogLevel.Trace);

        }
        lastGoldAmount = currentGold;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        stardewUIViewEngine = Helper.ModRegistry.GetApi<IViewEngine>("focustense.StardewUI");
        starControl = Helper.ModRegistry.GetApi<IStarControlApi>("focustense.StarControl");
        genericModConfig = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

        if (stardewUIViewEngine == null)
        {
            Monitor.Log("Failed to load StardewUI API. Make sure the mod is installed.", LogLevel.Error);
            return;
        }
        stardewUIViewEngine.RegisterViews($"Mods/{ModManifest.UniqueID}/Views", "assets/views");
        stardewUIViewEngine.RegisterSprites($"Mods/{ModManifest.UniqueID}/Sprites", "assets/sprites");
        stardewUIViewEngine.EnableHotReloading("/Users/demo/CascadeProjects/stardew_valley/CascadeProjects/windsurf-project/stardew_mod/SproutSight");

        // Setup gmcm
        if (genericModConfig is not null)
        {
            genericModConfig.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            genericModConfig.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Show Hud Icon",
                tooltip: () => "Display a shipping to toggle the tracker view.",
                getValue: () => this.Config.EnableHudIcon,
                setValue: value => this.Config.EnableHudIcon = value
            );
            genericModConfig.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Tracker View Toggle",
                getValue: () => this.Config.ToggleKey,
                setValue: value => this.Config.ToggleKey = value
            );
        }

        // Star Control
        if (starControl is not null)
        {
            StarControlIntegration.Register(starControl, ModManifest, new Action(ShowStatsMenu));
        }

        Monitor.Log("Sprout Sight Updated => Game Launched!", LogLevel.Trace);

    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _pendingItems = null;
        _pendingDate = null;
        lastGoldAmount = Game1.player.Money;
        todayGoldOut = 0;
        todayGoldIn = 0;

        trackedData = new DataFileHandler().LoadTrackedData(
            statsFilePath,
            Game1.player.Name,
            Game1.uniqueIDForThisGame
        );

        trackedData.GoldInOut = goldInOut;
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        _pendingItems = GetCurrentShippedItems();
        _pendingDate = StardewDate.GetStardewDate();
        Monitor.Log($"Day ending with {_pendingItems.Count} items queued for save", LogLevel.Trace);
    }

    private void OnSaved(object? sender, SavedEventArgs e)
    {
        if (_pendingItems == null || _pendingDate == null)
        {
            Monitor.Log("No items queued for saving", LogLevel.Trace);
            return;
        }

        goldInOut[_pendingDate] = (todayGoldIn, todayGoldOut);

        var fileHandler = new DataFileHandler();
        fileHandler.SaveShippedItems(
            statsFilePath,
            Game1.player.Name,
            Game1.player.farmName.Value,
            _pendingItems,
            _pendingDate,
            Game1.uniqueIDForThisGame
        );

        _pendingItems = null;
        _pendingDate = null;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree)
        {
            return;
        }

        if (Config.ToggleKey.GetKeybindCurrentlyDown() != null)
        {
            ShowStatsMenu();
        } 
        else 
        {
            switch (e.Button)
            {
                case SButton.MouseLeft:
                    HandleHudClick(sender, e);
                    break;
            }
        }
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Config.EnableHudIcon) 
        {
            if (hud != null)
            {
                hud.Dispose();
                hud = null;
            }
            return;
        }

        // Avoid rendering during cutscenes.
        if (!Game1.IsHudDrawn) 
        {
            return;
        }

        hud ??= stardewUIViewEngine!.CreateDrawableFromAsset($"{viewAssetPrefix}/HudIcon");

        int marginRight = 83;
        int marginTop = 306;
        var viewport = Game1.uiViewport;
        var x = viewport.Width - marginRight;
        var y = marginTop;
        hud.Draw(e.SpriteBatch, new(x, y));

        // We need hudClickable scaled coordinates for the OnButtonPress
        var scaleFactor = Game1.options.uiScale / Game1.options.zoomLevel;
        var scaledX = (int)Math.Round(x * scaleFactor);
        var scaledY = (int)Math.Round(y * scaleFactor);
        var scaledSize = (int)Math.Round(32 * scaleFactor);
        hudClickableArea = new Rectangle(scaledX, scaledY, scaledSize, scaledSize);

        // For some reason, GetScaledScreenPixels works differently here vs ButtonPressedEventArgs
        // So... just use the non scaled hud bounds
        // Maybe because I am drawing stuff in this method instead of UpdateTicked?
        var nonScaledHudArea = new Rectangle(x, y, 32, 32);
        var mousePos = Helper.Input.GetCursorPosition().GetScaledScreenPixels();
        if (nonScaledHudArea.Contains(mousePos))
        {
            IClickableMenu.drawHoverText(e.SpriteBatch, "SproutSight Pro", Game1.smallFont, 0, 0);
        }
    }

    // ================================ 
    // CLI Commands ===================
    // ================================

    private void ShowStatsMenu(string? command = null, string[]? args = null)
    {
        ShowStatsMenu();
    }

    private void ShowCurrentItems(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Please load a save file first.", LogLevel.Error);
            return;
        }
        List<TrackedItemStack> shippedItems = GetCurrentShippedItems();
        PrintShippedItems(shippedItems);
        Monitor.Log($"Current Gold Totals: Current Out: {todayGoldOut}. Current In: {todayGoldIn}.", LogLevel.Debug);
    }

    private void ShowTrackedData(string command, string[] args)
    {
        Monitor.Log("Showing tracked data", LogLevel.Info);
        trackedData.PrintTrackedData();
    }

    private void SaveTrackedData(string command, string[] args)
    {
        SaveStats();
        Monitor.Log("Saved tracked data", LogLevel.Info);
    }

    private void LoadTrackedData(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Cannot load data - save not loaded", LogLevel.Warn);
            return;
        }
        trackedData = new DataFileHandler().LoadTrackedData(
            statsFilePath,
            Game1.player.Name,
            Game1.uniqueIDForThisGame
        );
        Monitor.Log("Tracked data loaded successfully.", LogLevel.Info);
    }

    private void HandleHudClick(object? sender, ButtonPressedEventArgs e)
    {

        // In case I have to debug this stuff again
        // Monitor.Log("---Handle Hud Click---", LogLevel.Info);
        // Monitor.Log($"Margin Right: {marginRight}", LogLevel.Info);
        // Monitor.Log($"Margin Top: {marginTop}", LogLevel.Info);
        // Monitor.Log($"Draw Coords {x},{y}", LogLevel.Info);
        // Monitor.Log($"Hud Rect: {hudClickableArea}", LogLevel.Info);
        // Monitor.Log($"UI Viewport: {Game1.uiViewport}", LogLevel.Info);
        // Monitor.Log($"Viewport: {Game1.viewport}", LogLevel.Info);
        // Monitor.Log($"UI Scale: {Game1.options.uiScale}", LogLevel.Info);
        // Monitor.Log($"Base UI Scale: {Game1.options.baseUIScale}", LogLevel.Info);
        // Monitor.Log($"Zoom: {Game1.options.zoomLevel}", LogLevel.Info);
        // Monitor.Log($"Base Zoom: {Game1.options.baseZoomLevel}", LogLevel.Info);
        // Monitor.Log($"Click ScreenPixels: {e.Cursor.ScreenPixels.ToPoint()}", LogLevel.Info);
        // Monitor.Log($"Click GetScaledScreenPixels: {e.Cursor.GetScaledScreenPixels().ToPoint()}", LogLevel.Info);
        // Monitor.Log($"Click AbsolutePixels: {e.Cursor.AbsolutePixels.ToPoint()}", LogLevel.Info);
        // Monitor.Log($"Click GetScaledAbsolutePixels: {e.Cursor.GetScaledAbsolutePixels().ToPoint()}", LogLevel.Info);
        // Monitor.Log($"Scale Factor: {scaleFactor}", LogLevel.Info);
        // Monitor.Log($"Scaled Rectangle: {scaledRectangle}", LogLevel.Info);
        // Monitor.Log($"Click Position: {clickPosition.ToPoint()}", LogLevel.Info);

        var clickPosition = e.Cursor.GetScaledScreenPixels().ToPoint();
        if (hudClickableArea.Contains(clickPosition))
        {
            ShowStatsMenu();
        }
    }

    private List<TrackedItemStack> GetCurrentShippedItems()
    {
        // Since we are iterating over everything, I want to make sure its not too slow.
        Stopwatch stopwatch = Stopwatch.StartNew();
        var currentShippedItems = new List<TrackedItemStack>();

        // Main shipping bin
        currentShippedItems.AddRange(Game1
            .getFarm()
            .getShippingBin(Game1.player)
            .Select(item => new TrackedItemStack(item)));
        Monitor.Log($"Found main shipping bin with {currentShippedItems.Count} items", LogLevel.Trace);

        // Check for MiniShippingBins all locations
        Monitor.Log($"Checking {Game1.locations.Count} locations for mini-bins", LogLevel.Trace);
        foreach (GameLocation location in Game1.locations)
        {
            foreach (StardewValley.Object obj in location.Objects.Values)
            {
                if (obj is Chest chest && chest.SpecialChestType == Chest.SpecialChestTypes.MiniShippingBin)
                {
                    Monitor.Log($"Found mini-shipping bin at {location.Name} with {chest.Items.Count} items", LogLevel.Trace);
                    currentShippedItems.AddRange(chest.Items.Select(item => new TrackedItemStack(item)));
                }
            }
        }

        currentShippedItems.Sort();
        stopwatch.Stop();
        Monitor.Log($"[DEBUG] CollectShippedItems executed in {stopwatch.ElapsedMilliseconds} ms", LogLevel.Trace);
        return currentShippedItems;
    }

    private void SaveStats()
    {
        List<TrackedItemStack> shippedItems = GetCurrentShippedItems();
        var date = StardewDate.GetStardewDate();
        new DataFileHandler().SaveShippedItems(
            statsFilePath,
            Game1.player.Name,
            Game1.player.farmName.Value,
            shippedItems,
            date,
            Game1.uniqueIDForThisGame
        );

        Monitor.Log($"Saved shipping stats for {shippedItems.Count} items", LogLevel.Info);
        PrintShippedItems(shippedItems);
    }

    private void ShowStatsMenu()
    {
        if (stardewUIViewEngine == null)
        {
            return;
        }

        List<TrackedItemStack> shippedItems = GetCurrentShippedItems();
        var context = new SproutSightViewModel()
        {
            CurrentItems = shippedItems,
            TrackedData = trackedData,
            TodayGoldIn = todayGoldIn,
            TodayGoldOut = todayGoldOut
        };

        Game1.activeClickableMenu = stardewUIViewEngine.CreateMenuFromAsset($"{viewAssetPrefix}/SproutSightView", context);
    }

    private void PrintShippedItems(List<TrackedItemStack> shippedItems)
    {

        if (shippedItems.Count == 0)
        {
            Monitor.Log("No shipped items", LogLevel.Debug);
        }
        else
        {
            Monitor.Log("All shipped items (Main bin + Mini-Shipping Bins):", LogLevel.Debug);
            foreach (TrackedItemStack item in shippedItems)
            {
                Monitor.Log($"  - ({item.StackCount}x {item.Name}): {item.SalePrice}g - {item.CategoryName}", LogLevel.Debug);
            }
        }
    }

}

public sealed class ModConfig
{
    public bool EnableHudIcon { get; set; }
    public KeybindList ToggleKey { get; set; }

    public ModConfig()
    {
        EnableHudIcon = true;
        ToggleKey = KeybindList.Parse("F8");
    }
}
