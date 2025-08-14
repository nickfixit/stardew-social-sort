# Custom Social Sort - Stardew Valley Mod

A SMAPI mod that adds custom drag-and-drop sorting functionality to the Social tab in Stardew Valley.

## Features

- **Custom Sort Button**: Toggle custom sorting mode on/off
- **Drag & Drop**: Intuitive drag-and-drop interface for reordering NPCs
- **Visual Feedback**: Clear drop indicators and ghost previews while dragging
- **Per-Save Persistence**: Custom order is saved per game save file
- **Safe Implementation**: Uses overlay rendering to maintain compatibility across game versions

## Installation

1. Install [SMAPI](https://smapi.io/) (version 4.0.0 or later)
2. Download the latest release from [releases page]
3. Extract the mod folder to your `Mods` directory
4. Run the game through SMAPI

## Usage

1. Open the Social tab in-game
2. Click the "Custom Sort" button (appears next to the "Sort by Name" button)
3. When active, drag NPCs up and down to reorder them
4. Click the button again to disable custom sorting
5. Your custom order is automatically saved with your game

## Technical Details

- Built for Stardew Valley 1.6+ and .NET 6
- Uses Harmony patches for safe integration
- Reflection-based field access for version compatibility
- SMAPI data API for save persistence

## License

This mod is open source. Feel free to modify and redistribute.

## Credits

- Built with love for the Stardew Valley modding community
- Special thanks to the SMAPI team for the excellent modding framework