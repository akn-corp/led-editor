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
        Apply(entityManager, null, clipTime, weight);
    }

    public void Apply(EntityManager entityManager, LedWallVisualizer visualizer, float clipTime, float weight)
    {
        if (entityManager == null || !WallMapping.IsInitialized) return;

        int cols = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;
        if (cols <= 1 || rows <= 1) return;

        EnsureBuffer(cols, rows);
        if (visualizer != null)
            _visualizer = visualizer;
        if (_visualizer == null)
            _visualizer = FindLedWallVisualizer();

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

        float wavelength = Mathf.Max(2f, WallMapping.ScaleCellsFrom128(wavelengthCells));
        float lineW = Mathf.Max(0.4f, WallMapping.ScaleCellsFrom128(lineWidthCells));
        float halo = Mathf.Max(1f, WallMapping.ScaleCellsFrom128(haloCells));
        float edgeNoise = WallMapping.ScaleCellsFrom128(edgeNoiseCells);

        float invWave = 1f / wavelength;
        float w = lineW;
        float invWidthSq = 1f / (2f * w * w);
        float invHaloOut = 1f / halo;
        float invHaloIn = 1f / Mathf.Max(1f, halo * 0.4f);
        const float noiseFreq = 4.5f;
        const float noiseSpeed = 0.35f;

        for (int row = 0; row < rows; row++)
        {
            int texRow = rows - 1 - row;
            float dy = row - originRow;

            for (int col = 0; col < cols; col++)
            {
                float dx = col - originCol;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (edgeNoise > 0.01f)
                {
                    float ang = Mathf.Atan2(dy, dx);
                    float n1 = Mathf.PerlinNoise(ang * noiseFreq + 13.7f, clipTime * noiseSpeed) - 0.5f;
                    float n2 = Mathf.PerlinNoise(ang * noiseFreq * 2.3f + 51.3f, clipTime * noiseSpeed * 1.7f) - 0.5f;
                    float n = n1 + 0.5f * n2;
                    float radialRamp = Mathf.Clamp01(dist * (0.03f * 128f / Mathf.Max(1, cols)));
                    dist += n * 2f * edgeNoise * radialRamp;
                }

                float phase = dist * invWave - clipTime * wavesPerSecond;
                float f = phase - Mathf.Floor(phase);
                bool outward = f < 0.5f;
                float dRing = (outward ? f : 1f - f) * wavelength;

                float core = Mathf.Exp(-(dRing * dRing) * invWidthSq);
                float haloVal = Mathf.Exp(-dRing * (outward ? invHaloOut : invHaloIn));

                float atten = Mathf.Clamp01(1f - distanceFade * (dist * invCorner));
                float value = (core * glowIntensity + haloVal * haloStrength) * atten * weight;

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

    /// <summary>FindFirstObjectByType ignore souvent LedWall (DontSaveInEditor).</summary>
    public static LedWallVisualizer FindLedWallVisualizer()
    {
        var wall = Object.FindFirstObjectByType<LedWallVisualizer>();
        if (wall != null) return wall;

        var all = Resources.FindObjectsOfTypeAll<LedWallVisualizer>();
        for (int i = 0; i < all.Length; i++)
        {
            var w = all[i];
            if (w == null) continue;
            if (!w.gameObject.scene.IsValid()) continue;
            return w;
        }
        return null;
    }
}
