// Anim Paloma Rumba procédurale (sans vidéo).
// Mode Clash (1 s) : PALOMA ←→ RUMBA se percutent, flash, hold, explosion pixels.
// Mode Scroll : wallpaper lignes alternées (legacy).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public enum PalomaFontType
{
    Micro_3x5,
    Bold_8x12
}

public enum PalomaScrollDirection
{
    Left = -1,
    Right = 1,
}

public enum PalomaAnimMode
{
    Clash = 0,
    Scroll = 1,
}

public class PalomaRumbaTextBehaviour : PlayableBehaviour
{
    public PalomaAnimMode mode = PalomaAnimMode.Clash;

    public float bpm = 120f;
    public float maxSpeed = 14f;
    public Color32 goldColor = new Color32(0xFF, 0xD6, 0x00, 255);
    public Color32 redColor = new Color32(0xFF, 0xD6, 0x00, 255);
    public PalomaFontType fontType = PalomaFontType.Micro_3x5;
    public int bandHeight;
    public PalomaScrollDirection palomaDirection = PalomaScrollDirection.Right;
    public PalomaScrollDirection rumbaDirection = PalomaScrollDirection.Left;
    public float palomaSpeed;
    public float rumbaSpeed;
    public float rowPhase = 0.35f;
    public int glyphScale = 1;

    // Clash timing (fractions de la durée du clip, conçu pour 1 s)
    public float rushEnd = 0.38f;
    public float flashEnd = 0.46f;
    public float holdEnd = 0.58f;

    private Color[] _displayPixels;
    private LedWallVisualizer _visualizer;

    // Clash caches
    private bool[][] _bandPaloma;
    private bool[][] _bandRumba;
    private bool[][] _bandMerged;
    private int _pW, _rW, _mW, _glyphH;
    private int _cacheKey = int.MinValue;
    private Vector2[] _partsPos;
    private Vector2[] _partsVel;
    private bool _partsBuilt;
    private int _partsCols, _partsRows;

    public void Apply(EntityManager entityManager, float clipTime, float clipDuration)
    {
        Apply(entityManager, null, clipTime, clipDuration);
    }

    public void Apply(EntityManager entityManager, LedWallVisualizer visualizer, float clipTime, float clipDuration)
    {
        if (entityManager == null || !WallMapping.IsInitialized) return;

        if (visualizer != null) _visualizer = visualizer;
        if (_visualizer == null)
            _visualizer = RippleWaveBehaviour.FindLedWallVisualizer();
        if (_visualizer == null) return;

        if (mode == PalomaAnimMode.Clash)
            ApplyClash(entityManager, clipTime, clipDuration);
        else
            ApplyScroll(entityManager, clipTime, clipDuration);
    }

    // -------------------------------------------------------------------------
    // CLASH
    // -------------------------------------------------------------------------

