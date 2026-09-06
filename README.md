# Simple Mending Yourself

A RimWorld 1.6 addon for [Simple Mending](https://steamcommunity.com/sharedfiles/filedetails/?id=3657705987) that lets pawns repair their own worn apparel and equipped weapons at a mending bench.

## How It Works

Pawns with the **Basics** work type enabled will automatically go to a Simple Mending bench and repair their own equipment when it falls within the configured HP% range. The pawn fetches the required repair materials from the map, brings them to the bench, and performs the repair — no need to haul the item itself.

- Repair duration: Uses Simple Mending's repair calculation, with a configurable speed multiplier (default 50%, meaning 2x duration)
- Repair cost: same as Simple Mending (25% of item's material cost)
- Both apparel and primary weapons are supported

## Mod Settings

Access via Options → Mod Settings → Mend Yourself:

- **Upper HP threshold** (default 80%): Pawn won't mend items above this — prevents constant upkeep of barely-worn gear
- **Lower HP threshold** (default 20%): Pawn won't mend items below this — balance adjustment to discourage repairing nearly-destroyed gear
- **Speed multiplier** (default 50%): Adjusts repair speed relative to Simple Mending's base duration — 50% means 2x slower, 200% means 2x faster

## Requirements

- RimWorld 1.6
- [Simple Mending](https://steamcommunity.com/sharedfiles/filedetails/?id=3657705987)

## Building

1. Open `Source/SimpleMendingYourself.sln` in Visual Studio or Rider
2. Build the project — the DLL is copied to `Assemblies/` automatically

To also copy the built mod into the local RimWorld installation, build with
`-p:DeployToGame=true`.

CI builds are reproducible and do not require a local RimWorld installation. Run
`./Tools/CI/Build.ps1` from PowerShell to validate XML and translations, fetch the
pinned Simple Mending dependency, compile, and create a checksummed release package.

Version tags create GitHub Releases. Steam Workshop publishing is separately gated;
see `Docs/development/WorkshopAutomation.zh-CN.md` for the credential and safety setup.

## License

MIT
