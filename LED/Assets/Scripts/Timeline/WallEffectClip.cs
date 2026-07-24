// Assets/Scripts/Timeline/WallEffectClip.cs
//
// Un clip d'effet procedural pose sur la timeline. Contrairement a
// EntityColorClip (une couleur fixe sur une plage d'entites), ici la couleur
// depend de la position (colonne, ligne) ET du temps ecoule dans le clip.
//
// Tous les champs ci-dessous sont editables dans l'Inspector quand on
// selectionne le clip dans la fenetre Timeline. On peut donc composer un
// spectacle entier a la souris, sans toucher au code.

using UnityEngine;
using UnityEngine.Playables;

/// <summary>Type d'effet joue par le clip.</summary>
public enum WallEffectKind
{
    SolidColor,     // couleur unie (utile comme fond ou pour un noir)
    ScrollingText,  // texte defilant facon sol LED du jury show
    Flag,           // drapeau tricolore qui ondule
    Pulse,          // flash rythmique sur le tempo
    Drawers,        // mur de tiroirs de classeurs facon decor de scene
    DancersTrail,   // silhouettes sur dalles lumineuses, qui impriment un mot
    SequenceA3,     // sequence complete (casiers + feu d'artifice) — historique
    Casiers,        // seulement le mur de casiers 4x3
    FeuArtifice,    // seulement fusee -> explosion -> HETIC
    LiquidChrome,   // mercure liquide / chrome reflechissant
    SonicRings,     // ondes de choc synchronisees au kick (audio-reactif)
    HeticLogoBuild, // formes du symbole H qui s'ajoutent + typing HETIC
}

/// <summary>Ordre dans lequel les tiroirs s'allument.</summary>
public enum DrawerFillMode
{
    Sequential,   // dans l'ordre de lecture, un par temps — mecanique, l'ordre etabli
    Random,       // un tiroir au hasard par temps — deja moins previsible
    Wave,         // vague diagonale qui traverse le mur
    Contagion,    // un tiroir se desynchronise, puis contamine ses voisins
}

public class WallEffectClip : PlayableAsset
{
    [Header("Effet")]
    public WallEffectKind kind = WallEffectKind.ScrollingText;

    [Header("Couleurs")]
    [Tooltip("Couleur principale : texte, pulse, couleur unie.")]
    public Color primaryColor = new Color(0.90f, 0.76f, 0.16f); // jaune Paloma

    [Tooltip("Couleur de fond derriere le texte.")]
    public Color backgroundColor = new Color(0.03f, 0.03f, 0.04f);

    [Header("Drapeau (bandes de haut en bas)")]
    public Color flagTop = new Color(0.851f, 0f, 0.071f);    // rouge armenien
    public Color flagMiddle = new Color(0f, 0.200f, 0.627f); // bleu
    public Color flagBottom = new Color(0.949f, 0.659f, 0f); // abricot

    [Header("Texte defilant")]
    public string text = "PALOMA RUMBA";

    [Tooltip("Agrandissement de la police : 3 donne des lettres de 15 LED de haut.")]
    [Min(1)] public int textScale = 3;

    [Tooltip("Vitesse de defilement en LED par seconde.")]
    public float scrollSpeed = 18f;

    [Tooltip("Inverse le sens de defilement une ligne sur deux.")]
    public bool alternateDirection = true;

    [Header("Rythme")]
    [Tooltip("Tempo utilise par Pulse et par l'ondulation du drapeau.")]
    [Min(1f)] public float bpm = 120f;

    [Header("Ondulation (drapeau)")]
    public float waveAmplitude = 11f;
    public float waveSpeed = 3f;

    [Header("Tiroirs")]
    [Min(1)] public int drawerColumns = 5;
    [Min(1)] public int drawerRows = 4;

    [Tooltip("Epaisseur du joint entre deux tiroirs, en LED.")]
    [Min(0)] public int drawerGap = 2;

    public DrawerFillMode fillMode = DrawerFillMode.Sequential;

    [Tooltip("Teinte des tiroirs eteints.")]
    public Color drawerDim = new Color(0.10f, 0.10f, 0.11f);

    [Tooltip("Teinte des tiroirs allumes.")]
    public Color drawerLit = new Color(0.62f, 0.63f, 0.66f);

    [Tooltip("Couleur de l'etiquette au centre de chaque tiroir.")]
    public Color labelColor = new Color(0.95f, 0.95f, 0.92f);

