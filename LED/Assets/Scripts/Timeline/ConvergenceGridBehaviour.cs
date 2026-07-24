// Comportement Timeline — convergence géométrique en 3 phases.
//
//   Phase 1 : les lignes verticales apparaissent depuis les bords extérieurs
//             (gauche/droite) et convergent vers leurs positions cibles,
//             pendant que les lignes intermédiaires s'insèrent pour remplir l'espace.
//   Phase 2 : des lignes horizontales viennent croiser les verticales -> quadrillage complet.
//   Phase 3 : le quadrillage se transforme en matrice de CARRÉS lumineux espacés.
//
// IMPORTANT : les cellules LED sont physiquement carrées (cellWorldSize uniforme dans
// LedWallVisualizer). On raisonne donc en ESPACE-CELLULES : un carré = N cellules × N
// cellules. Le nombre de rangées de carrés est déduit du "pitch" horizontal pour que
// les modules restent carrés quel que soit le rapport largeur/hauteur du mur.
//
// Rendu : lumière blanche pure très vive (sortie HDR > 1 pour l'émission/bloom).
// Même pipeline que FluidWallBehaviour : WallMapping + EntityManager + LedWallVisualizer.

using UnityEngine;
using UnityEngine.Playables;

public class ConvergenceGridBehaviour : PlayableBehaviour
{
    // --- Paramètres (renseignés par le clip) ---
    public float phase1Weight = 1f;
    public float phase2Weight = 1f;
    public float phase3Weight = 1f;
    public int squareColumns = 8;
    public float lineThicknessCells = 1.6f;
    public float squareFillRatio = 0.5f;
    public float glowIntensity = 2.6f;
    public Color tint = Color.white;
    public float clipDuration = 0f; // secours si le mixer ne fournit pas la durée

    private Color[] _displayPixels;
    private LedWallVisualizer _visualizer;

    public void Apply(EntityManager entityManager, float clipTime, float duration)
    {
        Apply(entityManager, null, clipTime, duration);
    }

    public void Apply(EntityManager entityManager, LedWallVisualizer visualizer, float clipTime, float duration)
    {
        if (entityManager == null || !WallMapping.IsInitialized) return;

        int cols = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;
        if (cols <= 1 || rows <= 1) return;

        EnsureBuffer(cols, rows);
        if (visualizer != null)
            _visualizer = visualizer;
        if (_visualizer == null)
            _visualizer = RippleWaveBehaviour.FindLedWallVisualizer();

        // --- Découpage temporel des 3 phases ---
        float total = duration > 0.0001f ? duration : (clipDuration > 0.0001f ? clipDuration : 1f);
        float wSum = Mathf.Max(0.0001f, phase1Weight + phase2Weight + phase3Weight);
        float d1 = total * (phase1Weight / wSum);
        float d2 = total * (phase2Weight / wSum);

        float t = Mathf.Clamp(clipTime, 0f, total);
        float p1, p2, p3;
        if (t < d1)
        {
            p1 = Smooth(SafeDiv(t, d1)); p2 = 0f; p3 = 0f;
        }
        else if (t < d1 + d2)
        {
            p1 = 1f; p2 = Smooth(SafeDiv(t - d1, d2)); p3 = 0f;
        }
        else
        {
            p1 = 1f; p2 = 1f; p3 = Smooth(SafeDiv(t - d1 - d2, total - d1 - d2));
        }

        // --- Géométrie en espace-cellules (carrés garantis) ---
        // squareColumns / épaisseurs authorés pour 128 → adapter au viewport
        float cellScale = cols / 128f;
        int sqCols = Mathf.Max(1, Mathf.RoundToInt(squareColumns * cellScale));
        if (sqCols > cols / 2) sqCols = Mathf.Max(1, cols / 4);
        float pitch = cols / (float)sqCols;
        int sqRows = Mathf.Max(1, Mathf.FloorToInt(rows / pitch));
        float usedH = sqRows * pitch;
        float offsetY = (rows - usedH) * 0.5f;
        float offsetX = 0f;

        float halfSide = pitch * squareFillRatio * 0.5f;
        float edge = Mathf.Clamp(pitch * 0.06f, 0.35f, 2.5f);
        float halfThick = Mathf.Max(0.5f, lineThicknessCells * cellScale) * 0.5f;

        for (int row = 0; row < rows; row++)
        {
            int texRow = rows - 1 - row;

            // Ligne horizontale la plus proche (phase 2) — en cellules
            float rowF = row;

            for (int col = 0; col < cols; col++)
            {
                float colF = col;

                // ---- Lignes verticales (phase 1) ----
                float vInt = 0f;
                for (int i = 0; i <= sqCols; i++)
                {
                    float tx = offsetX + i * pitch;                 // cible en cellules
                    float startX = tx < cols * 0.5f ? 0f : (cols - 1);
                    float curX = Mathf.Lerp(startX, tx, p1);
                    vInt = Mathf.Max(vInt, LineFalloff(colF - curX, halfThick));
                }
                vInt *= p1;

                // ---- Lignes horizontales (phase 2) ----
                float hInt = 0f;
                if (p2 > 0f)
                {
                    for (int j = 0; j <= sqRows; j++)
                    {
                        float ty = offsetY + j * pitch;
                        float startY = ty < rows * 0.5f ? 0f : (rows - 1);
                        float curY = Mathf.Lerp(startY, ty, p2);
                        hInt = Mathf.Max(hInt, LineFalloff(rowF - curY, halfThick));
                    }
                    hInt *= p2;
                }

                float gridInt = Mathf.Max(vInt, hInt);

                // ---- Matrice de carrés (phase 3) ----
                float finalInt = gridInt;
                if (p3 > 0f)
                {
                    float bx = (colF - offsetX) / pitch;
                    float by = (rowF - offsetY) / pitch;
                    int ix = Mathf.FloorToInt(bx);
                    int iy = Mathf.FloorToInt(by);

                    float squareInt = 0f;
                    if (ix >= 0 && ix < sqCols && iy >= 0 && iy < sqRows)
                    {
                        float centerX = offsetX + (ix + 0.5f) * pitch;
                        float centerY = offsetY + (iy + 0.5f) * pitch;
                        float distX = Mathf.Abs(colF - centerX);
                        float distY = Mathf.Abs(rowF - centerY);
                        float sqX = 1f - Smooth01((distX - halfSide) / edge);
                        float sqY = 1f - Smooth01((distY - halfSide) / edge);
                        squareInt = Mathf.Clamp01(sqX) * Mathf.Clamp01(sqY);
                    }

                    finalInt = Mathf.Lerp(gridInt, squareInt, p3);
                }

                finalInt = Mathf.Clamp01(finalInt);

                Color rgb = tint * (finalInt * glowIntensity);
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

    // Intensité d'un trait : 1 au centre, 0 au-delà de la demi-épaisseur.
    private static float LineFalloff(float delta, float halfThick)
    {
        float d = Mathf.Abs(delta);
        if (d >= halfThick) return 0f;
        return 1f - (d / halfThick);
    }

    private static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private static float Smooth01(float x) => Smooth(x);

    private static float SafeDiv(float a, float b) => b <= 0.0001f ? 1f : a / b;

    private void EnsureBuffer(int cols, int rows)
    {
        int n = cols * rows;
        if (_displayPixels == null || _displayPixels.Length != n)
            _displayPixels = new Color[n];
    }
}
