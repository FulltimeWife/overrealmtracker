# Overrealm Tracker

A lightweight overlay for **Torchlight: Infinite** that reads the game's log
file and displays various stuff:

- Per Boss tracking.
- Per Floor tracking.
- Item counts (I know solid river maybe doesn't work, it's not as simple as simply providing the ID).

---

## Download & install

1. Download **`OverrealmTracker.exe`** from the
   [latest release](../../releases/latest).
2. Save it in its own folder, e.g `D:\TLITools\ORTracker`.
   **Do not** put it in `Program Files`.
3. Run it
   - There should be one JSON files created at runtime which is the config file `config.json`.
   - `config.json` This just contains the config for log file, position, boss name etc.
   - The second file, which tracks the data, is created upon first entry into the Overrealm which is the `tracker_data.json`.
   - `tracker_data.json` This actually records the data you see.
   - Both of these are freeable changeable at any point, the tracker only creates them with the default boilerplate.
   - It shouldn't override your custom names/floors (maybe) each release if you want to change them yourself.

There is **nothing to install**. It's a single self-contained `.exe`.

> **Windows may warn "Windows protected your PC"** on first run, this is normal as it's an unsigned executable.

---

## First-time setup

1. Launch `OverrealmTracker.exe`. The overlay then appears & should say **"Log not found"** if your log location differs from default.
2. **Right-click the overlay → Choose log file…**
3. Point it at your game's `UE_game.log`.
   > Default Steam location: `"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Torchlight Infinite\\UE_game\\TorchLight\\Saved\\Logs\\UE_game.log"`
