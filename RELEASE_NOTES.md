# Release Notes

## v1.1.0

### Added
- Added a **Refresh Saves** button next to the database selector.
- The button refreshes the available games/saves in **Select Game** without requiring the Aurora database to be loaded again.

### Fixed
- Fixed an issue where removing the currently selected Aurora game/save while Aurora MarvinS was open could cause an unhandled error.
- Improved handling of the game/save selection state after a game/save is removed.

### Usage
After creating or removing a game/save in Aurora:
1. Save the change in Aurora so it is written to the database.
2. Return to Aurora MarvinS.
3. Click **Refresh Saves**.
4. Select the game/save from **Select Game** if needed.

There is no need to close and reopen Aurora MarvinS or reload the database just to update the game/save list.