    private void ApplyClash(EntityManager entityManager, float clipTime, float clipDuration)
    {
        int cols = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;
        EnsureBuffer(cols, rows);
        _visualizer.SetSuppressSingleUpdates(true);

        float dur = clipDuration > 0.01f ? clipDuration : 1f;
        float t = Mathf.Clamp01(clipTime / dur);

        EnsureClashBands(cols);

        ClearBlack();

        float re = Mathf.Clamp(rushEnd, 0.05f, 0.9f);
        float fe = Mathf.Clamp(flashEnd, re + 0.02f, 0.95f);
        float he = Mathf.Clamp(holdEnd, fe + 0.02f, 0.98f);

        int totalW = _mW;
        int originY = (rows - _glyphH) / 2;
        int mergedX = (cols - totalW) / 2;
        int palomaTargetX = mergedX;
        int rumbaTargetX = mergedX + _pW; // collé (PALOMARUMBA)

        if (t < re)
        {
            float u = EaseOutCubic(t / re);
            int pX = Mathf.RoundToInt(Mathf.Lerp(-_pW, palomaTargetX, u));
            int rX = Mathf.RoundToInt(Mathf.Lerp(cols, rumbaTargetX, u));
            BlitBand(_bandPaloma, pX, originY, goldColor, 1f, cols, rows);
            BlitBand(_bandRumba, rX, originY, goldColor, 1f, cols, rows);
        }
        else if (t < fe)
        {
            // Flash blanc + texte fusionné
            float flashU = (t - re) / Mathf.Max(0.001f, fe - re);
            BlitBand(_bandMerged, mergedX, originY, goldColor, 1f, cols, rows);
            float flash = flashU < 0.55f ? 1f : 1f - (flashU - 0.55f) / 0.45f;
            if (flash > 0.05f)
                AddFlash(cols, rows, flash);
            _partsBuilt = false;
        }
        else if (t < he)
        {
            BlitBand(_bandMerged, mergedX, originY, goldColor, 1f, cols, rows);
            _partsBuilt = false;
        }
        else
        {
            float u = (t - he) / Mathf.Max(0.001f, 1f - he);
            EnsureParticles(cols, rows, mergedX, originY);
            DrawParticles(u, cols, rows);
        }

        Push(entityManager, cols, rows);
    }

    private void EnsureClashBands(int cols)
    {
        int scale = Mathf.Clamp(glyphScale, 1, 3);
        if (cols >= 96 && scale < 2) scale = 2; // plus lisible en 128
        if (cols <= 48) scale = Mathf.Min(scale, 2);

        int key = scale * 10 + (int)fontType + cols * 1000;
        if (_cacheKey == key && _bandMerged != null) return;
        _cacheKey = key;
        _partsBuilt = false;

        PalomaFontType effective = fontType;
        if (cols <= 48) effective = PalomaFontType.Micro_3x5;

        _bandPaloma = ScaleBand(BuildBand("PALOMA", effective), scale);
        _bandRumba = ScaleBand(BuildBand("RUMBA", effective), scale);
        _bandMerged = ScaleBand(BuildBand("PALOMARUMBA", effective), scale);
        _pW = _bandPaloma[0].Length;
        _rW = _bandRumba[0].Length;
        _mW = _bandMerged[0].Length;
        _glyphH = _bandMerged.Length;
    }

    private void EnsureParticles(int cols, int rows, int mergedX, int originY)
    {
        if (_partsBuilt && _partsCols == cols && _partsRows == rows) return;
        _partsCols = cols;
        _partsRows = rows;

        var pos = new List<Vector2>(256);
        var vel = new List<Vector2>(256);
        float cx = cols * 0.5f;
        float cy = rows * 0.5f;
        var rng = new System.Random(1337);

        for (int gy = 0; gy < _glyphH; gy++)
        for (int gx = 0; gx < _mW; gx++)
        {
            if (!_bandMerged[gy][gx]) continue;
            float x = mergedX + gx + 0.5f;
            float y = originY + gy + 0.5f;
            // Sous-échantillonner un peu sur grands murs
            if (cols >= 96 && ((gx + gy) & 1) != 0) continue;

            pos.Add(new Vector2(x, y));
            Vector2 dir = new Vector2(x - cx, y - cy);
            if (dir.sqrMagnitude < 0.01f)
                dir = new Vector2((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1));
            dir.Normalize();
            float jitter = 0.7f + (float)rng.NextDouble() * 0.8f;
            float spin = ((float)rng.NextDouble() - 0.5f) * 0.35f;
            vel.Add(dir * jitter + new Vector2(-dir.y, dir.x) * spin);
        }

        _partsPos = pos.ToArray();
        _partsVel = vel.ToArray();
        _partsBuilt = true;
    }

