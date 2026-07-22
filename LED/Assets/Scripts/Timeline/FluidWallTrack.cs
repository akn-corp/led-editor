// Piste Timeline mur fluide. Binding : LedWall (LedWallVisualizer).

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(FluidWallClip))]
[TrackBindingType(typeof(LedWallVisualizer))]
[TrackColor(0.15f, 0.55f, 0.95f)]
public class FluidWallTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<FluidWallMixerBehaviour>.Create(graph, inputCount);
    }
}

public class FluidWallMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var wall = playerData as LedWallVisualizer;
        if (wall == null || !wall.IsBuilt || wall.EntityManager == null) return;

        FluidWallBehaviour best = null;
        Playable bestPlayable = default;
        float bestWeight = 0f;

        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = (ScriptPlayable<FluidWallBehaviour>)playable.GetInput(i);
            var behaviour = input.GetBehaviour();
            if (behaviour == null) continue;

            if (weight >= bestWeight)
            {
                bestWeight = weight;
                best = behaviour;
                bestPlayable = input;
            }
        }

        if (best != null)
            best.Apply(wall.EntityManager, wall, (float)bestPlayable.GetTime());
    }
}
