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
using SproutSight.Display;

/*

Todo:
  - Some actual testing, like I am a real programmer.
  - In game testing automation
  - Animate the on hover on hud icon
  - More specific preserved names / icons.
  - Linter / Autoformat / C# Conventions
  - Fix ordering of SML params
  - Memoize aggregations
  - Improve title text
  - MultiPlayers multiFarms

Ideas:
  - Value of all assets
  - Make this an in-game element
  - Track All Expenses (e.g. Purchases, Sales, Ships, Building, Shipping, etc)
  - Show where items are being shipped from

*/

namespace SproutSight;
internal sealed class ModEntry : Mod
{
    private string _statsFolderPath = "";
    private string _viewAssetPrefix = "";

    private IViewEngine? _stardewUIViewEngine;
    private IStarControlApi? _starControl;
    private IGenericModConfigMenuApi? _genericModConfig;
    private ModConfig _config = new();
    private IIconicFrameworkApi? _iconicFrameworkApi;

    // In-Memory Repo of Tracked Data. This is always loaded from file OnDayStart.
    private TrackedData _trackedData = new();
    // Pending items to be saved after the game saves
    private List<TrackedItemStack>? _pendingItems;
    private StardewDate? _pendingDate;

    // The Shipping icon in the hud
    private IViewDrawable? _hud;
    // Needed to respond to click / hover events
    private Rectangle _hudClickableArea;

    // For tracking total gold in and out
    private int _todayGoldIn = 0;
    private int _todayGoldOut = 0;
    private int? _lastGoldAmount = null;


    public override void Entry(IModHelper helper)
    {
        _statsFolderPath = helper.DirectoryPath + Path.DirectorySeparatorChar;
        _viewAssetPrefix = $"Mods/{ModManifest.UniqueID}/Views";

        _config = Helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.Saved += OnSaved;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Display.RenderedHud += OnRenderedHud;

        helper.ConsoleCommands.Add("ssp_print_current", "Print data from today.", PrintCurrent);
        helper.ConsoleCommands.Add("ssp_print_historical", "Shows all historical data.", PrintHistoricalData);
        helper.ConsoleCommands.Add("ssp_show", "Shows the UI display.", ShowStatsMenu);
        helper.ConsoleCommands.Add("ssp_save", "Saves current data to files.", SaveCurrentData);
        helper.ConsoleCommands.Add("ssp_load", "Loads historical data from files.", LoadHistoricalData);

        Logging.Monitor = Monitor;
        Monitor.Log("SproutSight => Initialized!", LogLevel.Info);
    }

    // ================================ 
    // Game Events ====================
    // ================================