    [Tooltip("Colonne entierement sombre, comme dans le decor. -1 pour aucune.")]
    public int darkColumnIndex = 2;

    [Tooltip("Tiroir tire vers l'exterieur : il deborde sur ses voisins. -1 pour aucun.")]
    public int openDrawerColumn = 4;
    public int openDrawerRow = 0;

    [Tooltip("Vitesse de propagation en mode Contagion, en tiroirs par seconde.")]
    public float contagionSpeed = 1.5f;

    [Header("Danseurs sur dalles")]
    [Tooltip("Mot imprime par la trainee de dalles. Court de preference : 5 a 6 lettres.")]
    public string trailWord = "RUMBA";

    [Min(1)] public int dancerCount = 4;

    [Tooltip("Agrandissement des silhouettes. 2 donne des figures de 22 x 38 LED.")]
    [Min(1)] public int spriteScale = 2;

    [Tooltip("Cote d'une dalle en LED.")]
    [Min(2)] public int tileSize = 6;

    [Tooltip("Espace sombre entre deux dalles, en LED.")]
    [Min(0)] public int tileGap = 1;

    [Tooltip("Hauteur a laquelle repose la premiere rangee de dalles.")]
    public int trailBaseline = 30;

    [Tooltip("Nombre de dalles allumees par seconde.")]
    public float fillRate = 2.5f;

    [Tooltip("Hauteur de la zone de reflet, en bas du mur. 0 pour desactiver.")]
    [Min(0)] public int reflectionHeight = 26;

    [Range(0f, 1f)] public float reflectionStrength = 0.35f;

    public Color tileColor = new Color(0.95f, 0.95f, 0.93f);
    public Color dancerColor = new Color(0.55f, 0.55f, 0.58f);

    [Tooltip("Un danseur porte la couleur d'accent. -1 pour aucun.")]
    public int accentDancerIndex = 3;

    // -----------------------------------------------------------------------
    // Sequence A3 — 7 secondes, quatre phases enchainees.
    //   0-2s  casiers aleatoires aux couleurs armeniennes
    //   2-4s  zoom vers le centre, les casiers peripheriques s'eteignent
    //   4-5s  flash blanc total
    //   5-7s  silhouette qui danse, le texte s'affiche dans son corps
    // -----------------------------------------------------------------------
    [Header("Sequence A3 — bornes des phases (secondes)")]
    public float phaseLockersEnd = 2f;
    public float phaseZoomEnd = 4f;
    public float phaseFlashEnd = 5f;

    [Header("Sequence A3 — casiers (phase 0-2s)")]
    [Tooltip("Largeur d'un casier en LED. Les casiers sont plus larges que hauts.")]
    [Min(2)] public int lockerWidth = 26;

    [Tooltip("Hauteur d'un casier en LED.")]
    [Min(2)] public int lockerHeight = 22;

    [Tooltip("Joint noir entre les casiers, en LED. Epais, comme le decor.")]
    [Min(0)] public int lockerGap = 3;

    [Tooltip("Anthracite des casiers sombres.")]
    public Color lockerDark = new Color(0.11f, 0.11f, 0.12f);

    [Tooltip("Creme des casiers clairs.")]
    public Color lockerBright = new Color(0.93f, 0.91f, 0.86f);

    [Header("Sequence A3 — spots au sol (phase 4-5s)")]
    [Tooltip("Nombre de faisceaux : un par lyre de l'installation.")]
    [Min(1)] public int floorSpotCount = 5;

    public Color floorSpotColor = new Color(0.92f, 0.94f, 1f);

    [Header("Sequence A3 — phase finale (5-7s)")]
    [Tooltip("Revelation du mot par une barre de lumiere qui balaie l'ecran (recommande).")]
    public bool useScanner = true;

    [Tooltip("Duree du balayage du scanner, en secondes.")]
    public float scannerDuration = 1.2f;

    [Tooltip("Duree du balayage du drapeau armenien, en secondes.")]
    public float flagSweepDuration = 2.6f;

    [Tooltip("Lettres HETIC en silhouette (creux sombre) plutot que blanches.")]
    public bool silhouetteLetters = true;

    [Tooltip("Coche = nuage de particules. (Ignore si le scanner est actif.)")]
    public bool useParticles = false;

    [Tooltip("Hauteur de la figure/du mot, en fraction de la hauteur disponible.")]
    [Range(0.3f, 1f)] public float figureHeightRatio = 0.78f;

    public Color figureColor = new Color(0.93f, 0.93f, 0.95f);

