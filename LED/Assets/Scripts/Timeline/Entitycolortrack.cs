// Assets/Scripts/Timeline/EntityColorTrack.cs
//
// La piste elle-même. [TrackBindingType(typeof(EntityManager))] indique à
// Unity que cette piste doit être reliée (dans la fenêtre Timeline) à un
// GameObject portant un EntityManager — c'est ce lien qui permet au mixer
// de recevoir la bonne référence via playerData.

using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(EntityColorClip))]
[TrackBindingType(typeof(EntityManager))]
public class EntityColorTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, UnityEngine.GameObject go, int inputCount)
    {
        return ScriptPlayable<EntityColorMixerBehaviour>.Create(graph, inputCount);
    }
}