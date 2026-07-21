// Typographie cinétique (démo hors Timeline) — délègue le paint à LedWallTextPainter.
// Pour le montage spectacle : utiliser EntityTextClip sur une Entity Text Track.

using System;
using UnityEngine;

[RequireComponent(typeof(LedWallVisualizer))]
[RequireComponent(typeof(LedWallTextPainter))]
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

    private LedWallTextPainter _painter;
    private SequenceHandle _sequence;
    private WordState[] _live;
    private const float UpdateInterval = 1f / 40f;
    private float _timer;

    public void Initialize(EntityManager manager, LedWallVisualizer visualizer, int columns)
    {
        entityManager = manager;
        _painter = GetComponent<LedWallTextPainter>();
        if (_painter == null)
            _painter = gameObject.AddComponent<LedWallTextPainter>();
        if (!_painter.IsReady)
            _painter.Initialize(manager, visualizer, columns);

        if (timeline == null || timeline.Length == 0)
            timeline = BuildDefaultTimeline();

        PrefetchFromTimeline();
        _painter.Clear(Bg);
    }

    void Start()
    {
        if (_painter == null)
            _painter = GetComponent<LedWallTextPainter>();

        if (_painter != null && !_painter.IsReady && WallMapping.IsInitialized)
        {
            var viz = GetComponent<LedWallVisualizer>();
            Initialize(entityManager, viz, WallMapping.Columns);
        }

        if (playOnStart)
            Play();
    }

    void OnDisable() => _sequence?.Kill();

    void Update()
    {
        if (_painter == null || !_painter.IsReady) return;

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
        _painter.BeginFrame(Bg);
        if (_live != null)
        {
            for (int i = 0; i < _live.Length; i++)
            {
                var w = _live[i];
                if (w == null || w.opacity < 0.02f || string.IsNullOrEmpty(w.text))
                    continue;
                _painter.DrawTextLayer(w.text, w.position, w.size, w.color, w.opacity, fitToWall: true);
            }
        }
        _painter.EndFrame();
    }

    private void PrefetchFromTimeline()
    {
        if (timeline == null || _painter == null) return;
        var sb = new System.Text.StringBuilder();
        foreach (var kf in timeline)
        {
            if (kf.words == null) continue;
            foreach (var w in kf.words)
            {
                if (!string.IsNullOrEmpty(w.text))
                    sb.Append(w.text);
            }
        }
        _painter.PrefetchCharacters(sb.ToString());
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
