// Données runtime d'un DeviceClip + évaluation AutoSweep pan/tilt.

using UnityEngine;
using UnityEngine.Playables;

public class DeviceBehaviour : PlayableBehaviour
{
    public byte deviceId = 1;
    public byte dimmer = DeviceState.DimmerFull;
    public byte shutter = DeviceState.ShutterOpen;
    public byte colorWheel;
    public byte r = 255;
    public byte g;
    public byte b;
    public byte w = 255;
    public byte pan = DeviceState.Center;
    public byte panFine;
    public byte tilt = DeviceState.Center;
    public byte tiltFine;
    public byte moveSpeed = 128;
    public byte function;

    public bool autoSweep = true;
    public float sweepSpeed = 0.6f;
    public float sweepAmplitudePan = 60f;
    public float sweepAmplitudeTilt = 40f;
    public bool blink;
    public float blinkHz = 3f;

    public DeviceState Evaluate(float clipTime)
    {
        byte outPan = pan;
        byte outTilt = tilt;
        byte outDimmer = dimmer;

        if (autoSweep && deviceId >= 1)
        {
            float phase = deviceId * 1.1f;
            float p = DeviceState.Center + Mathf.Sin(clipTime * sweepSpeed + phase) * sweepAmplitudePan;
            float t = DeviceState.Center + Mathf.Cos(clipTime * sweepSpeed * 0.85f + phase) * sweepAmplitudeTilt;
            outPan = (byte)Mathf.Clamp(Mathf.RoundToInt(p), 0, 255);
            outTilt = (byte)Mathf.Clamp(Mathf.RoundToInt(t), 0, 255);
        }

        byte outR = r, outG = g, outB = b, outW = w;

        if (blink)
        {
            // Clignotement franc ON/OFF
            float phase = clipTime * blinkHz;
            bool on = (phase - Mathf.Floor(phase)) < 0.55f;
            if (!on)
            {
                outDimmer = 0;
                // deviceId 0 (projecteur) : hub n'applique que RGBW — couper aussi les couleurs
                if (deviceId == 0)
                    outR = outG = outB = outW = 0;
            }
        }

        return new DeviceState
        {
            deviceId = deviceId,
            pan = outPan,
            panFine = panFine,
            tilt = outTilt,
            tiltFine = tiltFine,
            dimmer = outDimmer,
            shutter = shutter,
            colorWheel = colorWheel,
            r = outR,
            g = outG,
            b = outB,
            w = outW,
            moveSpeed = moveSpeed,
            function = function,
        };
    }
}
