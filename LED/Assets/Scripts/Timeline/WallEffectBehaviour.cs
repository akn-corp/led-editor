// Assets/Scripts/Timeline/WallEffectBehaviour.cs
//
// Donnees d'un clip WallEffect pendant qu'il est joue, et calcul de la couleur
// d'une cellule du mur a un instant donne.
//
// Evaluate() est la seule methode que le mixer appelle : elle recoit une
// position dans la grille et le temps ecoule depuis le debut du clip, et
// retourne la couleur a afficher. C'est le meme principe que la fonction
// set(x, y, r, g, b) du simulateur HTML.

using UnityEngine;
using UnityEngine.Playables;

public class WallEffectBehaviour : PlayableBehaviour
{
    public WallEffectKind kind;

    public Color primaryColor;
    public Color backgroundColor;
    public Color flagTop;
    public Color flagMiddle;
    public Color flagBottom;

    public int textScale = 3;
    public float scrollSpeed = 18f;
    public bool alternateDirection = true;
    public float bpm = 120f;
    public float waveAmplitude = 11f;
    public float waveSpeed = 3f;

    public int drawerColumns = 5;
    public int drawerRows = 4;
    public int drawerGap = 2;
    public DrawerFillMode fillMode = DrawerFillMode.Sequential;
    public Color drawerDim;
    public Color drawerLit;
    public Color labelColor;
    public int darkColumnIndex = 2;
    public int openDrawerColumn = 4;
    public int openDrawerRow = 0;
    public float contagionSpeed = 1.5f;

    public int dancerCount = 4;
    public int spriteScale = 2;
    public int tileSize = 6;
    public int tileGap = 1;
    public int trailBaseline = 30;
    public float fillRate = 2.5f;
    public int reflectionHeight = 26;
    public float reflectionStrength = 0.35f;
    public Color tileColor;
    public Color dancerColor;
    public int accentDancerIndex = 3;

    public float phaseLockersEnd = 2f;
    public float phaseZoomEnd = 4f;
    public float phaseFlashEnd = 5f;
    public int lockerWidth = 8;
    public int lockerHeight = 16;
    public int lockerGap = 1;
    public Color lockerDark;
    public Color lockerBright;

    public int floorSpotCount = 5;
    public Color floorSpotColor;

    public float figureHeightRatio = 0.78f;
    public float mirrorHeightRatio = 0.18f;
    public float mirrorStrength = 0.35f;
    public float wordHeightRatio = 0.86f;
    public float figurePhaseDuration = 2f;
    public Color figureColor;
    public Color wordColor;

    public int bodyTextScale = 2;
    public bool[][] bodyTextBand;
    public int bodyTextLetters = 1;

    public float chromeSurface = 4.5f;
    public Color chromeTint = new Color(0.75f, 0.82f, 0.95f);
    private LiquidChromeField _chrome;

    public float ringSpeed = 78f;
    public Color ringCore = new Color(0.55f, 0.80f, 1f);
    public Color ringAccent = new Color(1f, 0.75f, 0.30f);
    private SonicRingField _rings;
    private AudioReactive _audio;

    public bool useScanner = true;
    public float scannerDuration = 1.2f;
    public float flagSweepDuration = 2.6f;
    public bool silhouetteLetters = true;
    public bool useParticles = false;
    public float particleChaos = 1.0f;
    public float particleConverge = 0.7f;
    public int particleTextScale = 5;
    private ParticleField _particles;

    // Temps ecoule dans la phase silhouette, partage avec FigureAndText pour
    // le micro-mouvement (evite de le passer en parametre partout).
    private float _figurePhaseTime;

    // Parcours de dalles pre-calcule par le clip.
    public int[] trailX;
    public int[] trailY;
    public int[][] trailOrder;
    public int trailCols;
    public int trailRows;

    public bool[][] textBand;

    /// <summary>
    /// Couleur de la cellule (column, row) a l'instant localTime secondes
    /// apres le debut du clip. row = 0 correspond au bas du mur.
    /// </summary>
    public Color Evaluate(int column, int row, int columns, int rows, float localTime)
    {
        switch (kind)
        {
            case WallEffectKind.SolidColor:
                return primaryColor;

            case WallEffectKind.ScrollingText:
                return EvaluateScrollingText(column, row, localTime);

            case WallEffectKind.Flag:
                return EvaluateFlag(column, row, rows, localTime);

            case WallEffectKind.Pulse:
                return EvaluatePulse(column, row, localTime);

            case WallEffectKind.Drawers:
                return EvaluateDrawers(column, row, columns, rows, localTime);

            case WallEffectKind.DancersTrail:
                return EvaluateDancersTrail(column, row, columns, rows, localTime);

            case WallEffectKind.SequenceA3:
                return EvaluateSequenceA3(column, row, columns, rows, localTime);

            case WallEffectKind.LiquidChrome:
                return EvaluateLiquidChrome(column, row, columns, rows, localTime);

            case WallEffectKind.SonicRings:
                return EvaluateSonicRings(column, row, columns, rows, localTime);

            default:
                return Color.black;
        }
    }

