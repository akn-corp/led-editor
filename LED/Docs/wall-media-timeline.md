# Wall Media (GIF / vidéo) sur Timeline

## Usage

1. Menu **LED > Import GIF or Video as Wall Media…** (ou **Import Samus 8-Bit GIF**)
2. Ouvre ta Timeline (ex. `TestTimeline.playable`)
3. **Add** → **Wall Media Track**
4. Binder la track sur **LedWall** (`LedWallVisualizer`) — auto via TimelineBinder au Play
5. Clic droit sur la track → **Add Wall Media Clip**
6. Inspector du clip → **Sequence** = `Assets/Media/Samus8Bit/Samus8Bit_Sequence.asset`
7. Place la tête de lecture **dans** le clip, puis Play Mode

Si le mur reste noir : vérifie que la Sequence n’est pas vide, que le clip n’est pas mute, et qu’aucune piste Fluid/Paloma active ne le recouvre au même temps.

## Samus (déjà importé)

- Source : `Assets/Media/Samus8Bit/source.gif`
- 8 frames 128×128 @ 12 fps → `Samus8Bit_Sequence.asset`

## Notes

- Frames extraites en **128×128** (GIF : nearest-neighbor ; vidéo : bilinear)
- `ffmpeg` requis (`brew install ffmpeg`)
- Formats : `.gif`, `.mp4`, `.mov`, `.webm`
- Ajuste `framesPerSecond` / `brightness` sur le clip ou la Sequence

## Fichiers

- `Scripts/Media/WallMediaSequence.cs`
- `Scripts/Timeline/WallMediaClip.cs` / `WallMediaBehaviour.cs` / `WallMediaTrack.cs`
- `Scripts/Editor/GifToWallMediaImporter.cs`
