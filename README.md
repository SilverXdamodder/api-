<p align="center">
  <img src="Resources/GitHub/icon.png" height="120px">
</p>

<h1 align="center">ii Stupid Remake</h1>

<p align="center">
  A feature-packed, open-source mod menu for Gorilla Tag — a maintained fork of
  <a href="https://github.com/iiDk-the-actual/iis.Stupid.Menu">ii's Stupid Menu</a>.
</p>

<p align="center">
  <a href="https://github.com/SilverXdamodder/api-/releases/latest"><img src="https://img.shields.io/github/v/release/SilverXdamodder/api-?label=version&style=for-the-badge"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-GPL--3.0-blue?style=for-the-badge"></a>
  <a href="https://github.com/SilverXdamodder/api-/releases/latest"><img src="https://img.shields.io/github/downloads/SilverXdamodder/api-/latest/total?style=for-the-badge"></a>
  <img src="https://img.shields.io/badge/.NET-Standard%202.1-purple?style=for-the-badge">
</p>

---

## What is this?

**ii Stupid Remake** is a fork of ii's Stupid Menu that has been ported to the current Gorilla Tag
build and keeps the full console / private-mod system running without depending on a web host.
It is a BepInEx 5 plugin with 1000+ mods: movement, fun, visuals, overpowered, safety, room mods,
an in-game console, cosmetics and private asset spawning.

Everything here is **open source under the GNU GPL v3.0** — no obfuscation, no paywalls, no
binaries without source.

## Features

| Area | What you get |
|------|--------------|
| **Mods** | 1000+ toggles across Movement, Fun, Visuals, Overpowered, Settings, Safety, Room Mods |
| **Console** | In-game console with admin tools, commands, notifications and console assets |
| **Private Mods** | Spawn any Unity asset bundle you drop into `iisStupidMenu/ServerData` |
| **Cosmetics** | Cosmetic spawner, custom colours, named/patched rigs |
| **Safety** | Anti-ban / anti-report / anti-cheat patches, telemetry blocking, moderation helpers |
| **PC support** | Full keyboard + mouse control (see `iiMenu_PCControls.txt`) |
| **Local-first assets** | Console bundles, admin icons and preload lists are read from disk before any URL |
| **Offline server data** | Falls back to a bundled `serverdata.json` if the web endpoint is unreachable |

## Requirements

- [Gorilla Tag](https://store.steampowered.com/app/1533390/Gorilla_Tag/) (Steam, PC)
- [BepInEx 5.4.23 x64](https://github.com/BepInEx/BepInEx/releases) installed in the game folder
- For building: [.NET SDK 8.0+](https://dotnet.microsoft.com/download) (tested on 10.x) on Windows

## Installation (prebuilt)

1. Download `iis_Stupid_Menu.dll` from the [latest release](https://github.com/SilverXdamodder/api-/releases/latest).
2. Drop the DLL into `Gorilla Tag/BepInEx/plugins/`.
3. Launch the game. On first launch the menu creates its data folder at
   `Gorilla Tag/iisStupidMenu/`.

> Remove or rename any other menu DLL in `BepInEx/plugins` first — two menus patching Photon and
> PlayFab at the same time cause failed joins and crashes.

## Building from source

```bash
git clone https://github.com/SilverXdamodder/api-.git
cd api-
```

1. Open `Directory.Build.props` and set `<GamePath>` to your Gorilla Tag install if Steam put it
   somewhere other than the default:
   ```xml
   <GamePath>C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag</GamePath>
   ```
2. Build:

   ```bash
   dotnet build iiMenu.csproj -c Release
   ```

3. The build copies the DLL and the console data automatically:
   - `bin/Release/netstandard2.1/ii's Stupid Menu.dll` → `BepInEx/plugins/`
   - `ServerData/**` → `iisStupidMenu/ServerData/`

Game and BepInEx assemblies are **not** committed to this repository. `Directory.Build.targets`
refreshes `References/Managed` and `References/core` from `<GamePath>` on every build, so a fresh
clone only needs the game installed.

## Project layout

```
Classes/        Menu core: Console, ServerData, friend manager, achievements
Extensions/     Game API compatibility helpers and rig utilities
Managers/       Notifications, plugins, TTS, translation, custom boards
Menu/           Main menu logic, buttons, UI
Mods/           Every mod category (Movement, Fun, Visuals, Overpowered, Safety, ...)
Patches/        Harmony patches (Menu, Safety, Player)
Resources/      Client (embedded) and server-side fonts/images
ServerData/     Console asset bundles and private mods (read from disk)
Utilities/      Assets, files, rig and hash helpers
```

## Console assets & private mods

Anything that looks like a Unity asset bundle (files starting with `UnityFS`) inside
`Gorilla Tag/iisStupidMenu/ServerData/` shows up in the menu under **Console Assets** and
**Private Mods**. `PreloadedAssets.txt` lists the bundles to preload; `icon.png` and `crown.png`
are the admin console textures. All of these are read locally first, so the menu keeps working even
if the upstream GitHub raw host is gone.

## Server data / API

Server data (admins, MOTD, poll, detected mods) is fetched from the endpoint configured in
`Classes/Menu/ServerData.cs`:

```csharp
public static readonly string ServerDataEndpoint = "https://silverxconsole.silverxconsoleapi.workers.dev/serverdata";
```

If that request fails, the menu loads `iisStupidMenu/ServerData/serverdata.json` instead, so admins
and the MOTD keep working offline. Every entry in `super-admins` may be either an admin name or a raw
player user ID; names are matched against the `admins` list. The JSON format is:

```json
{
  "menu-version": "1.1.0",
  "min-version": "1.0.0",
  "min-console-version": "2.8.0",
  "discord-invite": "https://discord.gg/...",
  "motd": "Message of the day",
  "admins": [{ "name": "Name", "user-id": "PLAYERID" }],
  "super-admins": ["Name or PLAYERID"],
  "patreon": [],
  "detected-mods": [],
  "poll": "Question?",
  "option-a": "Yes",
  "option-b": "No"
}
```

## Troubleshooting

| Problem | Fix |
|---------|-----|
| Menu does not appear | Check `BepInEx/LogOutput.log`; make sure the DLL is in `BepInEx/plugins` and the game is fully restarted |
| "patches failed to initialize" notification | The failed patch classes are listed and written to `iisStupidMenu/PatchErrors.txt` — patches never block you from joining rooms |
| Private Mods list empty | Put asset bundles in `Gorilla Tag/iisStupidMenu/ServerData/` and restart |
| Cannot join rooms with another menu installed | Remove the other menu DLL; multiple menus conflict |

## License

This project is licensed under the **GNU General Public License v3.0** — see [LICENSE](LICENSE).

Because it is built on ii's Stupid Menu (GPL-3.0), any fork or redistribution **must**:

- stay open source under the same license,
- keep the original copyright notices,
- credit the original authors.

```
ii's Stupid Menu  —  Copyright (C) 2026  Goldentrophy Software
https://github.com/iiDk-the-actual/iis.Stupid.Menu

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
```

## Credits

- **iiDk / Goldentrophy Software** — original ii's Stupid Menu, the base of this project.
- **SilverX** — fork maintenance, console/server data and the port to the current Gorilla Tag build.
- **Harmony / BepInEx** — modding framework.
- Console asset bundles and icons belong to their respective creators.

## Disclaimer

This project is **not** affiliated with, endorsed by, or sponsored by Gorilla Tag or Another Axiom
LLC. Portions of the materials contained herein are property of Another Axiom LLC.
© 2026 Another Axiom LLC. Use at your own risk — modding can get your account banned.
