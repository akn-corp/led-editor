// Défilement « PALOMA RUMBA » en fonte 3×5, rangées or / rouge alternées.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class PalomaRumbaTextBehaviour : PlayableBehaviour
{
    public float bpm = 120f;
    public float maxSpeed = 12f;
    public Color32 goldColor = new Color32(230, 194, 41, 255);
    public Color32 redColor = new Color32(217, 0, 18, 255);

    private static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
    {
        { 'P', new[] { "###", "#.#", "###", "#..", "#.." } },
        { 'A', new[] { ".#.", "#.#", "###", "#.#", "#.#" } },
        { 'L', new[] { "#..", "#..", "#..", "#..", "###" } },
        { 'O', new[] { "###", "#.#", "#.#", "#.#", "###" } },
        { 'M', new[] { "#.#", "###", "###", "#.#", "#.#" } },
        { 'R', new[] { "##.", "#.#", "##.", "#.#", "#.#" } },
        { 'U', new[] { "#.#", "#.#", "#.#", "#.#", "###" } },
        { 'B', new[] { "##.", "#.#", "##.", "#.#", "##." } },
        { ' ', new[] { "...", "...", "...", "...", "..." } },
    };

    private static bool[][] _textBand;
    private static int _bandWidth;
    private Color[] _displayPixels;
    private LedWallVisualizer _visualizer;

    static PalomaRumbaTextBehaviour()
    {
        InitTextBand("PALOMA RUMBA ");
    }

    private static void InitTextBand(string msg)
    {
        var rows = new List<bool>[5];
        for (int i = 0; i < 5; i++) rows[i] = new List<bool>();

        foreach (char ch in msg)
        {
            var glyph = Font.ContainsKey(ch) ? Font[ch] : Font[' '];
            for (int r = 0; r < 5; r++)
            {
                foreach (char c in glyph[r])
                    rows[r].Add(c == '#');
                rows[r].Add(false);
            }
        }

        _bandWidth = rows[0].Count;
        _textBand = new bool[5][];
        for (int r = 0; r < 5; r++)
            _textBand[r] = rows[r].ToArray();
    }

    public void Apply(EntityManager entityManager, float clipTime, float clipDuration)
    {
        if (entityManager == null || !WallMapping.IsInitialized) return;

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
        if (_visualizer == null)
            _visualizer = Object.FindFirstObjectByType<LedWallVisualizer>();

        for (int y = 0; y < rows; y++)
        {
            int bandIndex = y / 6;
            int ry = y % 6;
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
                    if (ry < 5 && _bandWidth > 0 && _textBand != null)
                    {
                        int dir = (bandIndex % 2 == 0) ? 1 : -1;
                        // Modulo positif robuste (évite bx == _bandWidth / négatif avec float %)
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
