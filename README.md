# SproutSight Pro&trade;

SproutSight Pro&trade; is a comprehensive financial analytics tool for your Stardew Valley farm. Track and analyze your shipping data, wallet gold, and cash flow with an intuitive interface that provides detailed insights across days, seasons, and years. Whether you're optimizing profits or just curious about your farm's performance, SproutSight Pro&trade; offers powerful visualization and aggregation tools to help you understand your farm's financial health. All data is automatically saved to CSV files for additional external analysis.

**Download on [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/31705/)** \
**Source on [Github](https://github.com/24v/SproutSight)**

![SproutSight UI](<docs/images/Shipping View.png>)

## Features
- 💼 **Comprehensive Financial Data**: View shipments, wallet gold, and cash flow in a single interface
- 📈 **Visual Statistics**: View your data with daily, seasonal, and yearly breakdowns
- 🔢 **Advanced Aggregation Options**: Analyze your data using min, max, sum, or average aggregations
- 📅 **Year Selection**: Select which years to view data on
- 💰 **Real-time Tracking**: See currently shipped items and their total proceeds
- 📊 **Detailed Shipping Records**: Tracks all items shipped through both main shipping bin and mini-shipping bins
- 📁 **CSV Export**: All data is saved in CSV format (at the end of the day) for easy external analysis
- ⚙️ **Configurable Interface**: Access via hotkey (default: F8) or optional HUD icon
- ️⭐ **Star Control Support**: Integrated with StarControl for easy controller setup

## Usage
- Press F8 (configurable) or click the shipping bin icon in the HUD to open the statistics viewer
- The mod automatically tracks all items you put in shipping bins and save the data at the end of the day.

## Installation
1. Install [SMAPI](https://smapi.io/) (4.0.0 or later)
2. Install [StardewUI](https://www.nexusmods.com/stardewvalley/mods/TODO) (0.6.0 or later)
3. Download this mod from [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/31705)
4. Unzip the mod into your `Stardew Valley/Mods` folder
6. Ship stuff. 
7. ???
8. Profit

## Configuration
Use [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) to configure:
- Toggle HUD icon visibility
- Change the hotkey to open statistics viewer

## Compatibility
- Tested on Stardew Valley 1.6.15
- Tested on SMAPI 4.1.10
- Tested on StardewUI 0.6.0
- Not yet tested in multiplayer environments

## Data Files
- The shipping data is saved as a CSV in the SproutSight Mod directory. It follows the filename pattern ```<FarmerName>_<UniqueSaveId>.csv```
- The wallet and cash flow data is saved as a CSV in the SproutSight Mod directory. It follows the filename pattern ```<FarmerName>_<UniqueSaveId>_gold.csv```

## Credits
- Inspired by (and some code taken) from [Iceburg333's Shipment Tracker](https://www.nexusmods.com/stardewvalley/mods/321)
- StardewUI for making this easy.

## Feedback and Support
Feedback and comments appreciated.
- Create an issue on [GitHub](https://github.com/24v/SproutSight)
- Find me (https://discord.com/users/malcier) on the [Stardew Valley Discord](https://discord.gg/stardewvalley)

## See also
- [Changelog](CHANGELOG.md)

## License
- [MIT License](LICENSE)
