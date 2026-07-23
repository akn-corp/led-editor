// Assets/Scripts/Timeline/WallEffectTrack.cs
//
// La piste d'effets proceduraux. Dans la fenetre Timeline :
//   clic droit -> Wall Effect Track
// puis on glisse des clips dessus, exactement comme dans un montage video.
//
// [TrackBindingType(typeof(EntityManager))] indique a Unity que la piste doit
// etre reliee au GameObject portant l'EntityManager : c'est ce lien qui fait
// arriver la bonne reference dans playerData cote mixer.
//
// La couleur de la piste dans l'editeur reprend le jaune du spectacle.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(WallEffectClip))]
[TrackBindingType(typeof(EntityManager))]
[TrackColor(0.90f, 0.76f, 0.16f)]
public class WallEffectTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<WallEffectMixerBehaviour>.Create(graph, inputCount);
    }
}
