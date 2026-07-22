// Paint texte sur le mur LED (police OS → EntityManager + LedWallVisualizer).
// Utilisé par KineticTypographyAnimator (démo) et EntityTextTrack (montage Timeline).
//
// Timeline : binder ce composant (sur LedWall) à une piste Entity Text Track.

using UnityEngine;

[RequireComponent(typeof(LedWallVisualizer))]
public class LedWallTextPainter : MonoBehaviour
{
    private const int FontPx = 64;

    private EntityManager _entityManager;
    private LedWallVisualizer _visualizer;
    private int _columns;
    private int _rows;
    private int?[,] _entityGrid;
    private Color[] _displayPixels;
    private Font _font;
    private Texture2D _fontReadable;
    private bool _ready;

    // Prêt seulement si l'initialisation a réellement produit les buffers. Évite une
    // NullReferenceException si le composant est reconstruit (rebuild Timeline / reload de
    // domaine éditeur) alors que _ready valait encore true.
    public bool IsReady =>
        _ready && _entityGrid != null && _displayPixels != null
        && _entityManager != null && _visualizer != null;

    public void Initialize(EntityManager entityManager, LedWallVisualizer visualizer, int columns)
    {
        _entityManager = entityManager;
        _visualizer = visualizer != null ? visualizer : GetComponent<LedWallVisualizer>();
        _columns = columns;
        _rows = WallMapping.VisibleRows;

        _entityGrid = new int?[_rows, _columns];
        for (int row = 0; row < _rows; row++)
        for (int col = 0; col < _columns; col++)
            _entityGrid[row, col] = WallMapping.EntityIdForCell(row, col);

        _displayPixels = new Color[_columns * _rows];
        _visualizer.SetSuppressSingleUpdates(true);

        _font = Font.CreateDynamicFontFromOSFont(
            new[] { "Arial Black", "Helvetica Bold", "Arial Bold", "Impact", "Arial", "Helvetica" },
            FontPx);

        PrefetchAscii();
        Clear(Color.black);
        _ready = true;
    }

    void OnDestroy()
    {
        if (_fontReadable != null)
            Destroy(_fontReadable);
    }

    /// <summary>Remplit le mur avec une couleur unie et pousse vers preview + state.</summary>
    public void Clear(Color background)
    {
        if (!_ready && _displayPixels == null) return;
        EnsureBuffers();
        for (int i = 0; i < _displayPixels.Length; i++)
            _displayPixels[i] = background;
        PushToWall();
    }

    /// <summary>
    /// Efface avec <paramref name="background"/> puis dessine le texte.
    /// </summary>
    public void PaintText(
        string text,
        Vector2 positionNorm,
        float sizeFrac,
        Color color,
        float opacity,
        Color background,
        bool fitToWall = true)
    {
        if (!EnsureBuffers()) return;

        for (int i = 0; i < _displayPixels.Length; i++)
            _displayPixels[i] = background;

        if (!string.IsNullOrEmpty(text) && opacity > 0.02f)
            DrawText(text, positionNorm, sizeFrac, color, opacity, fitToWall);

        PushToWall();
    }

    /// <summary>Dessine plusieurs lignes sur le même fond (démo Kinetic).</summary>
    public void BeginFrame(Color background)
    {
        if (!EnsureBuffers()) return;
        for (int i = 0; i < _displayPixels.Length; i++)
            _displayPixels[i] = background;
    }

    public void DrawTextLayer(string text, Vector2 positionNorm, float sizeFrac, Color color, float opacity, bool fitToWall = true)
    {
        if (!EnsureBuffers()) return;
        if (string.IsNullOrEmpty(text) || opacity < 0.02f) return;
        DrawText(text, positionNorm, sizeFrac, color, opacity, fitToWall);
    }

    public void EndFrame()
    {
        if (!EnsureBuffers()) return;
        PushToWall();
    }

    public void PrefetchCharacters(string chars)
    {
        if (_font == null || string.IsNullOrEmpty(chars)) return;
        _font.RequestCharactersInTexture(chars, FontPx, FontStyle.Bold);
        RefreshFontReadable();
    }

    private bool EnsureBuffers()
    {
        if (_displayPixels == null || _entityGrid == null
            || _entityManager == null || _visualizer == null)
            return false;
        if (_fontReadable == null && _font != null)
            RefreshFontReadable();
        return true;
    }

    private void PrefetchAscii()
    {
        if (_font == null) return;
        const string set =
            " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?'-:\"";
        _font.RequestCharactersInTexture(set, FontPx, FontStyle.Bold);
        RefreshFontReadable();
    }

