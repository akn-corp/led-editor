// Clip Timeline : GIF / séquence d'images → mur LED (résolution WallMapping).

using UnityEngine;
using UnityEngine.Playables;

public class WallMediaClip : PlayableAsset
{
    public WallMediaSequence sequence;

    [Tooltip("Si Sequence est vide, utilise ces textures directement")]
    public Texture2D[] framesOverride;

    [Min(1f)]
    public float framesPerSecond = 12f;

    public bool loop = true;

    [Header("Rendu")]
    [Tooltip("Point = pixel art net (recommandé GIF 8-bit)")]
    public FilterMode sampleFilter = FilterMode.Point;

    [Range(0f, 1f)]
    public float brightness = 1f;

    public Color backgroundColor = Color.black;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<WallMediaBehaviour>.Create(graph);
        var b = playable.GetBehaviour();
        b.sequence = sequence;
        b.framesOverride = framesOverride;
        b.framesPerSecond = framesPerSecond;
        b.loop = loop;
        b.sampleFilter = sampleFilter;
        b.brightness = brightness;
        b.backgroundColor = backgroundColor;
        return playable;
    }
}
