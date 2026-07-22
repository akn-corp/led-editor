// Évalue projecteur + 4 lyres pour un DeviceShowClip.

using UnityEngine;
using UnityEngine.Playables;

public class DeviceShowBehaviour : PlayableBehaviour
{
    public bool enableProjector = true;
    public byte projectorDimmer = 255;
    public byte projectorShutter = DeviceState.ShutterOpen;
    public byte projectorR = 230;
    public byte projectorG = 194;
    public byte projectorB = 41;
    public byte projectorW = 180;
    public bool projectorBlink = true;
    public float projectorBlinkHz = 2f;

    public bool enableLyres = true;
    public byte lyreDimmer = 255;
    public byte lyreShutter = DeviceState.ShutterOpen;
    /// <summary>Fallback si lyreColorWheels est null / trop court.</summary>
    public byte lyreColorWheel = 80;
    /// <summary>Une teinte roue par lyre (1–4) — envoyée telle quelle au Hub.</summary>
    public byte[] lyreColorWheels = { 80, 120, 160, 40 }; // rouge, vert, bleu, blanc (presets Hub)
    public byte lyreR = 255;
    /// <summary>RGB distincts par lyre (le colorWheel seul ne suffit pas sur certains fixtures).</summary>
    public byte[] lyreRChannels = { 255, 0, 0, 255 };
    public byte[] lyreGChannels = { 0, 255, 0, 180 };
    public byte[] lyreBChannels = { 0, 0, 255, 0 };
    public bool autoSweep = true;
    public float sweepSpeed = 1.8f;
    public float sweepAmplitudePan = 110f;
    /// <summary>0 = balayage horizontal seulement (évite le « bas→haut » dominant sur fixture).</summary>
    public float sweepAmplitudeTilt = 0f;
    public byte moveSpeed = 128;

    public void ApplyTo(DeviceManager deviceManager, float clipTime)
    {
        if (deviceManager == null) return;

        deviceManager.EnsureDefaults();

        if (enableProjector)
        {
            byte dim = projectorDimmer;
            byte shutter = projectorShutter;
            byte r = projectorR, g = projectorG, b = projectorB, w = projectorW;
            if (projectorBlink)
            {
                float phase = clipTime * projectorBlinkHz;
                bool on = (phase - Mathf.Floor(phase)) < 0.5f;
                if (!on)
                {
                    dim = 0;
                    shutter = 0;
                    r = g = b = w = 0;
                }
            }

            deviceManager.SetDevice(new DeviceState
            {
                deviceId = 0,
                pan = DeviceState.Center,
                tilt = DeviceState.Center,
                dimmer = dim,
                shutter = shutter,
                r = r,
                g = g,
                b = b,
                w = w,
            });
        }

        if (!enableLyres) return;

        for (byte id = 1; id <= 4; id++)
        {
            byte pan = DeviceState.Center;
            byte tilt = DeviceState.Center;
            if (autoSweep)
            {
                float phase = id * 1.1f;
                float p = DeviceState.Center + Mathf.Sin(clipTime * sweepSpeed + phase) * sweepAmplitudePan;
                float t = DeviceState.Center + Mathf.Cos(clipTime * sweepSpeed * 0.85f + phase) * sweepAmplitudeTilt;
                pan = (byte)Mathf.Clamp(Mathf.RoundToInt(p), 0, 255);
                tilt = (byte)Mathf.Clamp(Mathf.RoundToInt(t), 0, 255);
            }

            deviceManager.SetDevice(new DeviceState
            {
                deviceId = id,
                pan = pan,
                tilt = tilt,
                dimmer = lyreDimmer,
                shutter = lyreShutter,
                colorWheel = ColorWheelForLyre(id),
                r = ChannelOr(lyreRChannels, id, lyreR),
                g = ChannelOr(lyreGChannels, id, 0),
                b = ChannelOr(lyreBChannels, id, 0),
                moveSpeed = moveSpeed,
            });
        }
    }

    byte ColorWheelForLyre(byte id)
    {
        int idx = id - 1;
        if (lyreColorWheels != null && idx >= 0 && idx < lyreColorWheels.Length)
            return lyreColorWheels[idx];
        return lyreColorWheel;
    }

    static byte ChannelOr(byte[] arr, byte id, byte fallback)
    {
        int idx = id - 1;
        if (arr != null && idx >= 0 && idx < arr.Length)
            return arr[idx];
        return fallback;
    }
}
