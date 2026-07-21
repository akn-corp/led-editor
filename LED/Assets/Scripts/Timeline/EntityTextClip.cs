// Clip Timeline texte LED — propriétés + UnityEvents dans l'Inspector.
//
// Usage :
// 1. SceneBuilder.animationMode = None (Timeline pilote le mur)
// 2. Timeline → Add → Entity Text Track
// 3. Binder LedWall (composant LedWallTextPainter)
// 4. Add Entity Text Clip → régler text / couleur / fades / OnStart / OnEnd

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

    [Tooltip("Hauteur relative du texte (fraction du mur)")]
    [Range(0.04f, 0.5f)]
    public float size = 0.15f;

    [Header("Animation")]
    [Min(0f)] public float fadeIn = 0.15f;
    [Min(0f)] public float fadeOut = 0.15f;
    public Ease easeIn = Ease.OutExpo;
    public Ease easeOut = Ease.InExpo;
    public bool fitToWall = true;

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
        b.onStart = onStart;
        b.onEnd = onEnd;
        return playable;
    }
}
