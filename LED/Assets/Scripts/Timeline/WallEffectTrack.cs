// Assets/Scripts/Timeline/WallEffectTrack.cs
//
// Piste d'effets proceduraux. Binding : LedWall (LedWallVisualizer),
// comme FluidWall / WallMedia / Paloma.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(WallEffectClip))]
[TrackBindingType(typeof(LedWallVisualizer))]
[TrackColor(0.90f, 0.76f, 0.16f)]
public class WallEffectTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<WallEffectMixerBehaviour>.Create(graph, inputCount);
    }
}