    private void DrawParticles(float u, int cols, int rows)
    {
        if (_partsPos == null) return;
        // Ease-out explosion, opacity fade en fin
        float dist = EaseOutCubic(u) * Mathf.Max(cols, rows) * 0.85f;
        float opacity = u < 0.55f ? 1f : 1f - (u - 0.55f) / 0.45f;
        opacity = Mathf.Clamp01(opacity);

        byte r = (byte)(goldColor.r * opacity);
        byte g = (byte)(goldColor.g * opacity);
        byte b = (byte)(goldColor.b * opacity);
        Color c = new Color(r / 255f, g / 255f, b / 255f, 1f);

        int stamp = cols >= 96 ? 2 : 1;

        for (int i = 0; i < _partsPos.Length; i++)
        {
            Vector2 p = _partsPos[i] + _partsVel[i] * dist;
            int px = Mathf.RoundToInt(p.x);
            int py = Mathf.RoundToInt(p.y);
            for (int sy = 0; sy < stamp; sy++)
            for (int sx = 0; sx < stamp; sx++)
            {
                int x = px + sx;
                int y = py + sy;
                if ((uint)x >= (uint)cols || (uint)y >= (uint)rows) continue;
                int texRow = rows - 1 - y;
                _displayPixels[texRow * cols + x] = c;
            }
        }
    }

    private void AddFlash(int cols, int rows, float amount)
    {
        amount = Mathf.Clamp01(amount) * 0.85f;
        for (int i = 0; i < _displayPixels.Length; i++)
        {
            Color c = _displayPixels[i];
            c.r = Mathf.Min(1f, c.r + amount);
            c.g = Mathf.Min(1f, c.g + amount);
            c.b = Mathf.Min(1f, c.b + amount);
            _displayPixels[i] = c;
        }
    }

    // -------------------------------------------------------------------------
    // SCROLL (legacy wallpaper)
    // -------------------------------------------------------------------------

    private void ApplyScroll(EntityManager entityManager, float clipTime, float clipDuration)
    {
        int cols = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;
        EnsureBuffer(cols, rows);
        _visualizer.SetSuppressSingleUpdates(true);

        PalomaFontType effectiveFont = fontType;
        if (cols <= 48 && fontType == PalomaFontType.Bold_8x12)
            effectiveFont = PalomaFontType.Micro_3x5;

        int scale = Mathf.Clamp(glyphScale, 1, 2);
        int key = -1000 - scale - (int)effectiveFont;
        if (_cacheKey != key || _bandPaloma == null)
        {
            _cacheKey = key;
            _bandPaloma = ScaleBand(BuildBand("PALOMA ", effectiveFont), scale);
            _bandRumba = ScaleBand(BuildBand("RUMBA ", effectiveFont), scale);
            _pW = _bandPaloma[0].Length;
            _rW = _bandRumba[0].Length;
            _glyphH = _bandPaloma.Length;
        }

        float duration = clipDuration > 0f ? clipDuration : 14f;
        float fade = Mathf.Min(1.0f, duration * 0.1f);
        float opacity = 1f;
        if (clipTime < fade) opacity = Smooth01(clipTime / fade);
        else if (clipTime > duration - fade) opacity = Smooth01((duration - clipTime) / fade);

        float speedP = palomaSpeed > 0.01f ? palomaSpeed : maxSpeed;
        float speedR = rumbaSpeed > 0.01f ? rumbaSpeed : maxSpeed;
        float scrollP = clipTime * speedP;
        float scrollR = clipTime * speedR;
        int dirP = (int)palomaDirection == 0 ? 1 : (int)palomaDirection;
        int dirR = (int)rumbaDirection == 0 ? -1 : (int)rumbaDirection;

        int lineH = _glyphH;
        if (bandHeight > _glyphH) lineH = bandHeight;
        float phaseFrac = Mathf.Clamp01(rowPhase);

        ClearBlack();
        for (int y = 0; y < rows; y++)
        {
            int lineIndex = y / lineH;
            int ry = y % lineH;
            bool isPaloma = (lineIndex % 2) == 0;
            bool[][] band = isPaloma ? _bandPaloma : _bandRumba;
            int bandW = isPaloma ? _pW : _rW;
            int dir = isPaloma ? dirP : dirR;
            float scroll = (isPaloma ? scrollP : scrollR) + lineIndex * bandW * phaseFrac;
            Color32 color = isPaloma ? goldColor : redColor;

            if (ry >= _glyphH || band == null) continue;
            for (int x = 0; x < cols; x++)
            {
                int bx = Mod(Mathf.FloorToInt(x - dir * scroll), bandW);
                if (!band[ry][bx]) continue;
                int texRow = rows - 1 - y;
                byte r = (byte)(color.r * opacity);
                byte g = (byte)(color.g * opacity);
                byte b = (byte)(color.b * opacity);
                _displayPixels[texRow * cols + x] = new Color(r / 255f, g / 255f, b / 255f, 1f);
            }
        }

        Push(entityManager, cols, rows);
    }