    private Color EvaluateScrollingText(int column, int row, float localTime)
    {
        if (textBand == null || textBand.Length == 0 || textBand[0].Length == 0)
            return backgroundColor;

        int scale = Mathf.Max(1, textScale);
        int bandHeight = (PixelFont.GlyphHeight + 1) * scale; // 5 lignes de texte + interligne
        int bandWidth = textBand[0].Length;

        int lineIndex = row / bandHeight;          // quelle rangee de texte
        int withinLine = row % bandHeight;
        int glyphRow = withinLine / scale;

        if (glyphRow >= PixelFont.GlyphHeight)
            return backgroundColor;                // zone d'interligne

        // Le texte est stocke du haut vers le bas, mais row = 0 est en bas du mur.
        int sourceRow = PixelFont.GlyphHeight - 1 - glyphRow;

        int direction = (alternateDirection && lineIndex % 2 != 0) ? -1 : 1;
        int scroll = Mathf.RoundToInt(localTime * scrollSpeed) * direction;

        int bandColumn = Mathf.FloorToInt((column + scroll) / (float)scale);
        bandColumn = ((bandColumn % bandWidth) + bandWidth) % bandWidth; // modulo positif

        return textBand[sourceRow][bandColumn] ? primaryColor : backgroundColor;
    }

    private Color EvaluateFlag(int column, int row, int rows, float localTime)
    {
        // Ondulation : la bande se decale verticalement, la vague se propage
        // horizontalement le long du mur.
        float wave = Mathf.Sin(column * 0.11f - localTime * waveSpeed) * waveAmplitude;

        // Scintillement type tissu eclaire, legerement cale sur le tempo.
        float beat = localTime * bpm / 60f;
        float shade = 0.72f + 0.28f * Mathf.Sin(column * 0.17f - localTime * 4f + Mathf.Sin(beat * Mathf.PI));

        // row = 0 en bas : on inverse pour que flagTop soit bien en haut.
        float fromTop = (rows - 1 - row) + wave;
        int band = Mathf.Clamp(Mathf.FloorToInt(fromTop / rows * 3f), 0, 2);

        Color color = band == 0 ? flagTop : (band == 1 ? flagMiddle : flagBottom);
        return color * shade;
    }

    /// <summary>
    /// Ondes de choc audio-reactives. La couleur est calculee avec un decalage
    /// RVB sur les kicks (aberration chromatique) : les canaux rouge et bleu
    /// sont echantillonnes de part et d'autre, ce qui claque a l'impact.
    /// </summary>
    private Color EvaluateSonicRings(int column, int row, int columns, int rows, float localTime)
    {
        if (_rings == null)
        {
            _rings = new SonicRingField();
            _audio = AudioReactive.GetOrCreate();
        }

        _rings.Simulate(localTime, _audio);

        int off = Mathf.RoundToInt(_rings.ChromaticOffset());
        if (off <= 0)
            return _rings.Shade(column, row, columns, rows, ringSpeed, ringCore, ringAccent);

        // Split chromatique sur les impacts.
        Color r = _rings.Shade(column - off, row, columns, rows, ringSpeed, ringCore, ringAccent);
        Color g = _rings.Shade(column, row, columns, rows, ringSpeed, ringCore, ringAccent);
        Color b = _rings.Shade(column + off, row, columns, rows, ringSpeed, ringCore, ringAccent);
        return new Color(r.r, g.g, b.b);
    }

    /// <summary>
    /// Mercure liquide : gouttes de chrome qui coulent et fusionnent, avec
    /// reflets brillants. Effet en boucle, dure aussi longtemps que le clip.
    /// </summary>
    private Color EvaluateLiquidChrome(int column, int row, int columns, int rows, float localTime)
    {
        if (_chrome == null || !_chrome.Matches(columns, rows))
        {
            _chrome = new LiquidChromeField();
            _chrome.Build(columns, rows, 20240722);
        }

        _chrome.Simulate(localTime);
        return _chrome.Shade(column, row, chromeSurface, chromeTint);
    }

    // ---------------- Audio-reactivite ----------------
    // Grandeurs du son, rafraichies une fois par trame. En mode Play elles
    // viennent du vrai mix (AudioReactive) ; dans l'editeur, faute de son, on
    // les synthetise a partir du BPM pour que l'apercu reste vivant.
    private float _auLevel = 0.6f, _auBass, _auMid, _auHigh, _auBeat;
    private int _auBeatIndex;
    private bool _auLive;
    private float _auFetchTime = float.NaN;

    private void FetchAudio(float time)
    {
        if (time == _auFetchTime) return;
        _auFetchTime = time;

        var a = AudioReactive.Instance;
        if (a != null && (a.Level > 0.02f || a.BeatCount > 0))
        {
            _auLive = true;
            _auLevel = a.Level; _auBass = a.Bass; _auMid = a.Mid;
            _auHigh = a.High; _auBeat = a.Beat; _auBeatIndex = a.BeatCount;
        }
        else
        {
            // Synthese pour l'editeur : pulsation reguliere sur le BPM.
            _auLive = false;
            float beat = time * bpm / 60f;
            _auBeatIndex = Mathf.FloorToInt(beat);
            float frac = beat - _auBeatIndex;
            _auBeat = Mathf.Max(0f, 1f - frac * 2.2f);
            _auLevel = 0.6f; _auBass = _auBeat; _auMid = 0.4f;
            _auHigh = 0.3f + 0.3f * Mathf.Abs(Mathf.Sin(time * 13f));
        }
    }

