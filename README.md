# Duplicati

A Beat Saber (PC) mod that finds duplicate maps in your `CustomLevels` folder when you ask it to, and removes them so you keep only one copy of each.

- **Game versions:** **1.39.0** to **1.44.1**
- **Requires:** [BSIPA](https://github.com/nike4613/BeatSaber-IPA-Reloaded) 4.3.0 or newer and [BeatSaberMarkupLanguage (BSML)](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) 1.12.0 or newer.
- **Safe by default:** duplicates are moved to a recovery folder. They are only deleted permanently if you turn that on.

---

## Installation

1. Mod Beat Saber [BSManager](https://github.com/Zagrios/bs-manager).
2. Download the zip that matches your game version from the [Releases](../../releases) page, for example `Duplicati-1.1.0-bs1.40.8.zip`.
3. Extract it into your Beat Saber folder, so that the DLL ends up at `Beat Saber/Plugins/Duplicati.dll`.
4. Make sure BSML is installed, then start the game.
5. Open **Mods → Duplicati** from the left side of the main menu.
6. Choose your options and press **Scan and remove duplicates**. Duplicati does not scan or modify songs at startup.

> [!TIP]
> Enable **Dry Run** in the Duplicati menu first to see how many duplicates would be removed without changing any files.

---

## How it works

When you press **Scan and remove duplicates**, Duplicati runs the following steps on a background worker so filesystem scanning and removal do not block the game's UI:

1. **Scan** every level folder under `Beat Saber_Data/CustomLevels` (any folder that directly contains an `Info.dat`).
2. **Fingerprint** each level from its content.
3. **Group** levels that have the same fingerprint.
4. **Keep** one level from each group and move or delete the others.
5. **Show** a status message while scanning, then report the number of duplicate copies found/removed.
6. **Log** a summary, for example:
   `Done in 49.3s: 10840 levels scanned, 2 skipped, 17 duplicate sets, 21 removed, 0 failed.`

### What counts as a duplicate

Two levels are duplicates when their `Info.dat` **and every beatmap and lightshow file it references** are byte-identical. **Folder names are ignored**, so `2ab3f (Song - Mapper)` and a renamed copy called `My Favourite Song` are still detected as duplicates.

These are **not** treated as duplicates, on purpose:

| Case | Why it is kept |
| --- | --- |
| The same song mapped by different people | The charts are different |
| An updated or re-uploaded map with changed difficulties | The content is different |
| A level with a missing, unreadable or invalid `Info.dat` | It cannot be checked reliably, so it is skipped and logged |

The song audio and cover images are not hashed: they are large files, and `Info.dat` already identifies the chart. The fingerprint is internal to Duplicati and is **not** the same as the BeatSaver map hash, so it is never compared with BeatSaver.

Both `Info.dat` formats are supported: v2/v3 (`_beatmapFilename`) and v4 (`beatmapDataFilename`, `lightshowDataFilename`, `audioDataFilename`).

### Which copy is kept

Within each group of duplicates, Duplicati keeps:

1. a folder named like a BeatSaver download (`<hex key> (<song> - <mapper>)`, for example `2ab3f (Song - Mapper)`);
2. otherwise, the oldest folder (by creation date);
3. otherwise, the first folder in alphabetical order, so the result is always the same.

The folder name is only used to choose which copy to keep. It never decides whether two levels are duplicates.

---

## Configuration

BSIPA creates the config file at `UserData/Duplicati.json` the first time you launch the game with the mod.

```json
{
  "DryRun": false,
  "PermanentlyDelete": false
}
```

| Key | Default | What it does |
| --- | --- | --- |
| `DryRun` | `false` | Only reports what would be removed. No files are changed. |
| `PermanentlyDelete` | `false` | Deletes duplicates permanently instead of moving them to `UserData/Duplicati/Removed`. |

---

## Recovering removed maps

Unless `PermanentlyDelete` is turned on, removed duplicates are moved to:

```
Beat Saber/UserData/Duplicati/Removed/
```

If a folder with the same name is already there, the new one gets a numbered suffix (`Song (2)`, `Song (3)`, and so on). Once you have checked that everything is fine, you can delete this folder to free up space.

> [!NOTE]
> If you move a map from `Removed` back into `CustomLevels` while the other copy is still there, Duplicati will remove it again the next time you run a scan. If you would rather keep that copy, delete the other one first.

---

## Safety

- **Only `Beat Saber_Data/CustomLevels`** is scanned. `CustomWIPLevels` are never touched.
- **Symlinks and junctions** inside `CustomLevels` are skipped, which prevents loops and means nothing is ever deleted through a link.
- **Levels that cannot be read** with certainty are skipped and never removed.
- **The copy being kept is checked again** right before each removal. If it has disappeared, nothing is removed.
- **Moving across drives** (for example, `CustomLevels` on a different drive from `UserData`) copies the folder first and only deletes the original once the copy has completed. If the original cannot be deleted, the copy is undone so it does not pile up.
- **Errors never crash the game:** any problem is written to the log and the game continues normally.

### Responsiveness

The scan and file operations run on a background worker. The Duplicati menu immediately shows `Scanning for duplicates...` and updates with the result when the work is complete. The game's UI remains available while the scan runs. Only one scan can run at a time. The log shows exactly how long the scan took; look for `Duplicati` in `Logs/_latest.log` in the game folder.

---

## Building from source

### Requirements

- A recent [.NET SDK](https://dotnet.microsoft.com/download). The project targets .NET Framework 4.7.2 and uses the `Microsoft.NETFramework.ReferenceAssemblies` package, so it also builds on Linux and macOS without the .NET Framework Developer Pack.
- A modded Beat Saber install with BSIPA and BSML installed. The project references `IPA.Loader.dll`, `Newtonsoft.Json.dll`, BSML, and the required Beat Saber/Unity UI assemblies from your game folder.

### Point the build at your game folder

Set `BeatSaberDir` in one of these ways:

- copy `Duplicati/Duplicati.csproj.user.example` to `Duplicati/Duplicati.csproj.user` (which git ignores) and edit the path (BSMT-Rider can also create this file for you);
- set the `BEAT_SABER_DIR` environment variable;
- pass `-p:BeatSaberDir="/path/to/Beat Saber"` on the command line.

### Build

```bash
dotnet build Duplicati/Duplicati.csproj
```

If your game's `Plugins` folder exists, the DLL is copied into it automatically so you can test right away. To turn that off, add `-p:CopyToPlugins=false`.

### Package a release

A Release build also creates a zip that can be extracted directly into the game folder:

```bash
dotnet build Duplicati/Duplicati.csproj -c Release -p:GameVersion=1.40.8
```

The zip is written to `Duplicati/bin/Release/net472/zip/Duplicati-<version>-bs<gameVersion>.zip`.

To build a zip for every supported game version in one go:

```bash
./scripts/package.sh
```

The in-game menu references BSML and Beat Saber UI assemblies, including `BeatSaber.ViewSystem.dll` (which contains the `HMUI.FlowCoordinator` and `HMUI.ViewController` types used by this menu), so build against the Beat Saber installation you intend to use. The generated `manifest.json` is labelled with the selected `gameVersion`; that label does not itself guarantee compatibility with every game patch.

### Plugin metadata

The plugin ID, name, author, version, description and default game version are set in [`Directory.Build.props`](Directory.Build.props). They use the same property names as the BeatSaberModdingTools templates, and `manifest.json` is generated from them at build time.

---

## Project structure

```
Duplicati/
├── Plugin.cs                    # BSIPA entry point: registers the in-game Mods button
├── Configuration/
│   └── PluginConfig.cs          # Settings stored in UserData/Duplicati.json
├── UI/
│   ├── Duplicati.bsml             # Embedded BSML menu layout
│   ├── DuplicatiFlowCoordinator.cs
│   └── DuplicatiViewController.cs # Starts scans on a background worker
└── Core/                        # Cleanup logic, independent of BSIPA
    ├── LibraryCleaner.cs        # Runs the whole process: scan → fingerprint → group → remove
    ├── LevelScanner.cs          # Finds level folders (skipping links)
    ├── LevelFingerprint.cs      # Content hash from Info.dat and the files it references
    ├── DuplicateFinder.cs       # Groups duplicates and chooses which copy to keep
    ├── DuplicateRemover.cs      # Moves duplicates to Removed or deletes them
    ├── Models.cs                # LevelEntry, DuplicateGroup, CleanOptions, CleanSummary
    └── ILog.cs                  # Minimal logging interface
Directory.Build.props            # Plugin metadata and default game version
scripts/package.sh               # Builds release zips for every supported game version
```

---

## Author

Made by **Ryleeeee**.

## Disclaimer

This README.md file was written almost entirely by Claude.
