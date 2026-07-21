// Données runtime d'un clip EntityText pendant la lecture Timeline.

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
    public Ease easeIn = Ease.OutExpo;
    public Ease easeOut = Ease.InExpo;
    public bool fitToWall = true;

    public UnityEvent onStart;
    public UnityEvent onEnd;

    private bool _started;
    private bool _ended;

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

        if (fi > 0f && t < fi)
        {
            float u = t / fi;
            opacity = SimpleTween.Evaluate(easeIn, u);
        }
        else if (fo > 0f && t > d - fo)
        {
            float u = (float)((d - t) / fo);
            opacity = SimpleTween.Evaluate(easeOut, Mathf.Clamp01(u));
        }

        return Mathf.Clamp01(opacity);
    }
}
