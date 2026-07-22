// Clip Timeline device (projecteur RGBW ou lyre).
// Champs logiques uniquement — pas d'IP / univers / DMX.

using UnityEngine;
using UnityEngine.Playables;

public class DeviceClip : PlayableAsset
{
    [Range(0, 4)] public int deviceId = 1;

    [Header("Intensité")]
    [Range(0, 255)] public int dimmer = DeviceState.DimmerFull;
    [Range(0, 255)] public int shutter = DeviceState.ShutterOpen;
    [Range(0, 255)] public int colorWheel;

    [Header("Couleur (projecteur RGBW ; R utile lyre)")]
    [Range(0, 255)] public int r = 255;
    [Range(0, 255)] public int g;
    [Range(0, 255)] public int b;
    [Range(0, 255)] public int w = 255;

    [Header("Position (lyres)")]
    [Range(0, 255)] public int pan = DeviceState.Center;
    [Range(0, 255)] public int panFine;
    [Range(0, 255)] public int tilt = DeviceState.Center;
    [Range(0, 255)] public int tiltFine;
    [Range(0, 255)] public int moveSpeed = 128;
    [Range(0, 255)] public int function;

    [Header("AutoSweep (lyres tournantes)")]
    public bool autoSweep = true;
    [Min(0.01f)] public float sweepSpeed = 0.6f;
    [Range(1, 120)] public float sweepAmplitudePan = 60f;
    [Range(1, 120)] public float sweepAmplitudeTilt = 40f;

    [Header("Projecteur")]
    [Tooltip("Clignotement dimmer (utile pour deviceId 0)")]
    public bool blink;
    [Min(0.1f)] public float blinkHz = 3f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DeviceBehaviour>.Create(graph);
        CopyTo(playable.GetBehaviour());
        return playable;
    }

    public DeviceState EvaluateAt(float clipTime)
    {
        var b = new DeviceBehaviour();
        CopyTo(b);
        return b.Evaluate(clipTime);
    }

    private void CopyTo(DeviceBehaviour b)
    {
        if (b == null) return;
        b.deviceId = (byte)Mathf.Clamp(deviceId, 0, 4);
        b.dimmer = (byte)Mathf.Clamp(dimmer, 0, 255);
        b.shutter = (byte)Mathf.Clamp(shutter, 0, 255);
        b.colorWheel = (byte)Mathf.Clamp(colorWheel, 0, 255);
        b.r = (byte)Mathf.Clamp(r, 0, 255);
        b.g = (byte)Mathf.Clamp(g, 0, 255);
        b.b = (byte)Mathf.Clamp(this.b, 0, 255);
        b.w = (byte)Mathf.Clamp(w, 0, 255);
        b.pan = (byte)Mathf.Clamp(pan, 0, 255);
        b.panFine = (byte)Mathf.Clamp(panFine, 0, 255);
        b.tilt = (byte)Mathf.Clamp(tilt, 0, 255);
        b.tiltFine = (byte)Mathf.Clamp(tiltFine, 0, 255);
        b.moveSpeed = (byte)Mathf.Clamp(moveSpeed, 0, 255);
        b.function = (byte)Mathf.Clamp(function, 0, 255);
        b.autoSweep = autoSweep;
        b.sweepSpeed = sweepSpeed;
        b.sweepAmplitudePan = sweepAmplitudePan;
        b.sweepAmplitudeTilt = sweepAmplitudeTilt;
        b.blink = blink;
        b.blinkHz = blinkHz;
    }
}
