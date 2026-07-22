// Clip Timeline — vague fluide colorée sur le mur LED.

using UnityEngine;
using UnityEngine.Playables;

public class FluidWallClip : PlayableAsset
{
    public float speed = 1.2f;
    public float waveScale = 6f;
    public float glowIntensity = 1.8f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<FluidWallBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.speed = speed;
        behaviour.waveScale = waveScale;
        behaviour.glowIntensity = glowIntensity;
        return playable;
    }
}
