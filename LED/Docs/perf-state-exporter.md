# Perf StateExporter (Unity) — LEDS/DEVS zéro-alloc

Branche : avec le travail Timeline / devices.

## Objectif

Réduire GC et dérive d’horloge à **40 Hz full wall** (~128 chunks LEDS) **sans** changer le contrat UDP.

## Avant → Après

| Zone | Avant | Après |
|------|--------|--------|
| Encode LEDS | `new Rgb[n]` + `new byte[]` × chunks / frame | `LedFrameWriter` : buffers préalloués, RGB écrit in-place |
| Encode DEVS | `SnapshotAll()` + `new byte[]` / frame | buffer DEVS réutilisé, lecture directe `GetDevice` |
| Horloge | `_timer = 0` dans `Update` (dérive + drop) | `realtimeSinceStartup` + accumulateur, catch-up borné |

## API

- Hot path : `LedFrameWriter.EncodeLeds` / `EncodeDevs` via `StateExporter`
- Compat tests : `StateProtocol.EncodeLedFrame` / `EncodeLedsChunk` (allouent encore)

## Hors scope (contrat multi-repos)

1. Protocole **delta** LEDS (seulement LEDs dirty)
2. Envoi **viewport** preview vs full wall show