    private void OnUpdateTicked(object? sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
    {
        // Day hasnt started
        if (_lastGoldAmount == null) 
        {
            return;
        }
        int currentGold = Game1.player.Money;
        int goldAdded = currentGold - (int)_lastGoldAmount;
        if (goldAdded > 0) 
        {
            _todayGoldIn += goldAdded;
            Monitor.Log($"Player added gold: {goldAdded}. Current Out: {_todayGoldOut}. Current In: {_todayGoldIn}.", LogLevel.Trace);

        } else if (goldAdded < 0) 
        {
            _todayGoldOut += goldAdded;
            Monitor.Log($"Player lost gold: {goldAdded}. Current Out: {_todayGoldOut}. Current In: {_todayGoldIn}.", LogLevel.Trace);

        }
        _lastGoldAmount = currentGold;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        _stardewUIViewEngine = Helper.ModRegistry.GetApi<IViewEngine>("focustense.StardewUI");
        _starControl = Helper.ModRegistry.GetApi<IStarControlApi>("focustense.StarControl");
        _genericModConfig = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        _iconicFrameworkApi = Helper.ModRegistry.GetApi<IIconicFrameworkApi>("furyx639.ToolbarIcons");

        if (_stardewUIViewEngine == null)
        {
            Monitor.Log("Failed to load StardewUI API. Make sure the mod is installed.", LogLevel.Error);
            return;
        }
        _stardewUIViewEngine.RegisterViews($"Mods/{ModManifest.UniqueID}/Views", "assets/views");
        _stardewUIViewEngine.RegisterSprites($"Mods/{ModManifest.UniqueID}/Sprites", "assets/sprites");
        _stardewUIViewEngine.EnableHotReloading("/Users/demo/CascadeProjects/stardew_valley/CascadeProjects/windsurf-project/stardew_mod/SproutSight/SproutSight");

        // Setup gmcm
        if (_genericModConfig is not null)
        {
            _genericModConfig.Register(
                mod: this.ModManifest,
                reset: () => this._config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this._config)
            );

            _genericModConfig.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Show Hud Icon",
                tooltip: () => "Display a shipping to toggle the tracker view.",
                getValue: () => this._config.EnableHudIcon,
                setValue: value => this._config.EnableHudIcon = value
            );
            _genericModConfig.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Tracker View Toggle",
                getValue: () => this._config.ToggleKey,
                setValue: value => this._config.ToggleKey = value
            );
        }

        // Star Control
        if (_starControl is not null)
        {
            StarControlIntegration.Register(_starControl, ModManifest, new Action(ShowStatsMenu));
        }

        // Iconic Framework Integration
        if (_iconicFrameworkApi is not null)
        {
            _iconicFrameworkApi.AddToolbarIcon(
                id: $"{ModManifest.UniqueID}.ShowSproutSight",
                texturePath: "Buildings/Shipping Bin",
                sourceRect: new Rectangle(0, 0, 32, 32),
                getTitle: () => "SproutSight",
                getDescription: () => "Open SproutSight Overview",
                onClick: ShowStatsMenu
            );
            Monitor.Log("IconicFramework icon registered for SproutSight.", LogLevel.Trace);
        }
        else if (_iconicFrameworkApi is null)
        {
            Monitor.Log("IconicFramework API not found. SproutSight icon will not be added to the toolbar.", LogLevel.Trace);
        }

        Monitor.Log("Sprout Sight Updated => Game Launched!", LogLevel.Trace);

    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _pendingItems = null;
        _pendingDate = null;
        _lastGoldAmount = Game1.player.Money;
        _todayGoldOut = 0;
        _todayGoldIn = 0;

        _trackedData = new TrackingDataSerializer().LoadTrackedData(
            _statsFolderPath,
            Game1.player.Name,
            Game1.uniqueIDForThisGame
        );
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        _pendingItems = GetCurrentShippedItems();
        _pendingDate = StardewDate.GetTodaysDate();
        Monitor.Log($"Day ending with {_pendingItems.Count} items queued for save", LogLevel.Trace);
    }

    private void OnSaved(object? sender, SavedEventArgs e)
    {
        if (_pendingItems == null || _pendingDate == null)
        {
            Monitor.Log("No items queued for saving", LogLevel.Trace);
            return;
        }

        var handler = new TrackingDataSerializer();
        handler.SaveShippedItems(
            _statsFolderPath,
            Game1.player.Name,
            Game1.player.farmName.Value,
            _pendingItems,
            _pendingDate,
            Game1.uniqueIDForThisGame
        );


        // todayGoldIn and todayGoldOut will be updated by the UpdateTicked before save
        handler.SaveGoldData(
            _statsFolderPath, 
            Game1.player.Name, 
            Game1.player.farmName.Value, 
            Game1.uniqueIDForThisGame, 
            _pendingDate, 
            _todayGoldIn, 
            _todayGoldOut, 
            Game1.player.Money);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree)
        {
            return;
        }

        if (_config.ToggleKey.GetKeybindCurrentlyDown() != null)
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
        if (!_config.EnableHudIcon) 
        {
            if (_hud != null)
            {
                _hud.Dispose();
                _hud = null;
            }
            return;
        }

        if (!UIElementUtils.IsRenderingNormally()) 
        {
            return;
        }

        _hud ??= _stardewUIViewEngine!.CreateDrawableFromAsset($"{_viewAssetPrefix}/HudIcon");

        int marginRight = 39;
        int marginTop = 262;
        var viewport = Game1.uiViewport;
        var x = viewport.Width - marginRight;
        var y = marginTop;
        _hud.Draw(e.SpriteBatch, new(x, y));

        // We need hudClickable scaled coordinates for the OnButtonPress
        var scaleFactor = Game1.options.uiScale / Game1.options.zoomLevel;
        var scaledX = (int)Math.Round(x * scaleFactor);
        var scaledY = (int)Math.Round(y * scaleFactor);
        var scaledSize = (int)Math.Round(32 * scaleFactor);
        _hudClickableArea = new Rectangle(scaledX, scaledY, scaledSize, scaledSize);

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

    private void PrintCurrent(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Please load a save file first.", LogLevel.Error);
            return;
        }
        List<TrackedItemStack> shippedItems = GetCurrentShippedItems();
        PrintShippedItems(shippedItems);
        Monitor.Log($"Current Gold Totals: Current Out: {_todayGoldOut}. Current In: {_todayGoldIn}. Wallet{Game1.player.Money}", LogLevel.Debug);
    }

    private void PrintHistoricalData(string command, string[] args)
    {
        Monitor.Log("Showing tracked data", LogLevel.Info);
        _trackedData.PrintTrackedData();
    }

    private void SaveCurrentData(string command, string[] args)
    {
        SaveStats();
        Monitor.Log("Saved tracked data", LogLevel.Info);
    }

    private void LoadHistoricalData(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Cannot load data - save not loaded", LogLevel.Warn);
            return;
        }
        _trackedData = new TrackingDataSerializer().LoadTrackedData(
            _statsFolderPath,
            Game1.player.Name,
            Game1.uniqueIDForThisGame
        );
        Monitor.Log("Tracked data loaded successfully.", LogLevel.Info);
    }

    private void HandleHudClick(object? sender, ButtonPressedEventArgs e)
    {

        var clickPosition = e.Cursor.GetScaledScreenPixels().ToPoint();
        if (_hudClickableArea.Contains(clickPosition))
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
        Monitor.Log($"CollectShippedItems executed in {stopwatch.ElapsedMilliseconds} ms", LogLevel.Trace);
        return currentShippedItems;
    }

    private void SaveStats()
    {
        List<TrackedItemStack> shippedItems = GetCurrentShippedItems();
        var date = StardewDate.GetTodaysDate();
        TrackingDataSerializer handler = new TrackingDataSerializer();
        handler.SaveShippedItems(
            _statsFolderPath,
            Game1.player.Name,
            Game1.player.farmName.Value,
            shippedItems,
            date,
            Game1.uniqueIDForThisGame
        );
        handler.SaveGoldData(
            _statsFolderPath, 
            Game1.player.Name, 
            Game1.player.farmName.Value, 
            Game1.uniqueIDForThisGame, 
            date, 
            _todayGoldIn, 
            _todayGoldOut, 
            Game1.player.Money);

        Monitor.Log($"Saved shipping stats for {shippedItems.Count} items", LogLevel.Info);
        Monitor.Log($"Saved gold stats: {_todayGoldIn} {_todayGoldOut} {Game1.player.Money}", LogLevel.Info);
        PrintShippedItems(shippedItems);
    }

    private void ShowStatsMenu()
    {
        if (_stardewUIViewEngine == null)
        {
            return;
        }

        List<TrackedItemStack> shippedItems = GetCurrentShippedItems();
        var context = new SproutSightViewModel(_trackedData)
        {
            CurrentItems = shippedItems,
            TodayGoldIn = _todayGoldIn,
            TodayGoldOut = _todayGoldOut
        };

        Game1.activeClickableMenu = _stardewUIViewEngine.CreateMenuFromAsset($"{_viewAssetPrefix}/SproutSightView", context);
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
