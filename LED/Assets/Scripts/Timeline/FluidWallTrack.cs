// Piste Timeline dédiée au mur fluide. Binding : EntityManager.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(FluidWallClip))]
[TrackBindingType(typeof(EntityManager))]
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
        var entityManager = playerData as EntityManager;
        if (entityManager == null) return;

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
            best.Apply(entityManager, (float)bestPlayable.GetTime());
    }
}
