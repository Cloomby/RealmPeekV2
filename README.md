# RealmPeek - osu!lazer Database Management Tool

> ⚠️ **EXPERIMENTAL**: This tool directly modifies osu!lazer's Realm database. Always backup your `client.realm` file before use. Improper use may corrupt your beatmap library.

## 🎯 Vision

RealmPeek is a power-user tool for diagnosing, repairing, and optimizing osu!lazer's Realm database. It aims to solve issues that the game's built-in maintenance tools cannot handle, such as:

- Orphaned beatmaps and ghost references
- Corrupted metadata and missing dates
- Status mismatches between local DB and osu! servers
- Broken file hashes preventing imports
- Database bloat from deleted content

## ✨ Features

### Current
- **Database Auditing**: Scan for orphans, ghosts, and inconsistencies
- **API Sync**: Verify beatmap status and metadata against osu! servers
- **Smart Cleanup**: Remove orphaned beatmaps and reverse orphans (sets with ghost references)
- **Status Fixes**: Correct ranked/loved/graveyard status mismatches
- **Date Backfilling**: Populate missing submission/ranked dates
- **Hash Recovery**: Copy file hashes from corrupted databases
- **Bulk Download**: Queue missing/outdated beatmaps for re-download
- **GUID Inspector**: Deep dive into specific beatmap sets/maps

### Roadmap
- [ ] GUI interface (Terminal.Gui or Spectre.Console)
- [ ] CLI argument support for automation
- [ ] Integration with osu.NET library for robust API handling
- [ ] Complete Realm schema documentation
- [ ] File hash recovery from game files
- [ ] Duplicate detection and merging
- [ ] Collection management
- [ ] Score/replay preservation during cleanup

## 🏗️ Architecture

### Current State (Pre-Refactor)
```
Program.cs (800+ lines) - Monolithic, mixed concerns
├── Database access (Realm queries)
├── API communication (osu! API)
├── Business logic (audit, fix, download)
└── File I/O (cache, downloads)
```

### Target Architecture
```
RealmPeek/
├── Core/
│   ├── Models/          # Domain models (BeatmapSet, Beatmap, File, etc.)
│   ├── Services/        # Business logic layer
│   │   ├── AuditService.cs
│   │   ├── SyncService.cs
│   │   └── RepairService.cs
│   └── Data/
│       ├── RealmRepository.cs    # Database abstraction
│       └── OsuApiClient.cs       # API wrapper (or use osu.NET)
├── Infrastructure/
│   ├── FilePathHelper.cs
│   ├── Cache/
│   └── Logging/
├── Cli/                 # CLI interface
│   ├── Commands/
│   └── MenuSystem.cs
└── Gui/                 # Future GUI (Terminal.Gui)
    └── MainWindow.cs
```

## 📊 Realm Database Schema

osu!lazer uses Realm for local storage. Understanding the schema is critical:

### Core Tables

#### BeatmapSet (Primary container)
- **ID**: GUID (primary key)
- **OnlineID**: int (beatmapset ID from osu! servers, -1 if local)
- **Status**: int (enum: -4=Local, -3=Unknown, -2=Graveyard, -1=WIP, 0=Pending, 1=Ranked, 2=Approved, 3=Qualified, 4=Loved)
- **DateAdded**: DateTimeOffset (when imported to local DB)
- **DateSubmitted**: DateTimeOffset? (when uploaded to osu!)
- **DateRanked**: DateTimeOffset? (when ranked/loved)
- **Hash**: string? (SHA-256 of .osz file)
- **DeletePending**: bool
- **Protected**: bool
- **Beatmaps**: IList<Beatmap> (difficulties in this set)
- **Files**: IList<RealmNamedFileUsage> (audio, images, .osu files)

#### Beatmap (Individual difficulty)
- **ID**: GUID (primary key)
- **OnlineID**: int (beatmap ID from osu! servers, -1 if local)
- **BeatmapSet**: BeatmapSet? (parent set - **INVERSE RELATIONSHIP**)
- **Metadata**: BeatmapMetadata (title, artist, mapper)
- **DifficultyName**: string?
- **StarRating**: double
- **Status**: int (should match parent set)
- **Hash**: string? (SHA-256 of .osu file)
- **MD5Hash**: string? (legacy MD5 for online comparison)
- **OnlineMD5Hash**: string? (expected hash from server)
- **Length**: double (seconds)
- **BPM**: double
- **Ruleset**: Ruleset? (game mode)
- **Difficulty**: BeatmapDifficulty? (CS, AR, OD, HP)

#### File (Shared file store)
- **Hash**: string (primary key, SHA-256)
- Referenced by RealmNamedFileUsage to avoid duplicate storage

