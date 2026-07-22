// Comportement Timeline — chorégraphie ligne fine + barre pleine pulsante.
//
//   1. Descente : la ligne fine part du HAUT et descend jusqu'à la barre (impact).
//   2. Pulsation : à l'impact, la barre pleine s'allume (Or) et pulse. La ligne a fusionné.
//   3. Remontée : la barre s'éteint, la ligne remonte vers le haut.
//   4. Re-allumage : la barre pulsante réapparaît.
//   5. Inversion : la barre passe de l'Or au Cyan.
//
// Barre = tube plein (cœur + halo) avec point lumineux balayeur et respiration.
// Repère : row 0 = HAUT du mur ; bas = row (rows-1). HDR (>1) pour le bloom.

using UnityEngine;
using UnityEngine.Playables;

public class PulseBarSweepBehaviour : PlayableBehaviour
{
    // --- Paramètres (renseignés par le clip) ---
    public float phaseDescendWeight = 1.2f;
    public float phasePulseWeight = 1.5f;
    public float phaseRiseWeight = 1.2f;
    public float phaseRelightWeight = 1f;
    public float phaseInvertWeight = 1f;
    public int barHeightCells = 4;
    public int barBottomMarginCells = 22;
    public float glowCells = 3f;
    public float sweepSpeed = 0.5f;
    public float sweepStrength = 0.7f;
    public float sweepWidth = 0.12f;
    public float pulseSpeed = 2f;
    public float pulseDepth = 0.6f;
    public float lineWidthCells = 1f;
    public int lineTopMarginCells = 6;
    public Color goldColor = new Color(1f, 0.72f, 0.12f);
    public Color cyanColor = new Color(0.15f, 0.85f, 1f);
    public Color lineColor = new Color(1f, 0.82f, 0.35f);
    public float glowIntensity = 3.2f;

    private Color[] _displayPixels;
    private LedWallVisualizer _visualizer;

    private const float TwoPi = Mathf.PI * 2f;

    public void Apply(EntityManager entityManager, float clipTime, float duration, float weight)
    {
        if (entityManager == null || !WallMapping.IsInitialized) return;

        int cols = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;
        if (cols <= 1 || rows <= 1) return;

        EnsureBuffer(cols, rows);
        if (_visualizer == null)
            _visualizer = Object.FindFirstObjectByType<LedWallVisualizer>();

        int barBottom = Mathf.Clamp(rows - 1 - barBottomMarginCells, 1, rows - 1);
        int barHeight = Mathf.Clamp(barHeightCells, 1, barBottom);
        int barTopRow = barBottom - barHeight + 1;

        float lineTop = lineTopMarginCells;
        float impactRow = barTopRow - 1;   // la ligne s'arrête juste au-dessus de la barre

        // --- Découpage des 5 phases ---
        float total = duration > 0.0001f ? duration : 1f;
        float wSum = phaseDescendWeight + phasePulseWeight + phaseRiseWeight
                     + phaseRelightWeight + phaseInvertWeight;
        float d1 = total * (phaseDescendWeight / wSum);
        float d2 = total * (phasePulseWeight / wSum);
        float d3 = total * (phaseRiseWeight / wSum);
        float d4 = total * (phaseRelightWeight / wSum);
        float t = Mathf.Clamp(clipTime, 0f, total);

        float lineActive = 0f;
        float lineRow = lineTop;
        float barOn = 0f;
        float colorMix = 0f;

        if (t < d1)                                   // 1. Descente
        {
            float p = Smooth(t / d1);
            lineActive = 1f;
            lineRow = Mathf.Lerp(lineTop, impactRow, p);
        }
        else if (t < d1 + d2)                         // 2. Pulsation (impact)
        {
            float p = (t - d1) / d2;
            barOn = Smooth(Mathf.Clamp01(p * 5f));    // s'allume net à l'impact
        }
        else if (t < d1 + d2 + d3)                    // 3. Remontée
        {
            float p = (t - d1 - d2) / d3;
            barOn = 1f - Smooth(Mathf.Clamp01(p * 5f)); // s'éteint net
            lineActive = 1f;
            lineRow = Mathf.Lerp(impactRow, lineTop, Smooth(p));
        }
        else if (t < d1 + d2 + d3 + d4)               // 4. Re-allumage
        {
            float p = (t - d1 - d2 - d3) / d4;
            barOn = Smooth(Mathf.Clamp01(p * 5f));
        }
        else                                          // 5. Inversion Or -> Cyan
        {
            float p = Smooth((t - d1 - d2 - d3 - d4) / Mathf.Max(0.0001f, total - d1 - d2 - d3 - d4));
            barOn = 1f;
            colorMix = p;
        }

        // Fondu global (bords du clip).
        float fade = Mathf.Min(0.3f, total * 0.1f);
        float env = Mathf.Clamp01(clipTime / Mathf.Max(0.0001f, fade))
                    * Mathf.Clamp01((total - clipTime) / Mathf.Max(0.0001f, fade)) * weight;

        Color barBase = Color.Lerp(goldColor, cyanColor, colorMix);
        float breathe = (1f - pulseDepth) + pulseDepth * (0.5f + 0.5f * Mathf.Sin(clipTime * pulseSpeed * TwoPi));
        float sweepPos = 0.5f - 0.5f * Mathf.Cos(clipTime * sweepSpeed * TwoPi);
        float invSweepW = 1f / (2f * sweepWidth * sweepWidth);
        float lw = Mathf.Max(0.4f, lineWidthCells);
        float invLineSq = 1f / (2f * lw * lw);
        float invCols = 1f / Mathf.Max(1, cols - 1);
        float invGlowSq = glowCells > 0.01f ? 1f / (2f * glowCells * glowCells) : 0f;

        for (int row = 0; row < rows; row++)
        {
            int texRow = rows - 1 - row;

            // Profil vertical barre (plein + halo).
            float vprof;
            if (row >= barTopRow && row <= barBottom) vprof = 1f;
            else
            {
                float dOut = row < barTopRow ? (barTopRow - row) : (row - barBottom);
                vprof = invGlowSq > 0f ? Mathf.Exp(-(dOut * dOut) * invGlowSq) : 0f;
            }

            // Ligne fine à cette rangée.
            float lineInt = 0f;
            if (lineActive > 0f)
            {
                float dl = row - lineRow;
                lineInt = Mathf.Exp(-(dl * dl) * invLineSq) * lineActive;
            }

            for (int col = 0; col < cols; col++)
            {
                Color rgb = Color.black;

                if (barOn > 0f && vprof > 0.004f)
                {
                    float nx = col * invCols;
                    float dxs = Mathf.Abs(nx - sweepPos);
                    dxs = Mathf.Min(dxs, 1f - dxs);
                    float sweep = Mathf.Exp(-(dxs * dxs) * invSweepW);
                    float level = barOn * (1f + sweepStrength * sweep) * vprof * breathe * env;
                    rgb += barBase * (level * glowIntensity);
                }

                if (lineInt > 0.004f)
                    rgb += lineColor * (lineInt * env * glowIntensity);

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

    private static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private void EnsureBuffer(int cols, int rows)
    {
        int n = cols * rows;
        if (_displayPixels == null || _displayPixels.Length != n)
            _displayPixels = new Color[n];
    }
}
