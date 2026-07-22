// Piste Timeline pour clips texte LED.
// Binding : GameObject LedWall portant LedWallTextPainter (créé par SceneBuilder).

using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(EntityTextClip))]
[TrackBindingType(typeof(LedWallTextPainter))]
[TrackColor(0.2f, 0.85f, 0.35f)]
public class EntityTextTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, UnityEngine.GameObject go, int inputCount)
    {
        return ScriptPlayable<EntityTextMixerBehaviour>.Create(graph, inputCount);
    }
}
