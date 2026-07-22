// Piste Timeline — convergence géométrique (lignes -> grille -> matrice de carrés).
// Binding : EntityManager. Même architecture que FluidWallTrack.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(ConvergenceGridClip))]
[TrackBindingType(typeof(EntityManager))]
[TrackColor(0.95f, 0.95f, 0.95f)]
public class ConvergenceGridTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<ConvergenceGridMixerBehaviour>.Create(graph, inputCount);
    }
}

public class ConvergenceGridMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var entityManager = playerData as EntityManager;
        if (entityManager == null) return;

        ConvergenceGridBehaviour best = null;
        Playable bestPlayable = default;
        float bestWeight = 0f;

        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = (ScriptPlayable<ConvergenceGridBehaviour>)playable.GetInput(i);
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
            best.Apply(entityManager, (float)bestPlayable.GetTime(), (float)bestPlayable.GetDuration());
    }
}
