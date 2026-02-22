# Simple Mending Yourself

A RimWorld 1.6 addon for [Simple Mending](https://steamcommunity.com/sharedfiles/filedetails/?id=3657705987) that lets pawns repair their own worn apparel and equipped weapons at a mending bench.

## How It Works

Pawns with the **Basics** work type enabled will automatically go to a Simple Mending bench and repair their own equipment when it falls within the configured HP% range. The pawn fetches the required repair materials from the map, brings them to the bench, and performs the repair — no need to haul the item itself.

- Repair duration: **50% of Simple Mending's normal duration** (with a configurable minimum floor)
- Repair cost: same as Simple Mending (25% of item's material cost)
- Both apparel and primary weapons are supported

## Mod Settings

Access via Options → Mod Settings → Mend Yourself:

- **Upper HP threshold** (default 80%): Pawn won't mend items above this — prevents constant upkeep of barely-worn gear
- **Lower HP threshold** (default 20%): Pawn won't mend items below this — balance adjustment to discourage repairing nearly-destroyed gear
- **Minimum mend duration** (default 300 ticks): Floor for repair work time regardless of labor speed

## Requirements

- RimWorld 1.6
- [Simple Mending](https://steamcommunity.com/sharedfiles/filedetails/?id=3657705987)

## Building

1. Open `Source/MendYourself.sln` in Visual Studio or Rider
2. Build the project — the DLL is copied to `Assemblies/` automatically

## License

MIT