    // =======================================================================
    // Sequence A3 — 7 secondes
    //   0-2s  casiers rectangulaires, gris fonce / blanc, sur fond noir
    //   2-4s  une case centrale grossit lentement jusqu'a remplir le mur
    //   4-5s  on est dedans : noir, les spots montent du sol
    //   5-7s  silhouette acrobatique dont les poses dessinent H E T I C
    // =======================================================================

    private Color EvaluateSequenceA3(int column, int row, int columns, int rows, float localTime)
    {
        // Le mur est indexe avec la ligne 0 EN HAUT (c'est la convention du
        // LedWallVisualizer, qui inverse ensuite pour la texture). Toute la
        // sequence raisonne au contraire avec le bas du mur en zero : sol,
        // reflet, texte a l'endroit. On convertit donc une fois pour toutes.
        FetchAudio(localTime);
        int y = rows - 1 - row;

        // Rendu propre, sans modulation audio globale (version validee des casiers).
        return SequenceA3Raw(column, y, columns, rows, localTime);
    }

    private Color SequenceA3Raw(int column, int y, int columns, int rows, float localTime)
    {
        // 1) Les casiers (identiques a la version validee).
        if (localTime < phaseLockersEnd)
            return LockerWall(column, y, columns, rows, localTime);

        // 2) Le drapeau armenien balaie de gauche a droite et ecrit HETIC.
        return FlagWriteReveal(column, y, columns, rows, localTime - phaseLockersEnd);
    }

    /// <summary>
    /// Le drapeau armenien (rouge / bleu / abricot, bandes horizontales) essuie
    /// le mur de gauche a droite. Sur son passage, il "ecrit" HETIC : les
    /// lettres apparaissent en silhouette (creux sombre dans le drapeau), grandes
    /// et pleines. Le bord d'attaque est un liseré lumineux. Pulse leger sur le
    /// tempo via la luminosite maitresse.
    /// </summary>
    private Color FlagWriteReveal(int column, int row, int columns, int rows, float t)
    {
        float sweepDur = Mathf.Max(0.3f, flagSweepDuration);
        float sweep = Mathf.Clamp01(t / sweepDur);
        float edgeX = sweep * (columns + 10f);

        // Le drapeau n'a pas encore atteint cette colonne : noir.
        if (column > edgeX) return Color.black;

        // Bandes horizontales du drapeau (row = 0 en bas ; rouge en haut).
        int yTop = rows - 1 - row;
        int band = Mathf.Clamp(yTop * 3 / rows, 0, 2);
        Color flag = band == 0 ? flagTop : (band == 1 ? flagMiddle : flagBottom);

        // HETIC, grand et epais, centre.
        if (bodyTextBand != null && bodyTextBand.Length > 0)
        {
            int bandW = bodyTextBand[0].Length;
            int scale = Mathf.Max(1, Mathf.FloorToInt(columns * 0.9f / bandW));
            int textW = bandW * scale;
            int textH = PixelFont.GlyphHeight * scale;
            int ox = (columns - textW) / 2;
            int oy = Mathf.RoundToInt(rows * 0.5f) - textH / 2;

            int lx = column - ox;
            int ly = row - oy;
            if (lx >= 0 && lx < textW && ly >= 0 && ly < textH)
            {
                int bc = lx / scale;
                int brb = ly / scale;
                int sr = PixelFont.GlyphHeight - 1 - brb;
                if (bodyTextBand[sr][bc])
                {
                    // Lettre ecrite une fois que le balayage l'a depassee.
                    if (column <= edgeX)
                        return silhouetteLetters ? flag * 0.04f : wordColor;
                }
            }
        }

        // Liseré lumineux au bord d'attaque du drapeau.
        float edgeGlow = (sweep < 1f) ? Mathf.Exp(-((column - edgeX) * (column - edgeX)) / 7f) : 0f;
        Color result = flag;
        if (edgeGlow > 0.04f) result += Color.white * (edgeGlow * 0.85f);
        return result;
    }

