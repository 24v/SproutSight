# Changelog

## 0.1.0 - Initial Release
First release of the Sprout Sight Mod.

### Features
- Saves shipping data to CSV files
  - Tracks items shipped from both main shipping bin and mini-shipping bins
  - Records item details including quality, quantity, and sale price
  - Saves data per player and farm
  - CSV format for easy external analysis

- In-game statistics viewer
  - Accessible via configurable hotkey (default: F8) or HUD icon
  - Shows currently shipped items and and their total proceeds
  - Historical view with daily, seasonal, and yearly breakdowns
  - Visual graphs showing shipping trends over time

- Configuration options
  - Configurable hotkey to open statistics viewer
  - Toggle for HUD icon visibility
  - Compatible with Generic Mod Config Menu

### Technical Requirements
- SMAPI 4.0.0 or later
- StardewUI 0.6.0 or later

### Notes
- Not yet tested in multiplayer environments
- CSV files are saved in the mod directory as "[PlayerName] Track Stats.csv"
