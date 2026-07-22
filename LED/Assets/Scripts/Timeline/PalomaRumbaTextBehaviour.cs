// Assets/Scripts/Timeline/PalomaRumbaTextBehaviour.cs
//
// Animation de défilement de texte "PALOMA RUMBA" en couleurs Or / Rouge (Option A)
// Supporte le choix entre micro-pixel (3x5) et grand format gras lisible (8x12).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public enum PalomaFontType
{
    Micro_3x5,
    Bold_8x12
}

public class PalomaRumbaTextBehaviour : PlayableBehaviour
{
    public float bpm = 120f;
    public float maxSpeed = 12f;
    public Color32 goldColor = new Color32(230, 194, 41, 255);
    public Color32 redColor = new Color32(217, 0, 18, 255);
    public PalomaFontType fontType = PalomaFontType.Bold_8x12;
    public int bandHeight = 16;

    // Font 3x5 d'origine
    private static readonly Dictionary<char, string[]> Font3x5 = new Dictionary<char, string[]>
    {
        { 'P', new[] { "###", "#.#", "###", "#..", "#.." } },
        { 'A', new[] { ".#.", "#.#", "###", "#.#", "#.#" } },
        { 'L', new[] { "#..", "#..", "#..", "#..", "###" } },
        { 'O', new[] { "###", "#.#", "#.#", "#.#", "###" } },
        { 'M', new[] { "#.#", "###", "###", "#.#", "#.#" } },
        { 'R', new[] { "##.", "#.#", "##.", "#.#", "#.#" } },
        { 'U', new[] { "#.#", "#.#", "#.#", "#.#", "###" } },
        { 'B', new[] { "##.", "#.#", "##.", "#.#", "##." } },
        { ' ', new[] { "...", "...", "...", "...", "..." } }
    };

