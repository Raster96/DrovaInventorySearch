# Drova Inventory Search

Mod for **Drova - Forsaken Kin** that adds a search bar to the inventory window for quick item filtering.
<img width="544" height="795" alt="image" src="https://github.com/user-attachments/assets/afd4df01-9946-4384-b863-96dd1dd92620" />

## Features

- **Search Bar**: Adds an input field at the bottom of the inventory window
- **Smart Filtering**: Type item name to filter and show only matching items
- **Auto-Category Switch**: Automatically switches to "All" category when searching
- **State Restoration**: Returns to original category and shows all items when search is cleared
- **Localized Item Names**: Searches using the game's localized item names
- **Localized Placeholder**: Search bar placeholder text adapts to the game's language
- **Keyboard Shortcut**: Press `Ctrl+F` to focus the search bar instantly

## Usage

1. Open your inventory (`I` key)
2. A search bar will appear at the bottom of the inventory window
3. Click the search bar **or press `Ctrl+F`** to focus it
4. Type any part of an item name to filter items
5. The mod will:
   - Switch to "All" category if you're in a specific category
   - Hide items that don't match your search
   - Show only matching items
6. Press `Esc` or clear the search text to restore the original view

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+F` | Focus the search bar |
| `Esc` | Clear search and deactivate the search bar |

## Example

- You're in the "Potions" category
- You type "sword" in the search bar
- The inventory switches to "All" category
- Only items with "sword" in their name are shown
- All other items are hidden
- Clear the search to see all items again

## Installation

1. Make sure you have **MelonLoader** installed for Drova
2. Install **Drova Modding API** mod (required dependency)
3. Download the latest release from the [Releases](../../releases) page
4. Copy `DrovaInventorySearch.dll` to: `[Game Folder]/Mods/`
5. Launch the game

## Requirements

- Drova - Forsaken Kin (https://store.steampowered.com/app/1585180/Drova__Forsaken_Kin/)
- MelonLoader (https://melonwiki.xyz/)
- Drova Modding API (https://github.com/Drova-Modding/Drova-Modding-API/releases)

## Technical Details

- Uses dynamic type discovery to work with game classes
- Reflection-based approach for compatibility
- Real-time filtering as you type
- Localization-aware item name matching

## Version

**Current: 1.0.1**

### Changelog

#### v1.0.1
- **Improved**: Enhanced input field appearance with in-game textures and search icon
- **Improved**: Changed initialization method - search bar now appears significantly faster
- **Fixed**: Text overflow issue where long search queries would extend beyond the input field

#### v1.0.0
- Initial release
- Basic search functionality
- Multi-language support
- Keyboard shortcuts (Ctrl+F, Esc)

## Credits & Legal

**Textures**: The textures in the `Textures/` folder are extracted from *Drova - Forsaken Kin*. All rights to these assets belong to **Just2D GmbH**. They are used here solely for the purpose of maintaining visual consistency with the game's UI.

**Mod Author**: Created for the Drova modding community