    // -----------------------------------------------------------------------
    // Phase 1 — mur de casiers
    // -----------------------------------------------------------------------
    // Phase 1 — mur de casiers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Mur de casiers a dossiers. La grille est volontairement irreguliere :
    /// chaque rangee est decalee horizontalement, ce qui evite le quadrillage
    /// parfait et donne l'empilement de boites de la reference.
    ///
    /// Deux teintes seulement — creme et anthracite — separees par un joint
    /// noir epais. Chaque casier porte une petite etiquette claire : c'est ce
    /// detail qui fait lire "classeur" plutot que "carre lumineux".
    /// </summary>
    private Color LockerWall(int column, int row, int columns, int rows, float time)
    {
        int pitchY = lockerHeight + lockerGap;
        int cellY = row / pitchY;
        int insideY = row - cellY * pitchY;

        // Decalage horizontal propre a chaque rangee : la grille se desaligne.
        int rowShift = Hash(cellY * 31 + 7) % Mathf.Max(1, lockerWidth);

        int pitchX = lockerWidth + lockerGap;
        int shifted = column + rowShift;
        int cellX = shifted / pitchX;
        int insideX = shifted - cellX * pitchX;

        // Joint noir entre les casiers.
        if (insideX >= lockerWidth || insideY >= lockerHeight) return Color.black;

        Color body = LockerBody(cellX, cellY, time, columns);
        if (body.maxColorComponent <= 0.001f) return Color.black;

        // --- Etiquette : petit rectangle clair, cadre visible ---
        float labelHalfW = lockerWidth * 0.21f;
        float labelHalfH = Mathf.Max(1.2f, lockerHeight * 0.10f);
        float labelCX = lockerWidth * 0.5f;
        float labelCY = lockerHeight * 0.60f;

        float dx = Mathf.Abs(insideX - labelCX);
        float dy = Mathf.Abs(insideY - labelCY);

        if (dx <= labelHalfW && dy <= labelHalfH)
        {
            bool onBorder = dx > labelHalfW - 1.2f || dy > labelHalfH - 1.2f;

            // Sur un casier sombre l'etiquette ressort en clair ; sur un
            // casier clair c'est son cadre qui la dessine.
            bool darkBody = body.maxColorComponent < 0.5f;
            if (darkBody) return onBorder ? lockerBright : lockerBright * 0.75f;
            return onBorder ? lockerDark : body * 0.94f;
        }

        // Legere degradation verticale : le haut du casier accroche la lumiere.
        float shade = Mathf.Lerp(1.06f, 0.88f, insideY / (float)lockerHeight);
        return body * shade;
    }

