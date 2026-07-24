// Paint texte sur le mur LED (EntityManager + LedWallVisualizer).
// 128×128 : police OS (TrueType). Viewport bas-résolution (≤64) : police
// bitmap 5×7 / 3×5 crisp + centrage exact (même logique que le GIF).

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LedWallVisualizer))]
public class LedWallTextPainter : MonoBehaviour
{
    private const int FontPx = 64;
    private const int PixelModeMaxRows = 64;

    private EntityManager _entityManager;
    private LedWallVisualizer _visualizer;
    private int _columns;
    private int _rows;
    private int?[,] _entityGrid;
    private Color[] _displayPixels;
    private Font _font;
    private Texture2D _fontReadable;
    private bool _ready;

    public bool IsReady =>
        _ready && _entityGrid != null && _displayPixels != null
        && _entityManager != null && _visualizer != null;

    private bool UsePixelMode => _rows > 0 && _rows <= PixelModeMaxRows;

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

    public void Clear(Color background)
    {
        if (!_ready && _displayPixels == null) return;
        EnsureBuffers();
        for (int i = 0; i < _displayPixels.Length; i++)
            _displayPixels[i] = background;
        PushToWall();
    }

    public void PaintText(
        string text,
        Vector2 positionNorm,
        float sizeFrac,
        Color color,
        float opacity,
        Color background,
        bool fitToWall = true,
        int pixelScale = 1)
    {
        if (!EnsureBuffers()) return;

        for (int i = 0; i < _displayPixels.Length; i++)
            _displayPixels[i] = background;

        if (!string.IsNullOrEmpty(text) && opacity > 0.02f)
        {
            if (UsePixelMode)
                DrawTextBitmap(text, positionNorm, color, opacity, fitToWall, pixelScale);
            else
                DrawTextTrueType(text, positionNorm, sizeFrac, color, opacity, fitToWall);
        }

        PushToWall();
    }

    public void BeginFrame(Color background)
    {
        if (!EnsureBuffers()) return;
        for (int i = 0; i < _displayPixels.Length; i++)
            _displayPixels[i] = background;
    }

