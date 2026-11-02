# Overrealm Tracker

A lightweight overlay for **Torchlight: Infinite** that reads the game's log
file and tracks your Overrealm runs in real time:

- Per Boss tracking
- Item counts (I know solid river maybe doesn't work, it's not as simple as simply providing the ID)

---

---

## Download & install

1. Download **`OverrealmTracker.exe`** from the
   [latest release](../../releases/latest).
2. Save it in a folder you control — e.g. `Documents\TLI Tools\`.
   **Do not** put it in `Program Files` (Windows won't let it save state there).
3. Run it. First launch creates two files next to the exe:
   - `config.json` — settings (see Configuration below)
   - `tracker_data.json` — your accumulated counts

There is **nothing to install**. It's a single self-contained `.exe`.

> **Windows may warn "Windows protected your PC"** on first run. That's
> SmartScreen reacting to an unsigned exe — click **More info → Run anyway**.
> The tracker only reads the game's log file; it doesn't touch game memory,
> files, or network traffic.

---

## First-time setup

1. Launch `OverrealmTracker.exe`. A small overlay window appears, and the
   status line reads **"Log not found"**.
2. **Right-click the overlay → Choose log file…**
3. Point it at your game's `UE_game.log`. Default Steam location: **"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Torchlight Infinite\\UE_game\\TorchLight\\Saved\\Logs\\UE_game.log"**