    [Tooltip("Duree du chaos avant que les particules convergent (secondes).")]
    public float particleChaos = 1.0f;

    [Tooltip("Duree de la convergence des particules (secondes).")]
    public float particleConverge = 0.7f;

    [Tooltip("Agrandissement du mot forme par les particules.")]
    [Min(1)] public int particleTextScale = 5;

    [Tooltip("Hauteur de la zone de reflet au sol, en fraction du mur.")]
    [Range(0f, 0.4f)] public float mirrorHeightRatio = 0.18f;

    [Range(0f, 1f)] public float mirrorStrength = 0.35f;

    [Header("Sequence A3 — mot")]
    public string bodyText = "HETIC";

    [Tooltip("Agrandissement du mot. 2 donne des lettres de 10 LED de haut.")]
    [Min(1)] public int bodyTextScale = 2;

    [Tooltip("Hauteur du mot, en fraction du mur.")]
    [Range(0f, 1f)] public float wordHeightRatio = 0.86f;

    public Color wordColor = Color.white;

    [Header("Ondes de choc (audio-reactif)")]
    [Tooltip("Vitesse de propagation des ondes, en LED par seconde.")]
    public float ringSpeed = 78f;

    [Tooltip("Couleur froide du noyau et des ondes.")]
    public Color ringCore = new Color(0.55f, 0.80f, 1f);

    [Tooltip("Teinte du flash sur les gros coups.")]
    public Color ringAccent = new Color(1f, 0.75f, 0.30f);

    [Header("Mercure liquide / chrome")]
    [Tooltip("Seuil de surface du metal. Plus bas = le metal envahit le mur.")]
    [Range(1f, 12f)] public float chromeSurface = 4.5f;

    [Tooltip("Teinte du metal. Blanc = chrome pur ; bleute = acier froid ; dore = laiton.")]
    public Color chromeTint = new Color(0.75f, 0.82f, 0.95f);

    [Header("HETIC Logo Build")]
    [Tooltip("Delai entre chaque forme du symbole H.")]
    [Min(0.05f)] public float heticShapeInterval = 0.18f;

    [Tooltip("Pause apres la derniere forme, avant le typing.")]
    [Min(0f)] public float heticHoldBeforeType = 0.2f;

    [Tooltip("Delai entre chaque lettre du mot HETIC.")]
    [Min(0.05f)] public float heticTypeInterval = 0.12f;

    [Tooltip("Couleur des formes et du texte.")]
    public Color heticLogoColor = Color.white;

