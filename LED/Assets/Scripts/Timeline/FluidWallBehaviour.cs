// Applique une frame de vague fluide (même logique que FluidWallAnimator).

using UnityEngine;
using UnityEngine.Playables;

public class FluidWallBehaviour : PlayableBehaviour
{
    public float speed = 1.2f;
    public float waveScale = 6f;
    public float glowIntensity = 1.8f;

    private Color[] _displayPixels;
    private LedWallVisualizer _visualizer;

    public void Apply(EntityManager entityManager, float clipTime)
    {
        Apply(entityManager, null, clipTime);
    }

    public void Apply(EntityManager entityManager, LedWallVisualizer visualizer, float clipTime)
    {
        if (entityManager == null || !WallMapping.IsInitialized) return;

        int cols = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;
        EnsureBuffer(cols, rows);

        if (visualizer != null)
            _visualizer = visualizer;
        if (_visualizer == null)
            _visualizer = Object.FindFirstObjectByType<LedWallVisualizer>();
        if (_visualizer == null)
        {
            Debug.LogWarning("[FluidWall] LedWallVisualizer introuvable — mur pas encore construit.");
            return;
        }

        _visualizer.SetSuppressSingleUpdates(true);
        float time = clipTime * speed;
        float invCols = 1f / Mathf.Max(1, cols - 1);
        float invRows = 1f / Mathf.Max(1, rows - 1);

        for (int row = 0; row < rows; row++)
        {
            float ny = row * invRows;
            int texRow = rows - 1 - row;

            for (int col = 0; col < cols; col++)
            {
                float nx = col * invCols;

                float w1 = Mathf.Sin((nx * waveScale + ny * (waveScale * 0.65f)) - time * 2.8f);
                float w2 = Mathf.Sin((nx * (waveScale * 0.5f) - ny * (waveScale * 0.8f)) - time * 2.1f + 1.4f);
                float dist = Mathf.Sqrt((nx - 0.5f) * (nx - 0.5f) + (ny - 0.5f) * (ny - 0.5f));
                float w3 = Mathf.Sin(dist * waveScale * 1.8f - time * 3.2f);
                float ripple = Mathf.Sin((nx + ny) * waveScale * 0.9f - time * 4f);

                float intensity = (w1 + w2 + w3 + ripple) * 0.25f + 0.5f;
                intensity = Mathf.Clamp01(intensity);
                intensity *= intensity;

                float hue = (nx * 0.35f + ny * 0.25f + time * 0.12f) % 1f;
                Color rgb = Color.HSVToRGB(hue, 0.9f, intensity);
                rgb.r *= glowIntensity;
                rgb.g *= glowIntensity;
                rgb.b *= glowIntensity;

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

    private void EnsureBuffer(int cols, int rows)
    {
        int n = cols * rows;
        if (_displayPixels == null || _displayPixels.Length != n)
            _displayPixels = new Color[n];
    }
}
