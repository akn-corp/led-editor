// Animation fluide sur le mur LED — vague colorée qui parcourt tout le tableau.

using UnityEngine;

[RequireComponent(typeof(LedWallVisualizer))]
public class FluidWallAnimator : MonoBehaviour
{
    [SerializeField] private EntityManager entityManager;
    [SerializeField] private float speed = 1.2f;
    [SerializeField] private float waveScale = 6f;
    [SerializeField] private float glowIntensity = 1.8f;
    [SerializeField] private bool runOnStart = true;

    private LedWallVisualizer _visualizer;
    private int _columns;
    private int _rows;
    private int?[,] _entityGrid;
    private Color[] _displayPixels;
    private float _timer;
    private const float UpdateInterval = 1f / 30f;

    public void Initialize(EntityManager entityManager, LedWallVisualizer visualizer, int columns)
    {
        this.entityManager = entityManager;
        _visualizer = visualizer;
        _columns = columns;
        _rows = WallMapping.VisibleRows;

        _entityGrid = new int?[_rows, _columns];
        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _columns; col++)
                _entityGrid[row, col] = WallMapping.EntityIdForCell(row, col);
        }

        _displayPixels = new Color[_columns * _rows];
    }

    void Start()
    {
        if (!runOnStart) return;
        if (_visualizer == null)
            _visualizer = GetComponent<LedWallVisualizer>();
        if (_entityGrid == null && WallMapping.IsInitialized)
            Initialize(entityManager, _visualizer, WallMapping.Columns);
    }

    void Update()
    {
        if (_entityGrid == null || entityManager == null || _visualizer == null) return;

        _timer += Time.deltaTime;
        if (_timer < UpdateInterval) return;
        _timer = 0f;

        float t = Time.time * speed;
        ApplyFluidFrame(t);
    }

    private void ApplyFluidFrame(float time)
    {
        int cols = _columns;
        int rows = _rows;
        float invCols = 1f / Mathf.Max(1, cols - 1);
        float invRows = 1f / Mathf.Max(1, rows - 1);

        for (int row = 0; row < rows; row++)
        {
            float ny = row * invRows;
            int texRow = rows - 1 - row;

            for (int col = 0; col < cols; col++)
            {
                float nx = col * invCols;

                // Ondes superposées → effet fluide organique
                float w1 = Mathf.Sin((nx * waveScale + ny * (waveScale * 0.65f)) - time * 2.8f);
                float w2 = Mathf.Sin((nx * (waveScale * 0.5f) - ny * (waveScale * 0.8f)) - time * 2.1f + 1.4f);
                float dist = Mathf.Sqrt((nx - 0.5f) * (nx - 0.5f) + (ny - 0.5f) * (ny - 0.5f));
                float w3 = Mathf.Sin(dist * waveScale * 1.8f - time * 3.2f);
                float ripple = Mathf.Sin((nx + ny) * waveScale * 0.9f - time * 4f);

                float intensity = (w1 + w2 + w3 + ripple) * 0.25f + 0.5f;
                intensity = Mathf.Clamp01(intensity);
                intensity = intensity * intensity;

                float hue = (nx * 0.35f + ny * 0.25f + time * 0.12f) % 1f;
                Color rgb = Color.HSVToRGB(hue, 0.9f, intensity);

                rgb.r *= glowIntensity;
                rgb.g *= glowIntensity;
                rgb.b *= glowIntensity;

                _displayPixels[texRow * cols + col] = rgb;

                int? entityId = _entityGrid[row, col];
                if (!entityId.HasValue) continue;

                byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.r * 255f), 0, 255);
                byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.g * 255f), 0, 255);
                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.b * 255f), 0, 255);
                entityManager.SetColorSilent(entityId.Value, r, g, b);
            }
        }

        _visualizer.ApplyDisplayPixels(_displayPixels);
    }
}