#### RealmNamedFileUsage (Links files to sets)
- **File**: File? (actual file hash)
- **Filename**: string? (e.g., "audio.mp3", "bg.jpg", "difficulty.osu")

### Relationships

```
BeatmapSet (1) ─────< (N) Beatmap
    │                       │
    │                       └─ Metadata (embedded)
    │                       └─ Difficulty (embedded)
    │
    └─────< (N) RealmNamedFileUsage
                      │
                      └─> (1) File (shared)
```

### Common Issues

1. **Orphaned Beatmaps**: `Beatmap.BeatmapSet == null` - difficulty exists but parent set was deleted
2. **Reverse Orphans**: `BeatmapSet.Beatmaps` contains GUIDs that don't exist in Beatmap table
3. **Ghost Files**: `RealmNamedFileUsage.File == null` or points to non-existent hash
4. **Status Mismatches**: Local status differs from osu! API (e.g., local=Ranked, online=Graveyard)
5. **Missing Dates**: `DateRanked`/`DateSubmitted` are null for ranked maps
6. **Invalid OnlineIDs**: Set has OnlineID but beatmaps have `-1`, preventing API queries

## 🔧 Technical Details

### File Hash System

osu!lazer uses **content-addressable storage**:
1. Files are stored in `files/` directory named by their SHA-256 hash
2. `File` table acts as a registry of all hashes
3. `RealmNamedFileUsage` maps logical names to hashes
4. Multiple beatmap sets can share the same file (e.g., same audio file)

**Hash Recovery**:
- If `File.Hash` is corrupted but file exists in `files/`, we can recalculate SHA-256
- If file is missing, we must re-download the entire beatmap set
- Cross-referencing with other databases (e.g., corrupted → good) can recover hashes

### osu! API Integration

Currently uses custom wrapper. **Migration to osu.NET** recommended for:
- Proper OAuth2 handling
- Rate limiting
- Type-safe models
- Active maintenance

**API Endpoints Used**:
- `POST /oauth/token` - Authentication
- `GET /beatmaps?ids[]=X&ids[]=Y` - Bulk beatmap lookup (returns beatmapset data nested)

**Key Challenge**: API `/beatmaps` endpoint requires **beatmap IDs**, not beatmapset IDs. If all beatmaps in a set have `OnlineID=-1`, we cannot query the API.

## 🚀 Usage

### Normal Mode (Scan & Fix)
```bash
RealmPeek.exe
# Select: [ENTER] - Normal scan & fix
# Review action plan
# Type: EXECUTE (apply changes) or EXECUTE_DL (apply + download)
```

### Hash Fix Mode
```bash
RealmPeek.exe
# Select: HASH_FIX
# Enter good DB path and corrupted DB path
```

### Inspect Mode
```bash
RealmPeek.exe
# Select: INSPECT
# Enter BeatmapSet GUID and/or Beatmap GUID
```

### CLI Arguments (Future)
```bash
RealmPeek.exe scan --realm "path/to/client.realm"
RealmPeek.exe fix --auto-download --no-backup
RealmPeek.exe inspect --set "be512858-8d88-4d61-bd66-dc4af211e367"
```

## ⚠️ Safety

**Always backup before use!**
```bash
cp client.realm client.realm.backup
```

RealmPeek creates `client_modified.realm` - you must manually replace the original.

**What can go wrong**:
- Deleting sets that still have valid files (re-import required)
- Status fixes applied incorrectly (cosmetic, easily fixed)
- Date backfills using wrong dates (rare, API is source of truth)

**What's safe**:
- Orphan cleanup (removing dangling references)
- Ghost file cleanup (files already inaccessible)
- API sync (osu! servers are authoritative)

## 🛠️ Development

### Prerequisites
- .NET 8.0 SDK
- Realm .NET SDK
- osu!lazer installation (for testing)

### Building
```bash
dotnet build
dotnet run
```

### Testing
Use a **copy** of your real database for testing!
```bash
cp "C:\Users\<User>\AppData\Roaming\osu\client.realm" .\test_data\
```

## 📚 Resources

- [osu!lazer source code](https://github.com/ppy/osu/)
- [Realm .NET documentation](https://www.mongodb.com/docs/realm/sdk/dotnet/)
- [osu! API v2 documentation](https://osu.ppy.sh/docs/index.html)
- [osu.NET library](https://github.com/Kiritsu/OsuSharp) (candidate for migration)

## 🤝 Contributing

This is experimental software. Contributions welcome, but test thoroughly!

1. Fork the repository
2. Create a feature branch
3. Test with **copy** of real database
4. Submit pull request with detailed description

## 📄 License

MIT License - Use at your own risk!

---

**Made with ❤️ for the osu! community**