    private void RefreshFontReadable()
    {
        if (_font == null || _font.material == null || _font.material.mainTexture == null)
            return;

        var src = _font.material.mainTexture;
        int w = src.width;
        int h = src.height;
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);
        if (_fontReadable == null || _fontReadable.width != w || _fontReadable.height != h)
        {
            if (_fontReadable != null) Destroy(_fontReadable);
            _fontReadable = new Texture2D(w, h, TextureFormat.RGBA32, false);
        }
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        _fontReadable.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
        _fontReadable.Apply(false);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
    }

    private void DrawText(string text, Vector2 centerNorm, float sizeFrac, Color color, float opacity, bool fitToWall)
    {
        if (_font == null || _fontReadable == null) return;

        _font.RequestCharactersInTexture(text, FontPx, FontStyle.Bold);
        // Atlas peut grandir : refresh si besoin (taille changée)
        if (_font.material.mainTexture != null &&
            (_fontReadable.width != _font.material.mainTexture.width ||
             _fontReadable.height != _font.material.mainTexture.height))
            RefreshFontReadable();

        var infos = new CharacterInfo[text.Length];
        float totalAdvance = 0f;
        float maxH = 0f;
        for (int i = 0; i < text.Length; i++)
        {
            if (!_font.GetCharacterInfo(text[i], out infos[i], FontPx, FontStyle.Bold))
                _font.GetCharacterInfo(text[i], out infos[i], FontPx, FontStyle.Normal);

            float adv = infos[i].advance;
            float boxW = Mathf.Abs(infos[i].maxX - infos[i].minX);
            if (adv < boxW * 0.35f)
                adv = boxW * 0.85f;
            totalAdvance += Mathf.Max(adv, 1f);
            maxH = Mathf.Max(maxH, Mathf.Abs(infos[i].maxY - infos[i].minY), infos[i].glyphHeight);
        }
        if (totalAdvance < 1f) totalAdvance = 1f;
        if (maxH < 1f) maxH = FontPx;

        float targetH = sizeFrac * _rows;
        float scale = targetH / maxH;

        if (fitToWall)
        {
            float maxW = _columns * 0.88f;
            float textW = totalAdvance * scale;
            if (textW > maxW)
                scale *= maxW / textW;
        }

        float textWFinal = totalAdvance * scale;
        float textH = maxH * scale;

        float originX = centerNorm.x * (_columns - 1) - textWFinal * 0.5f;
        float originY = centerNorm.y * (_rows - 1) - textH * 0.5f;

        Color fg = color;
        fg.a = opacity;

        float penX = originX;
        for (int i = 0; i < text.Length; i++)
        {
            CharacterInfo info = infos[i];
            StampGlyph(info, penX, originY, scale, fg);

            float adv = info.advance;
            float boxW = Mathf.Abs(info.maxX - info.minX);
            if (adv < boxW * 0.35f)
                adv = boxW * 0.85f;
            penX += Mathf.Max(adv, 1f) * scale;
        }
    }

    private void StampGlyph(CharacterInfo info, float penX, float originY, float scale, Color fg)
    {
        float boxW = Mathf.Abs(info.maxX - info.minX);
        float boxH = Mathf.Abs(info.maxY - info.minY);
        if (boxW < 0.5f) boxW = Mathf.Max(info.advance, 1f);
        if (boxH < 0.5f) boxH = FontPx;

        int gw = Mathf.Max(1, Mathf.RoundToInt(boxW * scale));
        int gh = Mathf.Max(1, Mathf.RoundToInt(boxH * scale));

        int dx = Mathf.RoundToInt(penX + info.minX * scale);
        int dy = Mathf.RoundToInt(originY + info.minY * scale);

        Vector2 uvBL = info.uvBottomLeft;
        Vector2 uvBR = info.uvBottomRight;
        Vector2 uvTL = info.uvTopLeft;
        Vector2 uvTR = info.uvTopRight;

        for (int y = 0; y < gh; y++)
        {
            float ty = (y + 0.5f) / gh;
            for (int x = 0; x < gw; x++)
            {
                float tx = (x + 0.5f) / gw;
                float u = Mathf.Lerp(
                    Mathf.Lerp(uvBL.x, uvBR.x, tx),
                    Mathf.Lerp(uvTL.x, uvTR.x, tx),
                    ty);
                float v = Mathf.Lerp(
                    Mathf.Lerp(uvBL.y, uvBR.y, tx),
                    Mathf.Lerp(uvTL.y, uvTR.y, tx),
                    ty);

                Color sample = _fontReadable.GetPixelBilinear(u, v);
                float a = sample.a * fg.a;
                if (a < 0.05f)
                    a = Mathf.Max(sample.r, sample.g, sample.b) * fg.a;
                if (a < 0.08f) continue;

                SetPixelBlend(dx + x, dy + y, fg, a);
            }
        }
    }

    private void SetPixelBlend(int x, int y, Color color, float a)
    {
        if (x < 0 || x >= _columns || y < 0 || y >= _rows) return;
        int idx = y * _columns + x;
        Color dst = _displayPixels[idx];
        _displayPixels[idx] = Color.Lerp(dst, new Color(color.r, color.g, color.b, 1f), a);
    }

    private void PushToWall()
    {
        if (_entityGrid == null || _displayPixels == null
            || _entityManager == null || _visualizer == null) return;

        for (int row = 0; row < _rows; row++)
        {
            int texRow = _rows - 1 - row;
            for (int col = 0; col < _columns; col++)
            {
                Color rgb = _displayPixels[texRow * _columns + col];
                int? entityId = _entityGrid[row, col];
                if (!entityId.HasValue) continue;

                byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.r * 255f), 0, 255);
                byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.g * 255f), 0, 255);
                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(rgb.b * 255f), 0, 255);
                _entityManager.SetColorSilent(entityId.Value, r, g, b);
            }
        }

        _visualizer.ApplyDisplayPixels(_displayPixels);
    }
}
