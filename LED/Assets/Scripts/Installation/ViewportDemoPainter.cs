// Démo viewport 32×32 : motif reconnaissable (bordure + croix) pour valider
// Unity ↔ panneau (carré centré). Attacher sur LedWall ou un GO de test.

using UnityEngine;

[DisallowMultipleComponent]
public class ViewportDemoPainter : MonoBehaviour
{
    [Tooltip("Si true, peint le motif chaque frame (utile sans Timeline).")]
    public bool paintEveryFrame = true;

    [Tooltip("Couleur de fond du viewport.")]
    public Color fillColor = new Color(0.05f, 0.05f, 0.12f);

    [Tooltip("Couleur de la bordure / croix.")]
    public Color accentColor = new Color(1f, 0.2f, 0.35f);

    EntityManager _entities;
    LedWallVisualizer _visualizer;
    Color[] _pixels;
    int _cols;
    int _rows;

    void Start()
    {
        EnsureReady();
        PaintOnce();
    }

    void LateUpdate()
    {
        if (paintEveryFrame)
            PaintOnce();
    }

    void EnsureReady()
    {
        if (!WallMapping.IsInitialized)
        {
            Debug.LogWarning("[ViewportDemoPainter] WallMapping non initialisé — sync wall-bands d’abord.");
            return;
        }

        _cols = WallMapping.Columns;
        _rows = WallMapping.VisibleRows;
        if (_cols <= 0 || _rows <= 0) return;

        if (_entities == null)
            _entities = FindFirstObjectByType<EntityManager>();
        if (_visualizer == null)
            _visualizer = FindFirstObjectByType<LedWallVisualizer>();

        int n = _cols * _rows;
        if (_pixels == null || _pixels.Length != n)
            _pixels = new Color[n];
    }

    [ContextMenu("Paint demo pattern")]
    public void PaintOnce()
    {
        EnsureReady();
        if (_pixels == null || _cols <= 0 || _rows <= 0) return;

        for (int i = 0; i < _pixels.Length; i++)
            _pixels[i] = fillColor;

        // Bordure
        for (int x = 0; x < _cols; x++)
        {
            Set(x, 0, accentColor);
            Set(x, _rows - 1, accentColor);
        }
        for (int y = 0; y < _rows; y++)
        {
            Set(0, y, accentColor);
            Set(_cols - 1, y, accentColor);
        }

        // Croix centrale
        int cx = _cols / 2;
        int cy = _rows / 2;
        for (int x = 0; x < _cols; x++)
            Set(x, cy, accentColor);
        for (int y = 0; y < _rows; y++)
            Set(cx, y, accentColor);

        // Coins distincts (haut-gauche vert, bas-droit bleu) pour repérer l’orientation
        Set(1, 1, Color.green);
        Set(_cols - 2, _rows - 2, Color.blue);

        if (_entities != null)
        {
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _cols; col++)
                {
                    int? id = WallMapping.EntityIdForCell(row, col);
                    if (id == null) continue;
                    Color c = _pixels[row * _cols + col];
                    _entities.SetColorSilent(id.Value, (byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f));
                }
            }
        }

        if (_visualizer != null)
            _visualizer.ApplyDisplayPixels(_pixels);
    }

    void Set(int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= _cols || y >= _rows) return;
        _pixels[y * _cols + x] = c;
    }
}
