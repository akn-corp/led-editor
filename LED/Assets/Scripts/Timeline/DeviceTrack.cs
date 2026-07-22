// Piste Timeline devices (DEVS). Binding : DeviceManager.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(DeviceShowClip))]
[TrackClipType(typeof(DeviceClip))]
[TrackBindingType(typeof(DeviceManager))]
[TrackColor(0.95f, 0.45f, 0.15f)]
public class DeviceTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<DeviceMixerBehaviour>.Create(graph, inputCount);
    }
}
