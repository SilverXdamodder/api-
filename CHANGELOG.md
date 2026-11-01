# Changelog

All notable changes to **ii Stupid Remake**, a fork of
[ii's Stupid Menu](https://github.com/iiDk-the-actual/iis.Stupid.Menu).

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project
uses [semantic versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] — 2026-11-01

### Fixed

- The `TS` placements spawned nothing on any map. The map check now reads
  `ZoneManagement.instance.activeZones` (the local rig's `zoneEntity` is only a fallback), accepts the
  aliases a map can report (`Metropolis` / `city` / `cityNoBuildings` / `cityWithSkyJungle` and
  `forest` / `forestWithCity` / `GTFC`), and treats an empty or `none`-only zone set as "not known yet"
  instead of "wrong map". If no zone can be read at all, the statue spawns rather than the button
  silently doing nothing.

### Changed

- `TS` buttons are plain spawn buttons now, so a saved toggle state can't leave one stuck in the
  "already enabled" position; **Remove TS Statues** clears everything you placed.

## [1.0.1] — 2026-11-01

Release cut from the same source as 1.0.0's re-uploaded build. From this version on, every repo
update ships a new release with the built DLL and the version bumped by one patch number.

### Changed

- Version is `1.0.1`.

## [1.0.0] — 2026-11-01

First public release of the fork. Full source published under **GPL-3.0**.

### Added

- Compatibility layer for the current Gorilla Tag build: `Extensions/GameCompat.cs` plus call-site
  ports for the rig cache, cosmetics string set, swim parameters, hoverboard, terminal controls,
  social camera, debug HUD, `CosmeticsV2Spawner_Dirty.isPrepared` and
  `GameEntityManager.itemPrefabFactory`.
- Local-first console assets: `PreloadedAssets.txt`, `icon.png` and `crown.png` are read from
  `iisStupidMenu/ServerData` before any network request, with the web host used only as a fallback.
- Offline server data: when the `serverdata` endpoint is unreachable the menu loads the bundled
  `ServerData/serverdata.json`, so admins, MOTD and polls keep working.
- Patch diagnostics: failed Harmony patch classes are listed in the load notification and written to
  `iisStupidMenu/PatchErrors.txt`.
- **Refresh Private Mods** button, plus a lazy scan: `Private Mods` is now populated from every asset
  bundle in `iisStupidMenu/ServerData` when the page is opened, and can be rescanned without restarting.
  A full set (211 files) lists 561 spawnable prefabs.
- **Travis Scott Event Forest placements** — `TS Metro`, `TS Skatepark`, `TS Beach`, `TS Forest V2` and
  `TS Forest V3` on the Private Mods page. Each spawns the `travis` console asset at that map's recorded
  position and heading over `ReceiverGroup.All`, so every console user sees the same statue.

### Changed

- Version is `1.0.0`. The build contains no auto-updater and no version gate.
- `Directory.Build.props` now points at the real Steam install, and the build copies `ServerData/`
  into `iisStupidMenu/ServerData` alongside the DLL.
- **Private Mods** and **Console Assets** are populated from the asset bundles found in
  `iisStupidMenu/ServerData` (files starting with `UnityFS`).
- The watermark is a **filled orange cone** and is no longer tinted: the load-screen overlay and the
  in-world menu panel render it at a fixed white tint so theme colours, gradients, `Swap GUI Colors`
  and custom themes can't recolour it.
- Repository layout for publishing: game/BepInEx assemblies, `Resources/Server/Audio`,
  `Resources/Server/Videos` and local console bundles are excluded from version control.
- **Console Assets** and **Private Mods** are console-admin only. The Main page buttons are added from
  `SetupAdminPanel` once the admin list loads, and sit directly under **Admin Mods**.
- Each `TS` placement refuses to spawn outside its own map zone and reports which map it needs.

### Removed

- `ControllerPatch` — `ConnectedControllerHandler.DeviceDisconnected` no longer exists in the game;
  "Force Enable Hands" now drives `overrideLeftEnable` / `overrideRightEnable` directly.
- Two obsolete telemetry patches (`EnqueueTelemetryEventPlayFab`, `FlushPlayFabTelemetry`) whose
  game APIs were removed.

### Fixed

- **Room join lockout.** Any failed Harmony patch used to set `CriticalPatchFailed`, which made the
  menu show *"A critical patch has failed, and you have been blocked from joining rooms for safety
  reasons"*, call `GorillaComputer.GeneralFailureMessage` and send the player back to single player
  on every spawn. Patch failures are now non-fatal and only reported.
- `CriticalPatchFailed` is only raised for `[SecurityPatch]` failures, matching its documented intent.
- Console assets and admin icons no longer depend on
  `raw.githubusercontent.com/iiDk-the-actual/Console`, which returns 404 for every file.
- Server data no longer leaves the menu without admins or MOTD when the endpoint fails.
- Rig lookups use `VRRigCache.AllRigs` instead of the removed `GorillaParent.instance.vrrigs`,
  fixing every feature that iterated players.
- A single corrupt bundle (`medkit`) aborted the entire Private Mods scan. Each bundle is now loaded
  independently; unreadable files are logged and skipped.
- Console assets failed silently when a bundle or asset could not be loaded. `SpawnConsoleAsset` now
  reports it ("Couldn't spawn X: the Y bundle failed to load") and `LoadAsset` falls back to matching
  by asset file name instead of throwing.
- Refreshing Private Mods rebuilds the list from a pristine copy, so buttons are no longer duplicated.
