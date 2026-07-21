// Typographie cinétique → mur LED.
// Fond NOIR forcé. Texte blanc/vert peint via police OS (pas de RenderTexture blanc).

using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LedWallVisualizer))]
public class KineticTypographyAnimator : MonoBehaviour
{
    [Serializable]
    public class WordState
    {
        public string text = "word";
        public Color color = Color.white;
        public Vector2 position = new Vector2(0.5f, 0.5f);
        [Tooltip("Hauteur du texte en fraction du mur (0.2 = 20%)")]
        public float size = 0.2f;
        public float opacity = 1f;
    }

    [Serializable]
    public class Keyframe
    {
        public float duration = 0.2f;
        public Ease ease = Ease.OutExpo;
        public float hold = 0.12f;
        public WordState[] words;
    }

    [SerializeField] private EntityManager entityManager;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private Keyframe[] timeline;

    private static readonly Color Bg = Color.black;

    private LedWallVisualizer _visualizer;
    private int _columns;
    private int _rows;
    private int?[,] _entityGrid;
    private Color[] _displayPixels;
    private Font _font;
    private Texture2D _fontReadable;
    private int _fontReadableVersion = -1;
    private SequenceHandle _sequence;
    private WordState[] _live;
    private const float UpdateInterval = 1f / 40f;
    private float _timer;
    private const int FontPx = 64;

    public void Initialize(EntityManager manager, LedWallVisualizer visualizer, int columns)
    {
        entityManager = manager;
        _visualizer = visualizer;
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

        if (timeline == null || timeline.Length == 0)
            timeline = BuildDefaultTimeline();

        PrefetchGlyphs();
        ClearBlack();
        _visualizer.ApplyDisplayPixels(_displayPixels);
    }

    void Start()
    {
        if (_visualizer == null)
            _visualizer = GetComponent<LedWallVisualizer>();

        if (_entityGrid == null && WallMapping.IsInitialized)
            Initialize(entityManager, _visualizer, WallMapping.Columns);

        if (playOnStart)
            Play();
    }

    void OnDestroy()
    {
        if (_fontReadable != null)
            Destroy(_fontReadable);
    }

    void OnDisable() => _sequence?.Kill();

    void Update()
    {
        if (_entityGrid == null || entityManager == null || _visualizer == null) return;

        _timer += Time.deltaTime;
        if (_timer < UpdateInterval) return;
        _timer = 0f;
        PaintFrame();
    }

    [ContextMenu("Play Kinetic Sequence")]
    public void Play()
    {
        if (_live == null)
            _live = Array.Empty<WordState>();

        _sequence?.Kill();
        _sequence = SimpleTween.Sequence();

        foreach (var kf in timeline)
        {
            var frame = kf;
            _sequence.AppendCallback(() => BeginTransition(frame));
            _sequence.AppendInterval(frame.duration + frame.hold);
        }

        if (loop)
            _sequence.AppendCallback(Play);

        _sequence.Play(this);
        Debug.Log($"[KineticTypography] Play — fond noir, {timeline.Length} keyframes");
    }

    private void BeginTransition(Keyframe kf)
    {
        EnsureLiveCapacity(kf.words?.Length ?? 0);

        int n = kf.words?.Length ?? 0;
        for (int i = 0; i < n; i++)
        {
            var target = kf.words[i];
            var from = _live[i] ?? new WordState { opacity = 0f };
            int idx = i;

            float x0 = from.position.x, y0 = from.position.y;
            float x1 = target.position.x, y1 = target.position.y;
            float s0 = from.size, s1 = target.size;
            float o0 = from.opacity, o1 = target.opacity;
            Color c0 = from.color, c1 = target.color;

            _live[idx].text = target.text;

            SimpleTween.To(0f, 1f, kf.duration, kf.ease, t =>
            {
                _live[idx].position = new Vector2(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t));
                _live[idx].size = Mathf.Lerp(s0, s1, t);
                _live[idx].opacity = Mathf.Lerp(o0, o1, t);
                _live[idx].color = Color.Lerp(c0, c1, t);
            });
        }

