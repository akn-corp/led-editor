# Devices Timeline (DEVS) — lyres + projecteur

## Preview Timeline = Play Unity

Tu peux lancer le spectacle avec **Play / Espace dans la fenêtre Timeline** (sans bouton Play Unity) :

1. Ouvre la Timeline sur `Director`
2. Clique Play (ou Espace) dans la Timeline
3. Le mur + projecteur + lyres se mettent à jour comme en Play Mode

`TimelineEditPreviewDriver` construit le mur en Edit Mode et le mixer Device rafraîchit les faisceaux à chaque frame Timeline.

Note : l’envoi UDP `LEDS`/`DEVS` vers le hub ne part qu’en **Play Mode** Unity (`StateExporter`) — la Preview Edit est visuelle.

## Preview Unity

Au Play, `SceneBuilder` crée un groupe **DevicesPreview** autour du mur :
- `Projector_RGBW` (bas) — couleur RGBW / dimmer
- `Lyre_1` … `Lyre_4` — rotation pan/tilt + intensité

Ce n’est qu’une **preview locale** : le matériel réel est piloté via UDP `DEVS` → hub → ArtNet.

## Usage rapide

1. `SceneBuilder.animationMode = None`
2. Timeline → **Add** → **Device Track**
3. Binding : `DeviceManager` (auto via `EntityTextTrackBinder` au Play)
4. Add **Device Clip** :
   - `deviceId` `0` = projecteur RGBW ; `1`–`4` = lyres
   - `autoSweep` = lyres tournantes (LFO pan/tilt)
   - `dimmer` / `shutter` (shutter open ≈ 40)
5. `StateExporter.sendDevices = true` → UDP `DEVS` après chunks `LEDS` @ 40 Hz

## Contrat P4

L’authoring n’envoie que l’état logique (`deviceId` + champs). IP / univers / DMX restent dans `led-routing-hub` (`mur-led.json`).

**Hub — point important :** pour `deviceId 0` (projecteur), le hub n’écrit que **R/G/B/W** (univ 33 ch 1–4). Un blink qui ne touche que `dimmer` est invisible sur le matériel. `DeviceShow` coupe aussi RGBW à l’OFF ; le hub multiplie aussi RGBW × dimmer.

## Test

1. Hub : `npm start` puis `node tools/faker.js` (baseline matériel)
2. Unity Play Main → logs `[StateExporter] DEVS …`
3. Hub reçoit magic `DEVS`, route ArtNet univ 33
4. Attendu : **projecteur clignote**, **lyres balayent** (pas l’inverse)
