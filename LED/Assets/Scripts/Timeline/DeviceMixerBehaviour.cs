// Mixer DeviceTrack : DeviceShowClip (tous devices) ou DeviceClip (un device).

using UnityEngine;
using UnityEngine.Playables;

public class DeviceMixerBehaviour : PlayableBehaviour
{
    private bool _hadActive;
    private DevicePreviewVisualizer _preview;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var deviceManager = playerData as DeviceManager;
        if (deviceManager == null) return;

        int inputCount = playable.GetInputCount();
        bool any = false;

        // Priorité : DeviceShow (pilote les 5 devices) > DeviceClip last-wins par id
        DeviceShowBehaviour show = null;
        Playable showPlayable = default;
        float showWeight = 0f;

        var applied = new bool[DeviceManager.DeviceCount];

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = playable.GetInput(i);
            if (!input.IsValid()) continue;

            var type = input.GetPlayableType();
            if (type == typeof(DeviceShowBehaviour))
            {
                var sp = (ScriptPlayable<DeviceShowBehaviour>)input;
                if (weight >= showWeight)
                {
                    showWeight = weight;
                    show = sp.GetBehaviour();
                    showPlayable = input;
                }
                any = true;
            }
            else if (type == typeof(DeviceBehaviour))
            {
                var sp = (ScriptPlayable<DeviceBehaviour>)input;
                var behaviour = sp.GetBehaviour();
                if (behaviour == null) continue;
                var state = behaviour.Evaluate((float)input.GetTime());
                if (state.deviceId >= DeviceManager.DeviceCount) continue;
                deviceManager.SetDevice(state);
                applied[state.deviceId] = true;
                any = true;
            }
        }

        if (show != null)
        {
            // Temps local du clip (sweep / blink). Fallback graph si GetTime reste 0.
            float clipTime = (float)showPlayable.GetTime();
            if (clipTime <= 0.0001f && playable.GetGraph().IsValid())
            {
                var root = playable.GetGraph().GetRootPlayable(0);
                if (root.IsValid())
                    clipTime = (float)root.GetTime();
            }

            show.ApplyTo(deviceManager, clipTime);
            _hadActive = true;
        }
        else if (any)
        {
            for (byte id = 0; id < DeviceManager.DeviceCount; id++)
            {
                if (!applied[id])
                    deviceManager.SetDevice(DeviceState.Blackout(id));
            }
            _hadActive = true;
        }
        else if (_hadActive)
        {
            deviceManager.EnsureDefaults();
            _hadActive = false;
        }

        if (_preview == null)
        {
            var dm = playerData as DeviceManager;
            if (dm != null)
                _preview = dm.GetComponent<DevicePreviewVisualizer>();
            if (_preview == null)
            {
                var all = Resources.FindObjectsOfTypeAll<DevicePreviewVisualizer>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].gameObject.scene.IsValid())
                    {
                        _preview = all[i];
                        break;
                    }
                }
            }
        }
        _preview?.RefreshFromManager();
    }
}
