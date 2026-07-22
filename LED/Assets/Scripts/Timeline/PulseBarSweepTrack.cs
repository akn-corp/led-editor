// Piste Timeline — barre pulsante basse + ligne fine balayeuse.
// Binding : EntityManager. Même architecture que ConvergenceGridTrack.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(PulseBarSweepClip))]
[TrackBindingType(typeof(EntityManager))]
[TrackColor(1f, 0.8f, 0.1f)]
public class PulseBarSweepTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<PulseBarSweepMixerBehaviour>.Create(graph, inputCount);
    }
}

public class PulseBarSweepMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var entityManager = playerData as EntityManager;
        if (entityManager == null) return;

        PulseBarSweepBehaviour best = null;
        Playable bestPlayable = default;
        float bestWeight = 0f;

        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = (ScriptPlayable<PulseBarSweepBehaviour>)playable.GetInput(i);
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
            best.Apply(entityManager, (float)bestPlayable.GetTime(), (float)bestPlayable.GetDuration(), bestWeight);
    }
}
