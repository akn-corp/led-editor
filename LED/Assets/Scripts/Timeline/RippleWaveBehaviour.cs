// Comportement Timeline — ondes concentriques (ripple néon).
//
// Des arcs de cercle lumineux naissent à une origine (par défaut bas/centre du mur)
// et se propagent en pulsations concentriques vers les bords ; un nouvel arc réapparaît
// au centre à chaque cycle. Contours diffus, rendu néon HDR pour l'émission/bloom.
//
// L'origine étant sur un bord, les cercles complets apparaissent naturellement comme
// des demi-cercles / arcs. Même pipeline que FluidWallBehaviour.
//
// Repère : row 0 = HAUT du mur (cf. LedWallVisualizer), donc originY=0 (bas) => row rows-1.

using UnityEngine;
using UnityEngine.Playables;

public class RippleWaveBehaviour : PlayableBehaviour
{
    // --- Paramètres (renseignés par le clip) ---
    public float originX = 0.5f;
    public float originY = 0f;
    public float wavelengthCells = 22f;
    public float wavesPerSecond = 0.7f;
    public float lineWidthCells = 1.2f;   // cœur fin (tube néon)
    public float haloCells = 8f;          // étendue du halo diffus
    public float haloStrength = 0.35f;    // intensité du halo (0 = pas de halo)
    public float edgeNoiseCells = 1.5f;   // ondulation organique des bords (0 = arcs lisses)
    public float distanceFade = 0.2f;
    public float glowIntensity = 3f;
    public Color tint = new Color(0.15f, 0.7f, 1f);

    private Color[] _displayPixels;
    private LedWallVisualizer _visualizer;

    public void Apply(EntityManager entityManager, float clipTime, float weight)
    {
        if (entityManager == null || !WallMapping.IsInitialized) return;

        int cols = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;
        if (cols <= 1 || rows <= 1) return;

        EnsureBuffer(cols, rows);
        if (_visualizer == null)
            _visualizer = Object.FindFirstObjectByType<LedWallVisualizer>();

        // Origine en cellules. originY : 0 = bas (row rows-1), 1 = haut (row 0).
        float originCol = originX * (cols - 1);
        float originRow = (1f - originY) * (rows - 1);

        // Distance de référence = coin du mur le plus éloigné de la source.
        // L'atténuation est calée dessus pour que les arcs restent visibles jusqu'aux
        // bords/au sommet (le fade représente la perte d'intensité AU coin le plus loin).
        float cornerDist = Mathf.Max(
            Mathf.Max(Dist(0f, 0f, originCol, originRow), Dist(cols - 1, 0f, originCol, originRow)),
            Mathf.Max(Dist(0f, rows - 1, originCol, originRow), Dist(cols - 1, rows - 1, originCol, originRow)));
        float invCorner = 1f / Mathf.Max(1f, cornerDist);
        float invWave = 1f / Mathf.Max(2f, wavelengthCells);
        // Cœur fin (gaussien) + halo large et sombre -> aspect tube néon.
        float w = Mathf.Max(0.4f, lineWidthCells);
        float invWidthSq = 1f / (2f * w * w);
        // Halo asymétrique : la lueur bave davantage vers l'EXTÉRIEUR de l'arc (comme la
        // brume néon réelle) que vers l'intérieur.
        float invHaloOut = 1f / Mathf.Max(1f, haloCells);
        float invHaloIn = 1f / Mathf.Max(1f, haloCells * 0.4f);
        // Bruit organique : ondule le rayon de l'arc pour casser la symétrie parfaite.
        const float noiseFreq = 4.5f;   // nb d'ondulations autour de l'arc
        const float noiseSpeed = 0.35f; // évolution temporelle du bruit

        for (int row = 0; row < rows; row++)
        {
            int texRow = rows - 1 - row;
            float dy = row - originRow;

            for (int col = 0; col < cols; col++)
            {
                float dx = col - originCol;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Ondulation du rayon selon l'angle (bords turbulents/atmosphériques).
                // Deux octaves de bruit pour un chaos plus organique, atténuées près de
                // l'origine (sinon petit rayon = déformation en étoile).
                if (edgeNoiseCells > 0.01f)
                {
                    float ang = Mathf.Atan2(dy, dx);
                    float n1 = Mathf.PerlinNoise(ang * noiseFreq + 13.7f, clipTime * noiseSpeed) - 0.5f;
                    float n2 = Mathf.PerlinNoise(ang * noiseFreq * 2.3f + 51.3f, clipTime * noiseSpeed * 1.7f) - 0.5f;
                    float n = n1 + 0.5f * n2;
                    float radialRamp = Mathf.Clamp01(dist * 0.03f); // ~0 au centre -> 1 vers ~33 cellules
                    dist += n * 2f * edgeNoiseCells * radialRamp;
                }

                // Onde concentrique : la phase avance dans le temps -> arcs qui grandissent.
                float phase = dist * invWave - clipTime * wavesPerSecond;
                float f = phase - Mathf.Floor(phase);              // 0..1 dans le cycle
                bool outward = f < 0.5f;                            // côté extérieur de l'arc ?
                float dRing = (outward ? f : 1f - f) * wavelengthCells; // distance (cellules) à l'arc

                float core = Mathf.Exp(-(dRing * dRing) * invWidthSq);  // tube fin et vif
                float halo = Mathf.Exp(-dRing * (outward ? invHaloOut : invHaloIn));

                // Atténuation douce : au coin le plus éloigné il reste (1 - distanceFade).
                float atten = Mathf.Clamp01(1f - distanceFade * (dist * invCorner));

                // Le cœur passe en HDR (>1) pour le bloom ; le halo reste sombre (dégradé gris).
                float value = (core * glowIntensity + halo * haloStrength) * atten * weight;

                Color rgb = tint * value;
                _displayPixels[texRow * cols + col] = rgb;

                int? entityId = WallMapping.EntityIdForCell(row, col);
                if (!entityId.HasValue) continue;

                byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.r * 255f), 0, 255);
                byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.g * 255f), 0, 255);
                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.b * 255f), 0, 255);
                entityManager.SetColorSilent(entityId.Value, r, g, b);
            }
        }

        _visualizer?.ApplyDisplayPixels(_displayPixels);
    }

    private static float Dist(float ax, float ay, float bx, float by)
    {
        float dx = ax - bx, dy = ay - by;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private void EnsureBuffer(int cols, int rows)
    {
        int n = cols * rows;
        if (_displayPixels == null || _displayPixels.Length != n)
            _displayPixels = new Color[n];
    }
}
