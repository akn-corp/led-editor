// Clip Timeline texte LED — propriétés + UnityEvents dans l'Inspector.
//
// Usage :
// 1. SceneBuilder.animationMode = None (Timeline pilote le mur)
// 2. Timeline → Add → EntityText Track
// 3. Binder LedWall (composant LedWallTextPainter)
// 4. Add EntityText Clip → régler text / couleur / fades / entrance / OnStart / OnEnd

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class EntityTextClip : PlayableAsset
{
    [Header("Contenu")]
    [TextArea(1, 3)]
    public string text = "Cause I don't wanna";

    public Color color = Color.white;
    public Color backgroundColor = Color.black;

    [Tooltip("Centre normalisé sur le mur (0–1)")]
    public Vector2 position = new Vector2(0.5f, 0.5f);

    [Tooltip("Hauteur relative du texte (fraction du mur) — TrueType 128×128")]
    [Range(0.04f, 0.5f)]
    public float size = 0.15f;

    [Header("Animation")]
    [Min(0f)] public float fadeIn = 0.15f;
    [Min(0f)] public float fadeOut = 0.15f;
    public Ease easeIn = Ease.OutElastic;
    public Ease easeOut = Ease.InExpo;
    public bool fitToWall = true;

    [Tooltip("Entrée Punch Karaoke")]
    public EntityTextEntrance entrance = EntityTextEntrance.Pop;

    [Tooltip("Échelle glyphes bitmap 32×32 (1 = normal, 2 = hooks)")]
    [Range(1, 2)]
    public int pixelScale = 1;

    [Tooltip("Flash blanc court au début du clip")]
    public bool hitFlash = true;

    [Header("Événements")]
    public UnityEvent onStart;
    public UnityEvent onEnd;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<EntityTextBehaviour>.Create(graph);
        var b = playable.GetBehaviour();
        b.text = text;
        b.color = color;
        b.backgroundColor = backgroundColor;
        b.position = position;
        b.size = size;
        b.fadeIn = fadeIn;
        b.fadeOut = fadeOut;
        b.easeIn = easeIn;
        b.easeOut = easeOut;
        b.fitToWall = fitToWall;
        b.entrance = entrance;
        b.pixelScale = pixelScale;
        b.hitFlash = hitFlash;
        b.onStart = onStart;
        b.onEnd = onEnd;
        return playable;
    }
}
