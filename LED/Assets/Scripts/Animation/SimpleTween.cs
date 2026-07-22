// Mini moteur de tweens façon Framer Motion / DOTween — zéro dépendance.
// Usage : SimpleTween.To(0, 1, 0.25f, Ease.OutBack, v => scale = v);

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Ease
{
    Linear,
    InQuad,
    OutQuad,
    InOutQuad,
    InExpo,
    OutExpo,
    OutBack,
    InBack,
    OutElastic,
}

public sealed class SimpleTween
{
    private static readonly List<Tween> Active = new List<Tween>();
    private static TweenRunner _runner;

    public static TweenHandle To(
        float from,
        float to,
        float duration,
        Ease ease,
        Action<float> onUpdate,
        Action onComplete = null)
    {
        EnsureRunner();
        var tween = new Tween(from, to, duration, ease, onUpdate, onComplete);
        Active.Add(tween);
        return new TweenHandle(tween);
    }

    public static SequenceHandle Sequence()
    {
        EnsureRunner();
        return new SequenceHandle();
    }

    public static float Evaluate(Ease ease, float t)
    {
        t = Mathf.Clamp01(t);
        switch (ease)
        {
            case Ease.Linear: return t;
            case Ease.InQuad: return t * t;
            case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
            case Ease.InOutQuad:
                return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
            case Ease.InExpo: return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
            case Ease.OutExpo: return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
            case Ease.OutBack:
            {
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            }
            case Ease.InBack:
            {
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                return c3 * t * t * t - c1 * t * t;
            }
            case Ease.OutElastic:
            {
                if (t <= 0f) return 0f;
                if (t >= 1f) return 1f;
                const float c4 = 2f * Mathf.PI / 3f;
                return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
            }
            default: return t;
        }
    }

    internal static void Tick(float dt)
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            if (!Active[i].Update(dt))
                Active.RemoveAt(i);
        }
    }

    internal static void Kill(Tween tween)
    {
        tween.Kill();
        Active.Remove(tween);
    }

    private static void EnsureRunner()
    {
        if (_runner != null) return;
        var go = new GameObject("[SimpleTween]");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<TweenRunner>();
    }

    private sealed class TweenRunner : MonoBehaviour
    {
        void Update() => Tick(Time.deltaTime);
    }

    internal sealed class Tween
    {
        private readonly float _from;
        private readonly float _to;
        private readonly float _duration;
        private readonly Ease _ease;
        private readonly Action<float> _onUpdate;
        private readonly Action _onComplete;
        private float _elapsed;
        private bool _killed;
        private bool _completed;

        public Tween(float from, float to, float duration, Ease ease, Action<float> onUpdate, Action onComplete)
        {
            _from = from;
            _to = to;
            _duration = Mathf.Max(0.0001f, duration);
            _ease = ease;
            _onUpdate = onUpdate;
            _onComplete = onComplete;
            _onUpdate?.Invoke(_from);
        }

        public bool Update(float dt)
        {
            if (_killed || _completed) return false;
            _elapsed += dt;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float v = Mathf.LerpUnclamped(_from, _to, Evaluate(_ease, t));
            _onUpdate?.Invoke(v);
            if (t < 1f) return true;
            _completed = true;
            _onComplete?.Invoke();
            return false;
        }

        public void Kill() => _killed = true;
    }
}

public readonly struct TweenHandle
{
    private readonly SimpleTween.Tween _tween;
    internal TweenHandle(SimpleTween.Tween tween) => _tween = tween;
    public void Kill()
    {
        if (_tween != null) SimpleTween.Kill(_tween);
    }
}

/// <summary>Chaîne de tweens / délais, style Motion sequence.</summary>
public sealed class SequenceHandle
{
    private readonly Queue<IEnumerator> _steps = new Queue<IEnumerator>();
    private Coroutine _routine;
    private MonoBehaviour _host;

    public SequenceHandle Append(float from, float to, float duration, Ease ease, Action<float> onUpdate)
    {
        _steps.Enqueue(RunTween(from, to, duration, ease, onUpdate));
        return this;
    }

    public SequenceHandle AppendInterval(float seconds)
    {
        _steps.Enqueue(Wait(seconds));
        return this;
    }

    public SequenceHandle AppendCallback(Action callback)
    {
        _steps.Enqueue(Callback(callback));
        return this;
    }

    public SequenceHandle Play(MonoBehaviour host)
    {
        _host = host;
        if (_routine != null) host.StopCoroutine(_routine);
        _routine = host.StartCoroutine(Run());
        return this;
    }

    public void Kill()
    {
        if (_host != null && _routine != null)
            _host.StopCoroutine(_routine);
        _steps.Clear();
    }

    private IEnumerator Run()
    {
        while (_steps.Count > 0)
            yield return _host.StartCoroutine(_steps.Dequeue());
    }

    private static IEnumerator RunTween(float from, float to, float duration, Ease ease, Action<float> onUpdate)
    {
        bool done = false;
        SimpleTween.To(from, to, duration, ease, onUpdate, () => done = true);
        while (!done) yield return null;
    }

    private static IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    private static IEnumerator Callback(Action callback)
    {
        callback?.Invoke();
        yield break;
    }
}
