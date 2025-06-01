# Changelog

## 0.3.0 - Iconic Framework Integration

### Added
- Iconic Framework Integration
- Bugfix for config settings not persisting

## 0.2.0 - Enhanced Financial Analytics
### New Features
- **Comprehensive Financial Data**: Added support for viewing wallet gold and cash flow in addition to shipments
- **Flexible Time Periods**: Enhanced UI to display data by day, season, or year for more detailed analysis
- **Advanced Aggregation Options**: Added multiple aggregation methods (min, max, sum, average) for better data insights
- **Code Refactoring**: Reorganized project structure for better maintainability
### Bug Fixes
- Hud icon no longer shows during cutscenes.

## 0.1.2 - Add support for StarControl
SproutSight should show up in the "Library" for StarControl.

## 0.1.1 - Rename to SproutSight Pro
More official sounding.

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