        for (int i = n; i < _live.Length; i++)
        {
            int idx = i;
            float o0 = _live[idx].opacity;
            SimpleTween.To(0f, 1f, kf.duration, Ease.InExpo, t =>
            {
                _live[idx].opacity = Mathf.Lerp(o0, 0f, t);
            });
        }
    }

    private void EnsureLiveCapacity(int n)
    {
        if (n <= 0) n = 0;
        if (_live != null && _live.Length >= n && n > 0) return;
        if (n == 0)
        {
            if (_live == null) _live = Array.Empty<WordState>();
            return;
        }

        var next = new WordState[n];
        for (int i = 0; i < n; i++)
            next[i] = (i < (_live?.Length ?? 0) && _live[i] != null)
                ? _live[i]
                : new WordState { opacity = 0f, size = 0.05f, position = new Vector2(0.5f, 0.5f), color = Color.white };
        _live = next;
    }

    private void PaintFrame()
    {
        ClearBlack();

        if (_live != null)
        {
            for (int i = 0; i < _live.Length; i++)
            {
                var w = _live[i];
                if (w == null || w.opacity < 0.02f || string.IsNullOrEmpty(w.text))
                    continue;
                DrawText(w.text, w.position, w.size, w.color, w.opacity);
            }
        }

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
                entityManager.SetColorSilent(entityId.Value, r, g, b);
            }
        }

        _visualizer.ApplyDisplayPixels(_displayPixels);
    }

    private void ClearBlack()
    {
        for (int i = 0; i < _displayPixels.Length; i++)
            _displayPixels[i] = Bg;
    }

    private void PrefetchGlyphs()
    {
        if (_font == null) return;
        var chars = new HashSet<char>();
        foreach (var kf in timeline)
        {
            if (kf.words == null) continue;
            foreach (var w in kf.words)
            {
                if (string.IsNullOrEmpty(w.text)) continue;
                foreach (char c in w.text)
                    chars.Add(c);
            }
        }
        if (chars.Count == 0) return;
        _font.RequestCharactersInTexture(string.Concat(chars), FontPx, FontStyle.Bold);
        RefreshFontReadable();
    }

    private void RefreshFontReadable()
    {
        if (_font == null || _font.material == null || _font.material.mainTexture == null)
            return;

        var src = _font.material.mainTexture;
        int w = src.width;
        int h = src.height;
        // Recopie GPU → CPU (la texture dynamique n’est pas readable)
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
        _fontReadableVersion++;
    }

    private void DrawText(string text, Vector2 centerNorm, float sizeFrac, Color color, float opacity)
    {
        if (_font == null || _fontReadable == null) return;

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

        // Scale par hauteur demandée…
        float targetH = sizeFrac * _rows;
        float scale = targetH / maxH;

        // …puis shrink pour tenir dans le mur (marge 12 %)
        float maxW = _columns * 0.88f;
        float textW = totalAdvance * scale;
        if (textW > maxW)
            scale *= maxW / textW;

        textW = totalAdvance * scale;
        float textH = maxH * scale;

        float originX = centerNorm.x * (_columns - 1) - textW * 0.5f;
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

        // Largeur stamp = boîte glyphe (pas l’avance) pour garder les proportions
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

    private static Keyframe[] BuildDefaultTimeline()
    {
        Color ink = Color.white;
        Color accent = new Color(0.35f, 0.95f, 0.2f);

        WordState W(string t, Color c, float x, float y, float size, float op = 1f) =>
            new WordState { text = t, color = c, position = new Vector2(x, y), size = size, opacity = op };

        return new[]
        {
            new Keyframe
            {
                duration = 0.18f, hold = 0.22f, ease = Ease.OutBack,
                words = new[] { W("Cause", ink, 0.5f, 0.5f, 0.22f) }
            },
            new Keyframe
            {
                duration = 0.14f, hold = 0.16f, ease = Ease.OutExpo,
                words = new[]
                {
                    W("I don't", ink, 0.35f, 0.58f, 0.14f),
                    W("wanna", accent, 0.7f, 0.38f, 0.14f),
                }
            },
            new Keyframe
            {
                duration = 0.12f, hold = 0.14f, ease = Ease.OutExpo,
                words = new[] { W("talk about", ink, 0.5f, 0.5f, 0.12f) }
            },
            new Keyframe
            {
                duration = 0.16f, hold = 0.28f, ease = Ease.OutBack,
                words = new[] { W("numbers", accent, 0.5f, 0.5f, 0.2f) }
            },
            new Keyframe
            {
                duration = 0.18f, hold = 0.35f, ease = Ease.OutExpo,
                words = new[]
                {
                    W("all day", ink, 0.5f, 0.62f, 0.14f),
                    W("long", ink, 0.5f, 0.38f, 0.18f),
                }
            },
            new Keyframe
            {
                duration = 0.22f, hold = 0.55f, ease = Ease.OutQuad,
                words = new[]
                {
                    W("Cause I don't wanna", ink, 0.5f, 0.72f, 0.08f),
                    W("talk about numbers", ink, 0.5f, 0.52f, 0.08f),
                    W("all day long", accent, 0.5f, 0.3f, 0.1f),
                }
            },
            new Keyframe
            {
                duration = 0.2f, hold = 0.12f, ease = Ease.InExpo,
                words = Array.Empty<WordState>()
            },
        };
    }
}