    // Font 8x12 Gras / Haute Visibilité
    private static readonly Dictionary<char, byte[]> Font8x12 = new Dictionary<char, byte[]>
    {
        { 'P', new byte[] { 0xFC, 0xFE, 0xC3, 0xC3, 0xFE, 0xFC, 0xC0, 0xC0, 0xC0, 0xC0, 0xC0, 0xC0 } },
        { 'A', new byte[] { 0x3C, 0x7E, 0xC3, 0xC3, 0xFF, 0xFF, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3 } },
        { 'L', new byte[] { 0xC0, 0xC0, 0xC0, 0xC0, 0xC0, 0xC0, 0xC0, 0xC0, 0xC0, 0xC0, 0xFF, 0xFF } },
        { 'O', new byte[] { 0x3C, 0x7E, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0x7E, 0x3C } },
        { 'M', new byte[] { 0xC3, 0xE7, 0xFF, 0xFF, 0xDB, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3 } },
        { 'R', new byte[] { 0xFC, 0xFE, 0xC3, 0xC3, 0xFE, 0xFC, 0xCC, 0xCC, 0xC6, 0xC6, 0xC3, 0xC3 } },
        { 'U', new byte[] { 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0x7E, 0x3C } },
        { 'B', new byte[] { 0xFC, 0xFE, 0xC3, 0xC3, 0xFE, 0xFE, 0xC3, 0xC3, 0xC3, 0xC3, 0xFE, 0xFC } },
        { ' ', new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } }
    };

    private bool[][] _textBand;
    private int _bandWidth;
    private PalomaFontType _cachedFontType = (PalomaFontType)(-1);
    private Color[] _displayPixels;
    private LedWallVisualizer _visualizer;

    private void InitTextBand(string msg)
    {
        int charHeight = (fontType == PalomaFontType.Bold_8x12) ? 12 : 5;
        var rows = new List<bool>[charHeight];
        for (int i = 0; i < charHeight; i++) rows[i] = new List<bool>();

        foreach (char ch in msg)
        {
            if (fontType == PalomaFontType.Bold_8x12)
            {
                byte[] glyph = Font8x12.ContainsKey(ch) ? Font8x12[ch] : Font8x12[' '];
                for (int r = 0; r < 12; r++)
                {
                    byte val = glyph[r];
                    for (int col = 7; col >= 0; col--)
                    {
                        rows[r].Add((val & (1 << col)) != 0);
                    }
                    rows[r].Add(false); // Espace inter-caractères
                }
            }
            else
            {
                string[] glyph = Font3x5.ContainsKey(ch) ? Font3x5[ch] : Font3x5[' '];
                for (int r = 0; r < 5; r++)
                {
                    foreach (char c in glyph[r])
                        rows[r].Add(c == '#');
                    rows[r].Add(false); // Espace inter-caractères
                }
            }
        }

        _bandWidth = rows[0].Count;
        _textBand = new bool[charHeight][];
        for (int r = 0; r < charHeight; r++)
        {
            _textBand[r] = rows[r].ToArray();
        }
    }

    public void Apply(EntityManager entityManager, float clipTime, float clipDuration)
    {
        Apply(entityManager, null, clipTime, clipDuration);
    }

    public void Apply(EntityManager entityManager, LedWallVisualizer visualizer, float clipTime, float clipDuration)
    {
        if (entityManager == null || !WallMapping.IsInitialized) return;

        // Lazy-init de la bande si le type de police change
        if (_cachedFontType != fontType || _textBand == null)
        {
            _cachedFontType = fontType;
            InitTextBand("PALOMA RUMBA ");
        }

        float duration = clipDuration > 0f ? clipDuration : 14f;
        float fade = Mathf.Min(1.5f, duration * 0.15f);

        float opacity = 1f;
        float speedFactor = 1f;
        if (clipTime < fade)
        {
            opacity = Mathf.Clamp01(clipTime / fade);
            speedFactor = opacity * opacity;
        }
        else if (clipTime > duration - fade)
        {
            opacity = Mathf.Clamp01((duration - clipTime) / fade);
            speedFactor = opacity;
        }

        float accumulatedScroll = clipTime * maxSpeed * speedFactor;
        float beat = clipTime * bpm / 60f;
        bool isFlash = clipTime >= fade && clipTime <= duration - fade && (beat % 16f) > 15.2f;

        int cols = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;
        EnsureBuffer(cols, rows);

        if (visualizer != null)
            _visualizer = visualizer;
        if (_visualizer == null)
            _visualizer = Object.FindFirstObjectByType<LedWallVisualizer>();
        if (_visualizer == null)
        {
            Debug.LogWarning("[PalomaRumba] LedWallVisualizer introuvable — mur pas encore construit.");
            return;
        }
        _visualizer.SetSuppressSingleUpdates(true);

        int charHeight = (fontType == PalomaFontType.Bold_8x12) ? 12 : 5;
        int currentBandHeight = Mathf.Max(charHeight + 1, bandHeight);

        for (int y = 0; y < rows; y++)
        {
            int bandIndex = y / currentBandHeight;
            int ry = y % currentBandHeight;
            Color32 primaryColor = (bandIndex % 2 == 0) ? goldColor : redColor;
            int texRow = rows - 1 - y;

            for (int x = 0; x < cols; x++)
            {
                int? entityId = WallMapping.EntityIdForCell(y, x);
                byte r, g, b;

                if (isFlash)
                {
                    r = (byte)(primaryColor.r * opacity);
                    g = (byte)(primaryColor.g * opacity);
                    b = (byte)(primaryColor.b * opacity);
                }
                else
                {
                    bool on = false;
                    if (ry < charHeight && _bandWidth > 0 && _textBand != null)
                    {
                        int dir = (bandIndex % 2 == 0) ? 1 : -1;
                        int bx = Mathf.FloorToInt(x + accumulatedScroll * dir) % _bandWidth;
                        if (bx < 0) bx += _bandWidth;
                        if ((uint)bx < (uint)_textBand[ry].Length)
                            on = _textBand[ry][bx];
                    }

                    if (on)
                    {
                        r = (byte)(primaryColor.r * opacity);
                        g = (byte)(primaryColor.g * opacity);
                        b = (byte)(primaryColor.b * opacity);
                    }
                    else
                    {
                        r = (byte)(6 * opacity);
                        g = (byte)(6 * opacity);
                        b = (byte)(10 * opacity);
                    }
                }

                _displayPixels[texRow * cols + x] = new Color(r / 255f, g / 255f, b / 255f, 1f);

                if (entityId.HasValue)
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
