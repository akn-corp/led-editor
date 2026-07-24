// Données runtime d'un clip EntityText pendant la lecture Timeline.

using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class EntityTextBehaviour : PlayableBehaviour
{
    public string text = "Text";
    public Color color = Color.white;
    public Color backgroundColor = Color.black;
    public Vector2 position = new Vector2(0.5f, 0.5f);
    public float size = 0.15f;
    public float fadeIn = 0.15f;
    public float fadeOut = 0.15f;
    public Ease easeIn = Ease.OutElastic;
    public Ease easeOut = Ease.InExpo;
    public bool fitToWall = true;
    public EntityTextEntrance entrance = EntityTextEntrance.Pop;
    public int pixelScale = 1;
    public bool hitFlash = true;

    public UnityEvent onStart;
    public UnityEvent onEnd;

    private bool _started;
    private bool _ended;

    private static readonly char[] ScramblePool =
    {
        '#', '%', '&', '*', '+', 'X', 'Z', '0', '1', '8', '/', '\\', '?',
    };

    public struct MotionState
    {
        public string text;
        public Vector2 position;
        public float size;
        public Color color;
        public float opacity;
        public Color background;
        public bool fitToWall;
        public int pixelScale;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (_started) return;
        _started = true;
        _ended = false;
        onStart?.Invoke();
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (!_started || _ended) return;

        double t = playable.GetTime();
        double d = playable.GetDuration();
        bool finished = d > 0 && t >= d - 1e-3;
        bool deactivated = info.effectiveWeight <= 0f;

        if (finished || deactivated)
        {
            _ended = true;
            onEnd?.Invoke();
        }
    }

    public override void OnGraphStop(Playable playable)
    {
        if (_started && !_ended)
        {
            _ended = true;
            onEnd?.Invoke();
        }
        _started = false;
    }

    /// <summary>Opacity 0–1 selon fade in/out et temps local du clip.</summary>
    public float EvaluateOpacity(Playable playable)
    {
        double time = playable.GetTime();
        double duration = playable.GetDuration();
        if (duration <= 0) return 1f;

        float t = (float)time;
        float d = (float)duration;
        float opacity = 1f;

        float fi = Mathf.Max(0f, fadeIn);
        float fo = Mathf.Max(0f, fadeOut);
        if (fi + fo > d)
        {
            float scale = d / (fi + fo);
            fi *= scale;
            fo *= scale;
        }

        // Opacité : ease stable (évite le scintillement OutElastic / OutBack).
        // easeIn reste dédié au mouvement Punch (Pop / Slide / Scramble).
        bool motionEntrance =
            entrance == EntityTextEntrance.Pop
            || entrance == EntityTextEntrance.SlideLeft
            || entrance == EntityTextEntrance.SlideRight
            || entrance == EntityTextEntrance.Scramble;
        Ease opacityIn = motionEntrance ? Ease.OutExpo : easeIn;

        if (fi > 0f && t < fi)
        {
            float u = t / fi;
            opacity = Mathf.Clamp01(SimpleTween.Evaluate(opacityIn, u));
        }
        else if (fo > 0f && t > d - fo)
        {
            float u = (float)((d - t) / fo);
            opacity = Mathf.Clamp01(SimpleTween.Evaluate(easeOut, Mathf.Clamp01(u)));
        }

        return Mathf.Clamp01(opacity);
    }

    /// <summary>État de dessin Punch Karaoke (position, scramble, flash, scale).</summary>
    public MotionState EvaluateMotion(Playable playable, float weight = 1f)
    {
        float t = (float)playable.GetTime();
        float d = (float)playable.GetDuration();
        float opacity = EvaluateOpacity(playable) * Mathf.Clamp01(weight);

        float fi = Mathf.Max(0f, fadeIn);
        float fo = Mathf.Max(0f, fadeOut);
        if (d > 0f && fi + fo > d)
        {
            float s = d / (fi + fo);
            fi *= s;
            fo *= s;
        }

        float enterU = fi > 0.0001f ? Mathf.Clamp01(t / fi) : 1f;
        float enterEased = SimpleTween.Evaluate(easeIn, enterU);

        Vector2 pos = position;
        string display = text ?? "";
        Color col = color;
        int scale = Mathf.Clamp(pixelScale, 1, 2);
        float drawSize = size;

        switch (entrance)
        {
            case EntityTextEntrance.Pop:
                // Remonte depuis le bas + léger overshoot OutElastic / OutBack
                pos.y = Mathf.LerpUnclamped(position.y - 0.22f, position.y, enterEased);
                pos.y = Mathf.Clamp01(pos.y);
                drawSize = size * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(enterEased));
                if (enterU < 1f && enterEased > 1.05f)
                    scale = Mathf.Max(scale, 2);
                break;

            case EntityTextEntrance.SlideLeft:
                pos.x = Mathf.Lerp(-0.25f, position.x, Mathf.Clamp01(enterEased));
                break;

            case EntityTextEntrance.SlideRight:
                pos.x = Mathf.Lerp(1.25f, position.x, Mathf.Clamp01(enterEased));
                break;

            case EntityTextEntrance.Blink:
                if (enterU < 1f)
                {
                    float blink = Mathf.Repeat(t * 14f, 1f);
                    opacity *= blink < 0.45f ? 1f : 0.08f;
                }
                break;

            case EntityTextEntrance.Scramble:
                if (enterU < 1f)
                    display = BuildScramble(text, enterU);
                break;
        }

        if (hitFlash && t < 0.07f && d > 0.05f)
        {
            col = Color.white;
            opacity = Mathf.Max(opacity, 0.95f);
        }

        return new MotionState
        {
            text = display,
            position = pos,
            size = drawSize,
            color = col,
            opacity = Mathf.Clamp01(opacity),
            background = backgroundColor,
            fitToWall = fitToWall,
            pixelScale = scale,
        };
    }

    private static string BuildScramble(string source, float progress)
    {
        if (string.IsNullOrEmpty(source)) return source;

        int reveal = Mathf.Clamp(Mathf.FloorToInt(progress * source.Length), 0, source.Length);
        var sb = new StringBuilder(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            char ch = source[i];
            if (ch == ' ' || ch == '\n')
            {
                sb.Append(ch);
                continue;
            }

            if (i < reveal)
                sb.Append(ch);
            else
                sb.Append(ScramblePool[(i * 17 + reveal * 3) % ScramblePool.Length]);
        }

        return sb.ToString();
    }
}
