// Piste Timeline — ondes concentriques (ripple néon).
// Binding : EntityManager. Même architecture que FluidWallTrack / ConvergenceGridTrack.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(RippleWaveClip))]
[TrackBindingType(typeof(EntityManager))]
[TrackColor(0.15f, 0.85f, 1f)]
public class RippleWaveTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<RippleWaveMixerBehaviour>.Create(graph, inputCount);
    }
}

public class RippleWaveMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var entityManager = playerData as EntityManager;
        if (entityManager == null) return;

        RippleWaveBehaviour best = null;
        Playable bestPlayable = default;
        float bestWeight = 0f;

        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = (ScriptPlayable<RippleWaveBehaviour>)playable.GetInput(i);
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
            best.Apply(entityManager, (float)bestPlayable.GetTime(), bestWeight);
    }
}
