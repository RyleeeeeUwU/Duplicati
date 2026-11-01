# Duplicati

A Beat Saber (PC) mod that scans your `CustomLevels` folder at game start and removes duplicate maps, so only one copy of each stays downloaded.

Versions: Beat Saber **1.39.1** and **1.40.8**

## What counts as a duplicate

Two level folders are duplicates when their `Info.dat` **and every beatmap file it references** are byte-identical. Folder names do not matter, so `2ab3f (Song - Mapper)` and a manually renamed `Song` folder with the same content are caught.

Not treated as duplicates (on purpose):

- The same song mapped by different people (different charts).
- A re-upload or update of a map with changed difficulties (different content).
- Any level whose `Info.dat` is missing, unparsable or unreadable. These are skipped and logged, never touched.

Audio and cover images are not hashed. The fingerprint is internal and is **not** the BeatSaver hash, so it is never compared against BeatSaver.

## Which copy is kept

1. A folder named like a BeatSaver download (`<hex key> (<song> - <mapper>)`).
2. Otherwise the oldest folder (by creation time).
3. Otherwise alphabetical path, so the result is deterministic.

The name check is only a tie-breaker; it never decides whether something is a duplicate.

## Behaviour and safety

- Runs once at game start, before SongCore indexes your songs. On large libraries expect a few seconds of extra startup on the first run; check the log for timing.
- **Default: duplicates are moved, not deleted**, to `UserData/Duplicati/Removed`. Delete that folder yourself once you are happy. Set `PermanentlyDelete` for real deletion.
- Nested folders inside `CustomLevels` are scanned. Linked folders (symlinks/junctions) below the root are skipped.
- `CustomWIPLevels` and any other song folder are never touched.
- Any error is logged and swallowed; the mod cannot crash the game.

Config lives in `UserData/Duplicati.json` (created on first launch):

| Key | Default | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Master switch |
| `DryRun` | `false` | Only log what would be removed |
| `PermanentlyDelete` | `false` | Delete instead of moving to `Removed` |