    public void DrawTextLayer(
        string text,
        Vector2 positionNorm,
        float sizeFrac,
        Color color,
        float opacity,
        bool fitToWall = true,
        int pixelScale = 1)
    {
        if (!EnsureBuffers()) return;
        if (string.IsNullOrEmpty(text) || opacity < 0.02f) return;
        if (UsePixelMode)
            DrawTextBitmap(text, positionNorm, color, opacity, fitToWall, pixelScale);
        else
            DrawTextTrueType(text, positionNorm, sizeFrac, color, opacity, fitToWall);
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
            _fontReadable = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
            };
        }
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        _fontReadable.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
        _fontReadable.Apply(false);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
    }

    // -------------------------------------------------------------------------
    // Mode pixel (≤64) — police bitmap + retour à la ligne.
    // Capacité 32×32 (3×5, gap 1) : ~8 caractères / ligne, ~5 lignes.
    // Textes Timeline : CAUSE, WANNA, I DON'T, TALK ABOUT, NUMBERS, ALL DAY LONG,
    // LET'S, BURN THE PHONE, DELETE MY NUMBER, GOOOOOOOOOO.
    // -------------------------------------------------------------------------

    private void DrawTextBitmap(
        string text,
        Vector2 centerNorm,
        Color color,
        float opacity,
        bool fitToWall,
        int pixelScale = 1)
    {
        string upper = text.ToUpperInvariant().Replace('\r', '\n');
        int scale = Mathf.Clamp(pixelScale, 1, 2);

        // 5×7 seulement si tout tient sur UNE ligne ; sinon 3×5 + wrap
        // Avec scale 2, on mesure en largeur « logique » (avant scale).
        int maxLogicalW = Mathf.Max(1, _columns / scale);
        bool fits5x7 = !upper.Contains('\n')
            && MeasureBitmapWidth(upper, LedBitmapFont.Font5x7, 5, 1) <= maxLogicalW;
        // Sur 32×32 en scale 2, forcer 3×5 pour garder des hooks lisibles
        if (scale >= 2)
            fits5x7 = false;

        var font = fits5x7 ? LedBitmapFont.Font5x7 : LedBitmapFont.Font3x5;
        int glyphW = fits5x7 ? 5 : 3;
        int glyphH = fits5x7 ? 7 : 5;
        int gap = 1;
        int lineGap = 1;
        int maxW = fitToWall ? maxLogicalW : maxLogicalW;

        List<string> lines = WrapBitmapLines(upper, font, glyphW, gap, maxW);

        int scaledGlyphH = glyphH * scale;
        int scaledLineGap = lineGap * scale;

        // Si trop de lignes verticalement : réduire l’interligne, puis tronquer
        int maxLines = Mathf.Max(1, (_rows + scaledLineGap) / (scaledGlyphH + scaledLineGap));
        if (lines.Count > maxLines && lineGap > 0)
        {
            lineGap = 0;
            scaledLineGap = 0;
            maxLines = Mathf.Max(1, _rows / scaledGlyphH);
        }
        if (lines.Count > maxLines)
            lines = lines.GetRange(0, maxLines);

        int blockH = lines.Count * scaledGlyphH + Mathf.Max(0, lines.Count - 1) * scaledLineGap;
        int blockW = 0;
        foreach (string line in lines)
            blockW = Mathf.Max(blockW, MeasureBitmapWidth(line, font, glyphW, gap) * scale);

        int originX = Mathf.RoundToInt(centerNorm.x * (_columns - 1) - (blockW - 1) * 0.5f);
        int originY = Mathf.RoundToInt(centerNorm.y * (_rows - 1) - (blockH - 1) * 0.5f);
        originX = Mathf.Clamp(originX, 0, Mathf.Max(0, _columns - blockW));
        originY = Mathf.Clamp(originY, 0, Mathf.Max(0, _rows - blockH));

        Color fg = color;
        fg.a = opacity;

        for (int li = 0; li < lines.Count; li++)
        {
            string line = lines[li];
            int lineW = MeasureBitmapWidth(line, font, glyphW, gap) * scale;
            // Chaque ligne centrée horizontalement dans le bloc
            int lineX = originX + (blockW - lineW) / 2;
            int lineY = originY + (lines.Count - 1 - li) * (scaledGlyphH + scaledLineGap);
            DrawBitmapLine(line, font, glyphW, glyphH, gap, lineX, lineY, fg, scale);
        }
    }

    private void DrawBitmapLine(
        string line,
        Dictionary<char, string[]> font,
        int glyphW,
        int glyphH,
        int gap,
        int originX,
        int originY,
        Color fg,
        int pixelScale = 1)
    {
        int scale = Mathf.Clamp(pixelScale, 1, 2);
        int penX = originX;
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == ' ')
            {
                penX += (glyphW / 2 + gap) * scale;
                continue;
            }

            if (!font.TryGetValue(ch, out string[] rows))
            {
                penX += (glyphW + gap) * scale;
                continue;
            }

            for (int gy = 0; gy < rows.Length && gy < glyphH; gy++)
            {
                string row = rows[gy];
                int basePy = originY + (glyphH - 1 - gy) * scale;
                for (int gx = 0; gx < row.Length && gx < glyphW; gx++)
                {
                    if (row[gx] != '#') continue;
                    int basePx = penX + gx * scale;
                    for (int sy = 0; sy < scale; sy++)
                    for (int sx = 0; sx < scale; sx++)
                        SetPixelBlend(basePx + sx, basePy + sy, fg, 1f);
                }
            }

            penX += (glyphW + gap) * scale;
        }
    }

    /// <summary>
    /// Word-wrap : coupe aux espaces ; un mot trop long est cassé au milieu.
    /// Respecte les \n déjà présents dans le texte.
    /// </summary>
    private static List<string> WrapBitmapLines(
        string text,
        Dictionary<char, string[]> font,
        int glyphW,
        int gap,
        int maxW)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            result.Add("");
            return result;
        }

        string[] paragraphs = text.Split('\n');
        foreach (string paragraph in paragraphs)
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                result.Add("");
                continue;
            }

            // Un seul mot sans espace qui dépasse → coupe dure
            if (MeasureBitmapWidth(paragraph, font, glyphW, gap) <= maxW
                && paragraph.IndexOf(' ') < 0)
            {
                result.Add(paragraph);
                continue;
            }

            string[] words = paragraph.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            var current = new System.Text.StringBuilder();

            foreach (string word in words)
            {
                if (MeasureBitmapWidth(word, font, glyphW, gap) > maxW)
                {
                    // Flush ligne en cours
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                    // Coupe le mot long
                    foreach (string chunk in HardBreakWord(word, font, glyphW, gap, maxW))
                        result.Add(chunk);
                    continue;
                }

                string candidate = current.Length == 0 ? word : current + " " + word;
                if (MeasureBitmapWidth(candidate, font, glyphW, gap) <= maxW)
                {
                    if (current.Length > 0) current.Append(' ');
                    current.Append(word);
                }
                else
                {
                    if (current.Length > 0)
                        result.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
            }

            if (current.Length > 0)
                result.Add(current.ToString());
        }

        if (result.Count == 0)
            result.Add("");
        return result;
    }

    private static List<string> HardBreakWord(
        string word,
        Dictionary<char, string[]> font,
        int glyphW,
        int gap,
        int maxW)
    {
        var chunks = new List<string>();
        var buf = new System.Text.StringBuilder();
        foreach (char ch in word)
        {
            string candidate = buf.ToString() + ch;
            if (buf.Length > 0 && MeasureBitmapWidth(candidate, font, glyphW, gap) > maxW)
            {
                chunks.Add(buf.ToString());
                buf.Clear();
            }
            buf.Append(ch);
        }
        if (buf.Length > 0)
            chunks.Add(buf.ToString());
        return chunks;
    }

    private static int MeasureBitmapWidth(string text, Dictionary<char, string[]> font, int glyphW, int gap)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int w = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ')
                w += glyphW / 2;
            else
                w += glyphW;
            if (i < text.Length - 1)
                w += gap;
        }
        return w;
    }

    // -------------------------------------------------------------------------
    // Mode TrueType (128×128)
    // -------------------------------------------------------------------------

    private void DrawTextTrueType(string text, Vector2 centerNorm, float sizeFrac, Color color, float opacity, bool fitToWall)
    {
        if (_font == null || _fontReadable == null) return;

        _font.RequestCharactersInTexture(text, FontPx, FontStyle.Bold);
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

            float adv = GlyphAdvance(infos[i]);
            totalAdvance += adv;
            maxH = Mathf.Max(maxH, Mathf.Abs(infos[i].maxY - infos[i].minY), infos[i].glyphHeight);
        }
        if (totalAdvance < 1f) totalAdvance = 1f;
        if (maxH < 1f) maxH = FontPx;

        float targetH = Mathf.Max(1f, sizeFrac * _rows);
        float scale = targetH / maxH;

        if (fitToWall)
        {
            float maxW = _columns * 0.92f;
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
            StampGlyphTrueType(info, penX, originY, scale, fg);
            penX += GlyphAdvance(info) * scale;
        }
    }

    private static float GlyphAdvance(CharacterInfo info)
    {
        float adv = info.advance;
        float boxW = Mathf.Abs(info.maxX - info.minX);
        if (adv < boxW * 0.35f)
            adv = boxW * 0.85f;
        return Mathf.Max(adv, 1f);
    }

    private void StampGlyphTrueType(CharacterInfo info, float penX, float originY, float scale, Color fg)
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
                float a = Mathf.Max(sample.a, Mathf.Max(sample.r, sample.g, sample.b)) * fg.a;
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

