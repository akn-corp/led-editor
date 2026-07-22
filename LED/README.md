# Logiciel Animation — Projet Son & Lumière

Outil de création et de composition de spectacles lumineux : modélise l'installation
physique en 3D, permet de composer une animation dans le temps synchronisée à la
musique, et envoie l'état résultant (couleur de chaque LED) au logiciel de routage.

> Ce repo ne pilote **jamais** directement le matériel (pas d'ArtNet/DMX ici). Il ne
> connaît que des entités abstraites. La traduction vers le matériel est faite par le
> logiciel de routage (repo séparé) — voir `docs/contrat-interface-animation-routage.md`.

---

## Structure du projet

```
Assets/
├── Scripts/
│   ├── Entities/          # Dev 1 — EntityManager, ColorState
│   │   ├── EntityManager.cs
│   │   └── ColorState.cs
│   ├── Installation/       # Dev 1 — chargement de la géométrie/config
│   │   ├── InstallationLoader.cs
│   │   └── SceneBuilder.cs
│   ├── Timeline/            # Dev 2 — composition de l'animation
│   │   ├── TimelineController.cs
│   │   └── ClipManager.cs
│   ├── Audio/               # Dev 2 — synchronisation musicale
│   │   └── AudioSync.cs
│   └── Network/             # Dev 2 — communication avec le routage
│       ├── StateExporter.cs      # canal État (UDP, ~40Hz)
│       └── CommandClient.cs      # canal Commandes (TCP)
├── Scenes/
│   └── Main.unity           # ⚠️ à créer/modifier uniquement depuis Unity, jamais à la main
├── Configs/                 # configuration mur Glassworks
│   └── wall-bands.json
├── Prefabs/                 # prefabs des entités/illuminateurs
└── Audio/                   # fichiers audio de test pour la synchronisation

docs/
└── contrat-interface-animation-routage.md   # référence du protocole avec le routage
```

### Qui possède quoi

| Dossier | Responsable | Contenu |
|---|---|---|
| `Scripts/Entities/`, `Scripts/Installation/` | **Dev 1** | Modèle d'entité,chargement de la géométrie 2D ou 3D |
| `Scripts/Timeline/`, `Scripts/Audio/`, `Scripts/Network/` | **Dev 2** | Composition temporelle, sync audio, export réseau |

Cette séparation par dossier limite les conflits de merge : chacun travaille
principalement dans son périmètre.

---

## Le concept central : l'entité

Une entité = une LED virtuelle, identifiée par un entier unique. Elle contient une
position 3D (pour l'affichage) et une couleur courante (R, G, B).

**Règle absolue : une entité ne contient jamais d'IP, d'univers ou de canal DMX.**
Ces informations n'existent que côté logiciel de routage.

---

## Les deux canaux de communication vers le routage

| Canal | Protocole | Fréquence | Contenu |
|---|---|---|---|
| État | UDP **6455** | ~40x/seconde | Couleur de chaque entité (LEDS) |
| Config | HTTP **6456** | À la demande | `wall-bands` du profil Hub actif |
| Commandes | TCP | Rare | `START_SHOW`, `STOP_SHOW`, … |

Détail complet des formats de message : voir `docs/contrat-interface-animation-routage.md`.

### Sync mapping mur (Hub)

Au Play, `InstallationLoader` interroge `GET /api/wall-bands` sur le Hub (HTTP :6456),
puis fallback cache local / `Resources/Configs/wall-bands.json`.
Voir [`Docs/hub-config-sync.md`](Docs/hub-config-sync.md). Menu éditeur : **LED > Sync Wall Bands from Hub**.

---

## Tester sans matériel physique

1. **Simulateur Unity fourni par le cours** : visualise le mur LED (ou autre forme) en
   3D à partir du flux réseau émis. Utiliser en priorité, disponible en continu.
2. **Routage en stub minimal** : demander à l'équipe routage un mapping basique
   (quelques entités → un univers) pour un premier test d'intégration bout-en-bout,
   avant que le moteur de routage complet soit terminé.
3. **Matériel réel** : uniquement pendant les créneaux réservés (accès partagé entre
   groupes). Vérifier avant la session : même sous-réseau que les contrôleurs BC216,
   pare-feu autorisant l'UDP sortant sur le port 6454 (ArtNet).

---

## Workflow de branches

- `main` : branche protégée, toujours stable.
- `dev1-entities` : travail de Dev 1.
- `dev2-timeline` : travail de Dev 2.

---
## chaîne complète

-  Animation (état) → Routage (traduit en ArtNet) → BC216 → LED physique