# Sync config Routing Hub (Unity)

Branche : `feature/hub-config-sync`

## Principe

Comme le Studio Electron : Unity **n’édite pas** le mapping DMX. Il récupère `wall-bands` du profil actif Hub.

| Canal | Port | Usage |
|-------|------|--------|
| State | UDP **6455** | `StateExporter` → LEDS |
| Config | HTTP **6456** | `HubConfigClient` → `/api/wall-bands` |

## Ordre de chargement (Play)

1. `GET http://127.0.0.1:6456/api/wall-bands` (si sync activé sur `InstallationLoader`)
2. Sinon cache `Application.persistentDataPath/wall-bands-cache.json`
3. Sinon `Resources/Configs/wall-bands.json`

## Tester

1. Hub : `cd led-routing-hub && npm start` (API :6456)
2. Unity : Play sur `Main.unity`
3. Console : `[InstallationLoader] Sync Hub OK — 128 colonnes…`
4. Menu **LED > Sync Wall Bands from Hub** (éditeur, hors Play)

Inspector `InstallationLoader` :
- `Config Base Url` (défaut `http://127.0.0.1:6456`)
- `Sync From Hub On Start`

## Fichiers

- `Scripts/Network/HubConfigClient.cs`
- `Scripts/Network/WallBandsCache.cs`
- `Scripts/Network/WallBandsValidator.cs`
- `Scripts/Installation/InstallationLoader.cs`
- `Scripts/Editor/HubConfigSyncMenu.cs`
