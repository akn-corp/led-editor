// Piste dédiée au bandeau Paloma Rumba. Binding : EntityManager.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(PalomaRumbaTextClip))]
[TrackBindingType(typeof(EntityManager))]
[TrackColor(0.9f, 0.75f, 0.15f)]
public class PalomaRumbaTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<PalomaRumbaTrackMixer>.Create(graph, inputCount);
    }
}

public class PalomaRumbaTrackMixer : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var entityManager = playerData as EntityManager;
        if (entityManager == null) return;

        PalomaRumbaTextBehaviour best = null;
        Playable bestPlayable = default;
        float bestWeight = 0f;

        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = (ScriptPlayable<PalomaRumbaTextBehaviour>)playable.GetInput(i);
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