    /// <summary>
    /// Teinte d'un casier, avec fondu entre l'etat precedent et le suivant :
    /// le mur respire au rythme au lieu de clignoter sechement.
    /// </summary>
    private Color LockerBody(int cellX, int cellY, float time, int columns)
    {
        int cellsPerRow = Mathf.Max(1, columns / Mathf.Max(1, lockerWidth)) + 2;
        int key = cellY * cellsPerRow + cellX;

        float beat = time * bpm / 60f;
        int beatIndex = Mathf.FloorToInt(beat);
        float fraction = beat - beatIndex;

        // Transition douce sur le premier tiers du temps : le mur respire au
        // lieu de clignoter sechement.
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fraction / 0.33f));

        Color previous = LockerTone(key, beatIndex - 1);
        Color next = LockerTone(key, beatIndex);
        return Color.Lerp(previous, next, blend);
    }

    private Color LockerTone(int key, int beatIndex)
    {
        int seed = Hash(key + beatIndex * 7919) % 100;
        if (seed < 34) return Color.black;        // casier eteint
        if (seed < 70) return lockerDark;         // anthracite
        return lockerBright;                      // creme
    }

    // -----------------------------------------------------------------------
    // Phase 2 — zoom dans une case
    // -----------------------------------------------------------------------

    /// <summary>
    /// Une case du centre grossit jusqu'a occuper tout le mur, pendant que les
    /// autres s'eteignent. La courbe est volontairement lente au debut puis
    /// s'accelere : c'est ce qui fait comprendre qu'on entre dans la case.
    /// </summary>
    private Color ZoomIntoLocker(int column, int row, int columns, int rows, float progress)
    {
        // Extinction des voisines sur le premier tiers de la phase.
        float others = Mathf.Clamp01(1f - progress * 3f);
        if (others > 0.01f)
        {
            Color around = LockerWall(column, row, columns, rows, phaseLockersEnd);
            around *= others;

            if (!InsideGrowingCell(column, row, columns, rows, progress))
                return around;
        }

        if (InsideGrowingCell(column, row, columns, rows, progress))
            return lockerBright;

        return Color.black;
    }

    /// <summary>
    /// Rectangle de la case qui grossit, interpole entre sa taille d'origine
    /// et le mur entier.
    /// </summary>
    private bool InsideGrowingCell(int column, int row, int columns, int rows, float progress)
    {
        // Acceleration progressive : lent, puis rapide.
        float eased = progress * progress;

        float halfWidth = Mathf.Lerp(lockerWidth * 0.5f, columns * 0.75f, eased);
        float halfHeight = Mathf.Lerp(lockerHeight * 0.5f, rows * 0.75f, eased);

        float centerX = columns * 0.5f;
        float centerY = rows * 0.5f;

        return Mathf.Abs(column - centerX) <= halfWidth
            && Mathf.Abs(row - centerY) <= halfHeight;
    }

    // -----------------------------------------------------------------------
    // Phase 3 — a l'interieur, les spots montent du sol
    // -----------------------------------------------------------------------

    /// <summary>
    /// Noir total, puis des faisceaux montent depuis le bas du mur — un par
    /// lyre de l'installation. La lumiere gagne en hauteur au fil de la phase.
    /// </summary>
    private Color InsideTheLocker(int column, int row, int columns, int rows, float progress)
    {
        float intensity = 0f;
        float reach = progress * rows * 0.6f;
        if (reach < 1f) return Color.black;

        float spread = columns * 0.085f;

        for (int i = 0; i < floorSpotCount; i++)
        {
            float spotX = columns * (i + 0.5f) / floorSpotCount;
            float dx = (column - spotX) / spread;
            float lateral = Mathf.Exp(-dx * dx);

            // Le faisceau s'attenue en montant.
            float vertical = Mathf.Clamp01(1f - row / reach);
            intensity += lateral * vertical * vertical;
        }

        // Sol tres lumineux sur les toutes premieres rangees.
        if (row < 3) intensity += progress * 0.8f;

        intensity = Mathf.Clamp01(intensity) * progress;
        return floorSpotColor * intensity;
    }

    // -----------------------------------------------------------------------
    // Phase 4 — la silhouette qui dessine les lettres
    // -----------------------------------------------------------------------

    /// <summary>
    /// Silhouette construite a partir d'un squelette : elle enchaine cinq
    /// poses acrobatiques qui dessinent H, E, T, I puis C. Le mot s'ecrit en
    /// petit au-dessus, et le sol renvoie le reflet de la figure.
    /// </summary>
    private Color LetterFigure(int column, int row, int columns, int rows, float phaseTime)
    {
        if (useScanner)
            return ScannerReveal(column, row, columns, rows, phaseTime);

        if (useParticles)
            return ParticlePhase(column, row, columns, rows, phaseTime);

        // Le corps glisse d'une lettre a la suivante : la pose est tenue les
        // deux premiers tiers du temps imparti, puis se transforme.
        float perLetter = LetterDuration();
        int poseA = Mathf.Clamp(Mathf.FloorToInt(phaseTime / perLetter), 0, BodyPoses.PoseCount - 1);
        int poseB = Mathf.Min(poseA + 1, BodyPoses.PoseCount - 1);
        float within = (phaseTime - poseA * perLetter) / perLetter;
        float morph = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 1f, within));
        BodyPoses.PrepareBlend(poseA, poseB, morph);
        _figurePhaseTime = phaseTime;

        int mirrorTop = Mathf.RoundToInt(rows * mirrorHeightRatio);

        // --- Sol miroir : on rejoue la figure a l'envers, attenuee ---
        if (row < mirrorTop)
        {
            int mirrored = 2 * mirrorTop - row;
            if (mirrored >= rows) return Color.black;

            float fade = row / (float)mirrorTop;
            Color above = FigureAndText(column, mirrored, columns, rows, mirrorTop, phaseTime);
            return above * (mirrorStrength * fade);
        }

        return FigureAndText(column, row, columns, rows, mirrorTop, phaseTime);
    }

    /// <summary>
    /// Revelation de HETIC par un balayage : une barre de lumiere traverse le
    /// mur de gauche a droite et allume les lettres sur son passage. Lettres
    /// epaisses et pleines. Un reflet doux ferme la composition au sol.
    /// </summary>
    private Color ScannerReveal(int column, int row, int columns, int rows, float phaseTime)
    {
        int mirrorTop = Mathf.RoundToInt(rows * mirrorHeightRatio);

        if (row < mirrorTop)
        {
            int mirrored = 2 * mirrorTop - row;
            if (mirrored >= rows) return Color.black;
            float fade = row / (float)mirrorTop;
            Color above = ScannerScene(column, mirrored, columns, rows, phaseTime);
            return above * (mirrorStrength * fade);
        }

        return ScannerScene(column, row, columns, rows, phaseTime);
    }

    private Color ScannerScene(int column, int row, int columns, int rows, float phaseTime)
    {
        if (bodyTextBand == null || bodyTextBand.Length == 0) return Color.black;

        // Grand mot, epais, centre : la police pixel agrandie donne des traits
        // larges et pleins, lisibles de loin.
        int bandW = bodyTextBand[0].Length;
        int scale = Mathf.Max(1, Mathf.FloorToInt(columns * 0.92f / bandW));
        int textW = bandW * scale;
        int textH = PixelFont.GlyphHeight * scale;
        int originX = (columns - textW) / 2;
        int originY = Mathf.RoundToInt(rows * 0.52f) - textH / 2;

        // Position de la barre : traverse tout le mur, avec une marge pour
        // qu'elle sorte proprement du cadre a la fin.
        float sweep = Mathf.Clamp01(phaseTime / scannerDuration);
        float barX = sweep * (columns + 8f);

        // La barre elle-meme : ligne verticale brillante sur toute la hauteur.
        float barDist = column - barX;
        float bar = (sweep < 1f) ? Mathf.Exp(-(barDist * barDist) / 6f) : 0f;

        // Le pixel appartient-il a une lettre ?
        bool onLetter = false;
        int lx = column - originX;
        int ly = row - originY;
        if (lx >= 0 && lx < textW && ly >= 0 && ly < textH)
        {
            int bandColumn = lx / scale;
            int bandRowFromBottom = ly / scale;
            int sourceRow = PixelFont.GlyphHeight - 1 - bandRowFromBottom;
            onLetter = bodyTextBand[sourceRow][bandColumn];
        }

        // Une lettre est allumee une fois que la barre l'a depassee.
        bool revealed = column <= barX;

        Color result = Color.black;

        if (onLetter && revealed)
        {
            // Lettre pleine, blanche. Sursaut a l'allumage, puis PULSE sur
            // chaque temps + scintillement fin sur les aigus.
            float freshGlow = Mathf.Clamp01(1f - (barX - column) / 10f) * 0.6f;
            float pulse = 0.55f + 0.5f * _auBeat + 0.2f * _auLevel;
            float shimmer = 1f + _auHigh * 0.35f * Mathf.Sin(column * 0.7f + row * 0.9f);
            result = wordColor * Mathf.Clamp01((0.85f + freshGlow) * pulse * shimmer);
        }

        // La barre de scan passe par-dessus tout, franche et lumineuse.
        if (bar > 0.02f)
        {
            Color barColor = Color.Lerp(figureColor, Color.white, 0.6f);
            result += barColor * bar;
        }

        return result;
    }

    /// <summary>
    /// Phase finale en nuage de particules : elles dansent puis forment HETIC.
    /// Gere son propre reflet au sol, comme la silhouette.
    /// </summary>
    private Color ParticlePhase(int column, int row, int columns, int rows, float phaseTime)
    {
        if (_particles == null || !_particles.Matches(columns, rows))
        {
            _particles = new ParticleField();
            float centerY = 0.30f + figureHeightRatio * 0.28f; // hauteur du mot
            _particles.Build(columns, rows, bodyTextBand, particleTextScale, centerY, 12345);
        }

        // Simulation faite une fois par trame (protegee en interne).
        _particles.Simulate(phaseTime, particleChaos, particleConverge);

        int mirrorTop = Mathf.RoundToInt(rows * mirrorHeightRatio);

        if (row < mirrorTop)
        {
            int mirrored = 2 * mirrorTop - row;
            if (mirrored >= rows) return Color.black;
            float fade = row / (float)mirrorTop;
            float refl = _particles.Sample(column, mirrored);
            return ParticleColor(refl) * (mirrorStrength * fade);
        }

        return ParticleColor(_particles.Sample(column, row));
    }

    /// <summary>Colorisation d'une intensite de particule : coeur blanc, halo chaud.</summary>
    private Color ParticleColor(float intensity)
    {
        if (intensity <= 0.003f) return Color.black;

        // Coeur presque blanc, franges legerement dorees — vivant sans etre criard.
        Color core = figureColor;
        Color warm = new Color(0.95f, 0.80f, 0.35f);
        Color c = Color.Lerp(warm, core, Mathf.Clamp01(intensity * 1.4f));
        return c * Mathf.Clamp01(intensity * 1.6f);
    }

    private Color FigureAndText(int column, int row, int columns, int rows, int mirrorTop, float phaseTime)
    {
        // --- Le mot, en petit, au-dessus de la figure ---
        Color textPixel = SmallWord(column, row, columns, rows, phaseTime);
        if (textPixel.maxColorComponent > 0f) return textPixel;

        // --- La silhouette ---
        float figureHeight = (rows - mirrorTop) * figureHeightRatio;
        if (figureHeight < 1f) return Color.black;

        // Micro-mouvement permanent : une respiration a peine perceptible qui
        // empeche la figure de paraitre figee entre deux poses. Deux frequences
        // incommensurables pour que le motif ne se repete jamais tout a fait.
        float breathe = _figurePhaseTime;
        float swayX = (Mathf.Sin(breathe * 2.3f) + 0.5f * Mathf.Sin(breathe * 5.1f)) * 0.006f;
        float swayY = Mathf.Sin(breathe * 1.7f) * 0.004f;

        float y = (row - mirrorTop) / figureHeight - swayY;
        if (y < -0.1f || y > 1.1f) return Color.black;

        // Repere isotrope : une meme distance a la meme echelle en x et en y.
        float x = (column - columns * 0.5f) / figureHeight - swayX;

        // Anticrénelage : bord lisse sur une bande d'environ 1 LED. Au lieu
        // d'un pixel tout allume ou tout eteint, on module l'intensite par la
        // couverture — c'est le gain visuel le plus fort sur ce mur.
        float edge = 0.9f / figureHeight;
        float coverage = BodyPoses.CoverageBlend(x, y, edge);
        if (coverage <= 0.003f) return Color.black;

        return figureColor * coverage;
    }

    private float LetterDuration()
    {
        float total = Mathf.Max(0.5f, figurePhaseDuration);
        return total / Mathf.Max(1, bodyTextLetters);
    }

    /// <summary>
    /// Le mot en petit, blanc, revele lettre par lettre au rythme des poses.
    /// Retourne du noir quand le pixel n'appartient pas a une lettre visible.
    /// </summary>
    private Color SmallWord(int column, int row, int columns, int rows, float phaseTime)
    {
        if (bodyTextBand == null || bodyTextBand.Length == 0) return Color.black;

        int scale = Mathf.Max(1, bodyTextScale);
        int bandWidth = bodyTextBand[0].Length;
        int textWidth = bandWidth * scale;
        int textHeight = PixelFont.GlyphHeight * scale;

        int originX = (columns - textWidth) / 2;
        int originY = Mathf.RoundToInt(rows * wordHeightRatio);

        int localX = column - originX;
        int localY = row - originY;

        if (localX < 0 || localX >= textWidth) return Color.black;
        if (localY < 0 || localY >= textHeight) return Color.black;

        int bandColumn = localX / scale;
        int bandRowFromBottom = localY / scale;
        int sourceRow = PixelFont.GlyphHeight - 1 - bandRowFromBottom;

        if (!bodyTextBand[sourceRow][bandColumn]) return Color.black;

        // Une lettre de plus a chaque changement de pose.
        int lettersShown = Mathf.FloorToInt(phaseTime / LetterDuration()) + 1;
        int letterIndex = bandColumn / (PixelFont.GlyphWidth + 1);
        if (letterIndex >= lettersShown) return Color.black;

        return wordColor;
    }

    /// <summary>
    /// Silhouettes posees sur des dalles lumineuses, avec reflet dans un sol
    /// miroir. Les danseurs avancent et les dalles qu'ils quittent restent
    /// allumees : le mot s'imprime ligne par ligne, de bas en haut.
    ///
    /// Cette methode gere le reflet, puis delegue la scene elle-meme a
    /// EvaluateTrailScene().
    /// </summary>
    private Color EvaluateDancersTrail(int column, int row, int columns, int rows, float localTime)
    {
        // Zone basse = reflet. On echantillonne la scene en miroir, attenuee.
        if (reflectionHeight > 0 && row < reflectionHeight)
        {
            int mirrored = 2 * reflectionHeight - row;
            if (mirrored >= rows) return Color.black;

            // Le reflet s'estompe en s'eloignant de la ligne de sol.
            float fade = row / (float)reflectionHeight;
            Color reflected = EvaluateTrailScene(column, mirrored, columns, rows, localTime);
            return reflected * (reflectionStrength * fade);
        }

        return EvaluateTrailScene(column, row, columns, rows, localTime);
    }

    private Color EvaluateTrailScene(int column, int row, int columns, int rows, float localTime)
    {
        if (trailX == null || trailX.Length == 0) return Color.black;

        int trailCount = trailX.Length;
        int pitch = tileSize + tileGap;
        int originX = (columns - trailCols * pitch) / 2;

        float progress = localTime * fillRate;
        int litCount = Mathf.Clamp(Mathf.FloorToInt(progress), 0, trailCount);
        float step = progress - Mathf.Floor(progress);

        // --- Les danseurs, dessines par-dessus les dalles ---
        for (int i = 0; i < dancerCount; i++)
        {
            int target = litCount - i;
            if (target < 0 || target >= trailCount) continue;

            int previous = Mathf.Max(0, target - 1);

            float fromX = originX + trailX[previous] * pitch + pitch * 0.5f;
            float toX = originX + trailX[target] * pitch + pitch * 0.5f;
            float centerX = Mathf.Lerp(fromX, toX, step);

            // Les pieds reposent sur le haut de la dalle visee.
            int feetY = trailBaseline + (trailY[target] + 1) * pitch;

            int spriteWidth = DancerSprites.Width * spriteScale;
            int localX = Mathf.FloorToInt(column - (centerX - spriteWidth * 0.5f));
            int localY = row - feetY;

            if (localX < 0 || localX >= spriteWidth) continue;
            if (localY < 0 || localY >= DancerSprites.Height * spriteScale) continue;

            // Chaque danseur change de pose sur le tempo, avec un decalage.
            float beat = localTime * bpm / 60f;
            int pose = Mathf.FloorToInt(beat) + i;

            if (!DancerSprites.IsFilled(pose, localX / spriteScale, localY / spriteScale))
                continue;

            return (i == accentDancerIndex) ? primaryColor : dancerColor;
        }

        // --- Les dalles deja allumees ---
        int tileX = (column - originX) / pitch;
        int tileY = (row - trailBaseline) / pitch;

        if (column < originX || row < trailBaseline) return Color.black;
        if (tileX < 0 || tileX >= trailCols || tileY < 0 || tileY >= trailRows) return Color.black;

        // Joint sombre entre les dalles.
        int insideX = (column - originX) % pitch;
        int insideY = (row - trailBaseline) % pitch;
        if (insideX >= tileSize || insideY >= tileSize) return Color.black;

        int tileOrder = trailOrder[tileY][tileX];
        if (tileOrder < 0 || tileOrder >= litCount) return Color.black;

        // La derniere dalle allumee brille encore un peu plus fort.
        float freshness = (tileOrder == litCount - 1) ? 1f : 0.78f;
        return tileColor * freshness;
    }

    /// <summary>
    /// Mur de tiroirs de classeurs, comme le decor de scene : une grille de
    /// grands rectangles separes par un joint sombre, chacun portant une
    /// petite etiquette claire. Une colonne peut rester noire, et un tiroir
    /// peut etre "tire" vers l'exterieur pour casser la regularite.
    /// </summary>
    private Color EvaluateDrawers(int column, int row, int columns, int rows, float localTime)
    {
        float cellWidth = columns / (float)drawerColumns;
        float cellHeight = rows / (float)drawerRows;

        int cellX = Mathf.Clamp((int)(column / cellWidth), 0, drawerColumns - 1);
        int cellY = Mathf.Clamp((int)(row / cellHeight), 0, drawerRows - 1);

        // Position a l'interieur du tiroir, en LED.
        float localX = column - cellX * cellWidth;
        float localY = row - cellY * cellHeight;

        // Le tiroir "tire" deborde : on elargit sa zone et on decale son contenu.
        bool isOpenDrawer = (cellX == openDrawerColumn && cellY == openDrawerRow);
        if (isOpenDrawer) localX -= cellWidth * 0.12f;

        // Joint entre les tiroirs.
        if (localX < drawerGap || localX > cellWidth - drawerGap ||
            localY < drawerGap || localY > cellHeight - drawerGap)
            return Color.black;

        float brightness = DrawerBrightness(cellX, cellY, localTime);

        // La colonne sombre du decor.
        if (cellX == darkColumnIndex) brightness *= 0.12f;

        // Le tiroir tire attrape plus de lumiere : il est en avant.
        if (isOpenDrawer) brightness = Mathf.Min(1f, brightness + 0.35f);

        Color body = Color.Lerp(drawerDim, drawerLit, brightness);

        // Etiquette : petit rectangle clair, centre horizontalement, au tiers haut.
        float labelHalfWidth = cellWidth * 0.13f;
        float labelCenterX = cellWidth * 0.5f;
        float labelCenterY = cellHeight * 0.62f;
        float labelHalfHeight = Mathf.Max(1.5f, cellHeight * 0.05f);

        bool onLabel = Mathf.Abs(localX - labelCenterX) < labelHalfWidth &&
                       Mathf.Abs(localY - labelCenterY) < labelHalfHeight;

        if (onLabel)
            return Color.Lerp(body, labelColor, 0.55f + 0.45f * brightness);

        return body;
    }

    /// <summary>Intensite d'un tiroir donne, selon le mode de remplissage.</summary>
    private float DrawerBrightness(int cellX, int cellY, float localTime)
    {
        float beat = localTime * bpm / 60f;
        int cellIndex = cellY * drawerColumns + cellX;
        int cellCount = drawerColumns * drawerRows;

        // Enveloppe d'un temps : attaque nette, extinction rapide.
        float decay = Mathf.Max(0f, 1f - (beat % 1f) * 2.2f);

        switch (fillMode)
        {
            case DrawerFillMode.Sequential:
                return (Mathf.FloorToInt(beat) % cellCount == cellIndex) ? decay : 0.05f;

            case DrawerFillMode.Random:
                return (Hash(Mathf.FloorToInt(beat)) % cellCount == cellIndex) ? decay : 0.05f;

            case DrawerFillMode.Wave:
            {
                float phase = (cellX + cellY) * 0.6f - beat * Mathf.PI;
                return Mathf.Max(0.05f, Mathf.Sin(phase) * 0.5f + 0.5f);
            }

            case DrawerFillMode.Contagion:
            {
                // Chaque tiroir a un delai propre : la desynchronisation part
                // d'un seul tiroir et gagne les autres au fil du clip.
                float delay = (Hash(cellIndex) % 100) / 100f * cellCount;
                bool awake = localTime * contagionSpeed > delay;

                // Les tiroirs "reveilles" clignotent a contretemps.
                float offset = awake ? 0.5f : 0f;
                float local = (beat + offset) % 1f;
                float envelope = Mathf.Max(0f, 1f - local * 2.2f);

                return awake ? envelope : envelope * 0.35f;
            }

            default:
                return 0.05f;
        }
    }

    /// <summary>Hachage deterministe : pas d'allocation, resultat stable.</summary>
    private static int Hash(int value)
    {
        unchecked
        {
            value = (value ^ 61) ^ (value >> 16);
            value += value << 3;
            value ^= value >> 4;
            value *= 0x27d4eb2d;
            value ^= value >> 15;
            return value & 0x7fffffff;
        }
    }

    private Color EvaluatePulse(int column, int row, float localTime)
    {
        // Priorite au vrai kick de la piste ; repli sur le contretemps BPM.
        var audio = AudioReactive.GetOrCreate();
        float clap;
        if (audio != null && audio.Level > 0.001f)
        {
            clap = audio.Beat;
        }
        else
        {
            float beat = localTime * bpm / 60f;
            clap = (beat % 2f) > 1f ? Mathf.Max(0f, 1f - (beat % 1f) * 4f) : 0f;
        }

        // Fond grisaille legerement bruite pour eviter un aplat mort.
        float grey = 0.07f + 0.05f * ((column * 3 + row * 7) % 3);
        var baseColor = new Color(grey, grey, grey);

        return Color.Lerp(baseColor, primaryColor, clap);
    }
}