    // -------------------------------------------------------------------------
    // Drawing helpers
    // -------------------------------------------------------------------------

    private void BlitBand(bool[][] band, int originX, int originY, Color32 color, float opacity, int cols, int rows)
    {
        if (band == null) return;
        int h = band.Length;
        int w = band[0].Length;
        byte r = (byte)(color.r * opacity);
        byte g = (byte)(color.g * opacity);
        byte b = (byte)(color.b * opacity);
        Color c = new Color(r / 255f, g / 255f, b / 255f, 1f);

        for (int gy = 0; gy < h; gy++)
        {
            int y = originY + gy;
            if ((uint)y >= (uint)rows) continue;
            int texRow = rows - 1 - y;
            for (int gx = 0; gx < w; gx++)
            {
                if (!band[gy][gx]) continue;
                int x = originX + gx;
                if ((uint)x >= (uint)cols) continue;
                _displayPixels[texRow * cols + x] = c;
            }
        }
    }

    private void ClearBlack()
    {
        for (int i = 0; i < _displayPixels.Length; i++)
            _displayPixels[i] = Color.black;
    }

    private void Push(EntityManager entityManager, int cols, int rows)
    {
        for (int y = 0; y < rows; y++)
        {
            int texRow = rows - 1 - y;
            for (int x = 0; x < cols; x++)
            {
                int? id = WallMapping.EntityIdForCell(y, x);
                if (!id.HasValue) continue;
                Color c = _displayPixels[texRow * cols + x];
                entityManager.SetColorSilent(id.Value,
                    (byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f));
            }
        }
        _visualizer.ApplyDisplayPixels(_displayPixels);
    }

    private void EnsureBuffer(int cols, int rows)
    {
        int n = cols * rows;
        if (_displayPixels == null || _displayPixels.Length != n)
            _displayPixels = new Color[n];
    }

    private static float EaseOutCubic(float u)
    {
        u = Mathf.Clamp01(u);
        float inv = 1f - u;
        return 1f - inv * inv * inv;
    }

    private static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static int Mod(int a, int m)
    {
        int r = a % m;
        return r < 0 ? r + m : r;
    }

    private static bool[][] ScaleBand(bool[][] src, int scale)
    {
        if (scale <= 1 || src == null) return src;
        int h = src.Length;
        int w = src[0].Length;
        var dst = new bool[h * scale][];
        for (int r = 0; r < h * scale; r++)
            dst[r] = new bool[w * scale];
        for (int r = 0; r < h; r++)
        for (int c = 0; c < w; c++)
        {
            if (!src[r][c]) continue;
            for (int sy = 0; sy < scale; sy++)
            for (int sx = 0; sx < scale; sx++)
                dst[r * scale + sy][c * scale + sx] = true;
        }
        return dst;
    }

    private static bool[][] BuildBand(string msg, PalomaFontType _)
    {
        // Même police que EntityText (LET'S / GO)
        return LedBitmapFont.BuildBand3x5(msg ?? " ");
    }
}
