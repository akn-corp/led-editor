// Applique une frame texture (GIF / image) sur le mur LED.

using UnityEngine;
using UnityEngine.Playables;

public class WallMediaBehaviour : PlayableBehaviour
{
    public WallMediaSequence sequence;
    public Texture2D[] framesOverride;
    public float framesPerSecond = 12f;
    public bool loop = true;
    public FilterMode sampleFilter = FilterMode.Point;
    public float brightness = 1f;
    public Color backgroundColor = Color.black;

    private Color[] _displayPixels;
    private Texture2D _readableScratch;
    private int _scratchW;
    private int _scratchH;

    public void Apply(EntityManager entityManager, LedWallVisualizer visualizer, float clipTime)
    {
        if (entityManager == null || !WallMapping.IsInitialized) return;
        if (visualizer == null)
            visualizer = RippleWaveBehaviour.FindLedWallVisualizer();
        if (visualizer == null) return;

        Texture2D frame = ResolveFrame(clipTime);
        int cols = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;
        EnsureBuffer(cols, rows);

        visualizer.SetSuppressSingleUpdates(true);

        if (frame == null)
        {
            for (int i = 0; i < _displayPixels.Length; i++)
                _displayPixels[i] = backgroundColor;
        }
        else
        {
            Texture2D src = EnsureReadable(frame);
            SampleNearest(src, cols, rows);
        }

        for (int row = 0; row < rows; row++)
        {
            int texRow = rows - 1 - row;
            for (int col = 0; col < cols; col++)
            {
                Color rgb = _displayPixels[texRow * cols + col];
                rgb *= brightness;
                _displayPixels[texRow * cols + col] = rgb;

                int? entityId = WallMapping.EntityIdForCell(row, col);
                if (!entityId.HasValue) continue;

                byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.r * 255f), 0, 255);
                byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.g * 255f), 0, 255);
                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.b * 255f), 0, 255);
                entityManager.SetColorSilent(entityId.Value, r, g, b);
            }
        }

        visualizer.ApplyDisplayPixels(_displayPixels);
    }

    Texture2D ResolveFrame(float clipTime)
    {
        if (sequence != null && sequence.FrameCount > 0)
            return sequence.GetFrameAtTime(clipTime);

        if (framesOverride == null || framesOverride.Length == 0)
            return null;

        float fps = Mathf.Max(0.01f, framesPerSecond);
        int index;
        if (loop)
        {
            index = Mathf.FloorToInt(clipTime * fps) % framesOverride.Length;
            if (index < 0) index += framesOverride.Length;
        }
        else
            index = Mathf.Clamp(Mathf.FloorToInt(clipTime * fps), 0, framesOverride.Length - 1);

        return framesOverride[index];
    }

    void SampleNearest(Texture2D src, int cols, int rows)
    {
        int sw = src.width;
        int sh = src.height;
        if (sw < 1 || sh < 1) return;

        // Point sampling : chaque LED = un texel (ou moyenne du bloc si source plus grande)
        for (int row = 0; row < rows; row++)
        {
            int texRow = rows - 1 - row;
            // Unity texture (0,0) = bas-gauche ; mur row 0 = haut
            float v = (row + 0.5f) / rows;
            int sy = Mathf.Clamp(Mathf.FloorToInt((1f - v) * sh), 0, sh - 1);

            for (int col = 0; col < cols; col++)
            {
                float u = (col + 0.5f) / cols;
                int sx = Mathf.Clamp(Mathf.FloorToInt(u * sw), 0, sw - 1);
                Color c = src.GetPixel(sx, sy);
                if (c.a < 0.05f)
                    c = backgroundColor;
                else
                {
                    c.r = Mathf.Lerp(backgroundColor.r, c.r, c.a);
                    c.g = Mathf.Lerp(backgroundColor.g, c.g, c.a);
                    c.b = Mathf.Lerp(backgroundColor.b, c.b, c.a);
                    c.a = 1f;
                    // GIF "transparence" parfois bakée en damier gris très sombre (~5/255)
                    if (c.r <= 0.09f && c.g <= 0.09f && c.b <= 0.09f)
                        c = backgroundColor;
                }
                _displayPixels[texRow * cols + col] = c;
            }
        }
    }

    Texture2D EnsureReadable(Texture2D src)
    {
        if (src == null) return null;
        try
        {
            src.GetPixel(0, 0);
            return src;
        }
        catch
        {
            // Texture non readable : blit vers scratch
            int w = src.width;
            int h = src.height;
            if (_readableScratch == null || _scratchW != w || _scratchH != h)
            {
                if (_readableScratch != null)
                    Object.DestroyImmediate(_readableScratch);
                _readableScratch = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    filterMode = sampleFilter,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _scratchW = w;
                _scratchH = h;
            }

            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            var prevFilter = src.filterMode;
            src.filterMode = sampleFilter;
            Graphics.Blit(src, rt);
            src.filterMode = prevFilter;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            _readableScratch.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            _readableScratch.Apply(false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return _readableScratch;
        }
    }

    void EnsureBuffer(int cols, int rows)
    {
        int n = cols * rows;
        if (_displayPixels == null || _displayPixels.Length != n)
            _displayPixels = new Color[n];
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (_readableScratch != null)
        {
            Object.DestroyImmediate(_readableScratch);
            _readableScratch = null;
        }
    }
}
