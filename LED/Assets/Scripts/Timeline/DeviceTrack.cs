// Assets/Scripts/Timeline/DeviceTrack.cs
//
// Piste de chorégraphie lumineuse. Dans la fenetre Timeline :
//   bouton + -> Device Track
// puis relier la piste au GameObject portant le DeviceManager.
//
// Couleur de piste bleutee pour la distinguer au premier coup d'oeil des
// pistes du mur, qui sont jaunes.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(DeviceClip))]
[TrackBindingType(typeof(DeviceManager))]
[TrackColor(0.35f, 0.62f, 0.92f)]
public class DeviceTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<DeviceMixerBehaviour>.Create(graph, inputCount);
    }
}