    [Tooltip("Ignore (texte = glyphes du logo). Conserve pour compat Timeline.")]
    [Min(1)] public int heticTextScale = 3;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<WallEffectBehaviour>.Create(graph);
        FillBehaviour(playable.GetBehaviour());
        return playable;
    }

    /// <summary>Remplit un behaviour (Playable + Preview Edit Mode).</summary>
    public void FillBehaviour(WallEffectBehaviour behaviour)
    {
        if (behaviour == null) return;

        behaviour.kind = kind;
        behaviour.primaryColor = primaryColor;
        behaviour.backgroundColor = backgroundColor;
        behaviour.flagTop = flagTop;
        behaviour.flagMiddle = flagMiddle;
        behaviour.flagBottom = flagBottom;
        behaviour.textScale = Mathf.Max(1, textScale);
        behaviour.scrollSpeed = scrollSpeed;
        behaviour.alternateDirection = alternateDirection;
        behaviour.bpm = Mathf.Max(1f, bpm);
        behaviour.waveAmplitude = waveAmplitude;
        behaviour.waveSpeed = waveSpeed;

        behaviour.drawerColumns = Mathf.Max(1, drawerColumns);
        behaviour.drawerRows = Mathf.Max(1, drawerRows);
        behaviour.drawerGap = Mathf.Max(0, drawerGap);
        behaviour.fillMode = fillMode;
        behaviour.drawerDim = drawerDim;
        behaviour.drawerLit = drawerLit;
        behaviour.labelColor = labelColor;
        behaviour.darkColumnIndex = darkColumnIndex;
        behaviour.openDrawerColumn = openDrawerColumn;
        behaviour.openDrawerRow = openDrawerRow;
        behaviour.contagionSpeed = contagionSpeed;

        behaviour.dancerCount = Mathf.Max(1, dancerCount);
        behaviour.spriteScale = Mathf.Max(1, spriteScale);
        behaviour.tileSize = Mathf.Max(2, tileSize);
        behaviour.tileGap = Mathf.Max(0, tileGap);
        behaviour.trailBaseline = trailBaseline;
        behaviour.fillRate = fillRate;
        behaviour.reflectionHeight = Mathf.Max(0, reflectionHeight);
        behaviour.reflectionStrength = reflectionStrength;
        behaviour.tileColor = tileColor;
        behaviour.dancerColor = dancerColor;
        behaviour.accentDancerIndex = accentDancerIndex;

        behaviour.phaseLockersEnd = phaseLockersEnd;
        behaviour.phaseZoomEnd = phaseZoomEnd;
        behaviour.phaseFlashEnd = phaseFlashEnd;

        behaviour.lockerWidth = Mathf.Max(2, lockerWidth);
        behaviour.lockerHeight = Mathf.Max(2, lockerHeight);
        behaviour.lockerGap = Mathf.Max(0, lockerGap);
        behaviour.lockerDark = lockerDark;
        behaviour.lockerBright = lockerBright;

        behaviour.floorSpotCount = Mathf.Max(1, floorSpotCount);
        behaviour.floorSpotColor = floorSpotColor;

        behaviour.useScanner = useScanner;
        behaviour.scannerDuration = Mathf.Max(0.2f, scannerDuration);
        behaviour.flagSweepDuration = Mathf.Max(0.3f, flagSweepDuration);
        behaviour.silhouetteLetters = silhouetteLetters;
        behaviour.useParticles = useParticles;
        behaviour.figureHeightRatio = figureHeightRatio;
        behaviour.figureColor = figureColor;
        behaviour.particleChaos = particleChaos;
        behaviour.particleConverge = particleConverge;
        behaviour.particleTextScale = Mathf.Max(1, particleTextScale);
        behaviour.mirrorHeightRatio = mirrorHeightRatio;
        behaviour.mirrorStrength = mirrorStrength;

        behaviour.bodyTextScale = Mathf.Max(1, bodyTextScale);
        behaviour.wordHeightRatio = wordHeightRatio;
        behaviour.wordColor = wordColor;

        behaviour.figurePhaseDuration = Mathf.Max(0.5f, 7f - phaseFlashEnd);

        behaviour.ringSpeed = ringSpeed;
        behaviour.ringCore = ringCore;
        behaviour.ringAccent = ringAccent;

        behaviour.chromeSurface = chromeSurface;
        behaviour.chromeTint = chromeTint;

        behaviour.heticShapeInterval = Mathf.Max(0.05f, heticShapeInterval);
        behaviour.heticHoldBeforeType = Mathf.Max(0f, heticHoldBeforeType);
        behaviour.heticTypeInterval = Mathf.Max(0.05f, heticTypeInterval);
        behaviour.heticLogoColor = heticLogoColor;
        behaviour.heticTextScale = Mathf.Max(1, heticTextScale);

        behaviour.bodyTextBand = PixelFont.BuildBand(bodyText);
        behaviour.bodyTextLetters = string.IsNullOrEmpty(bodyText) ? 1 : bodyText.Length;

        bool[][] trailBand = PixelFont.BuildBand(trailWord);
        DancerSprites.BuildTrail(
            trailBand,
            out behaviour.trailX,
            out behaviour.trailY,
            out behaviour.trailOrder,
            out behaviour.trailCols,
            out behaviour.trailRows);

        behaviour.textBand = PixelFont.BuildBand(text);
    }

    /// <summary>Applique le clip sur le mur (Preview Timeline Edit Mode).</summary>
    public void ApplyToWall(LedWallVisualizer wall, float localTime)
    {
        if (wall == null || !wall.IsBuilt || wall.EntityManager == null) return;
        if (!WallMapping.IsInitialized) return;

        var behaviour = new WallEffectBehaviour();
        FillBehaviour(behaviour);

        var entityManager = wall.EntityManager;
        int cols = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;
        var pixels = new Color[cols * rows];

        wall.SetSuppressSingleUpdates(true);
        for (int row = 0; row < rows; row++)
        {
            int texRow = rows - 1 - row;
            for (int col = 0; col < cols; col++)
            {
                Color c = behaviour.Evaluate(col, row, cols, rows, localTime);
                pixels[texRow * cols + col] = c;
                int? id = WallMapping.EntityIdForCell(row, col);
                if (!id.HasValue) continue;
                entityManager.SetColorSilent(
                    id.Value,
                    (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255));
            }
        }
        wall.ApplyDisplayPixels(pixels);
    }
}
