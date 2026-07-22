// Clip Timeline multi-devices : projecteur + 4 lyres sur UNE piste
// (Unity n’active qu’un clip par piste si chevauchement).

using UnityEngine;
using UnityEngine.Playables;

public class DeviceShowClip : PlayableAsset
{
    [Header("Projecteur RGBW (deviceId 0)")]
    public bool enableProjector = true;
    [Range(0, 255)] public int projectorDimmer = 255;
    [Range(0, 255)] public int projectorShutter = DeviceState.ShutterOpen;
    [Range(0, 255)] public int projectorR = 230;
    [Range(0, 255)] public int projectorG = 194;
    [Range(0, 255)] public int projectorB = 41;
    [Range(0, 255)] public int projectorW = 180;
    public bool projectorBlink = true;
    [Min(0.1f)] public float projectorBlinkHz = 2f;

    [Header("Lyres 1–4")]
    public bool enableLyres = true;
    [Range(0, 255)] public int lyreDimmer = 255;
    [Tooltip("40 = ouvert authoring (Hub mappe vers 255 DMX pour éviter le strobe)")]
    [Range(0, 255)] public int lyreShutter = DeviceState.ShutterOpen;
    [Tooltip("Fallback si le tableau ci-dessous est vide")]
    [Range(0, 255)] public int lyreColorWheel = 80;
    [Tooltip("Roue couleur par lyre (presets Hub : 80 rouge, 120 vert, 160 bleu, 40 blanc)")]
    public int[] lyreColorWheels = { 80, 120, 160, 40 };
    [Range(0, 255)] public int lyreR = 255;
    public bool autoSweep = true;
    [Min(0.01f)] public float sweepSpeed = 1.8f;
    [Range(1, 120)] public float sweepAmplitudePan = 110f;
    [Tooltip("0 = balayage horizontal seulement (recommandé pour coller au mur)")]
    [Range(0, 120)] public float sweepAmplitudeTilt = 0f;
    [Range(0, 255)] public int moveSpeed = 128;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DeviceShowBehaviour>.Create(graph);
        CopyTo(playable.GetBehaviour());
        return playable;
    }

    public void ApplyTo(DeviceManager deviceManager, float clipTime)
    {
        var b = new DeviceShowBehaviour();
        CopyTo(b);
        b.ApplyTo(deviceManager, clipTime);
    }

    private void CopyTo(DeviceShowBehaviour b)
    {
        if (b == null) return;
        b.enableProjector = enableProjector;
        b.projectorDimmer = (byte)Mathf.Clamp(projectorDimmer, 0, 255);
        b.projectorShutter = (byte)Mathf.Clamp(projectorShutter, 0, 255);
        b.projectorR = (byte)Mathf.Clamp(projectorR, 0, 255);
        b.projectorG = (byte)Mathf.Clamp(projectorG, 0, 255);
        b.projectorB = (byte)Mathf.Clamp(projectorB, 0, 255);
        b.projectorW = (byte)Mathf.Clamp(projectorW, 0, 255);
        b.projectorBlink = projectorBlink;
        b.projectorBlinkHz = projectorBlinkHz;

        b.enableLyres = enableLyres;
        b.lyreDimmer = (byte)Mathf.Clamp(lyreDimmer, 0, 255);
        b.lyreShutter = (byte)Mathf.Clamp(lyreShutter, 0, 255);
        b.lyreColorWheel = (byte)Mathf.Clamp(lyreColorWheel, 0, 255);
        b.lyreColorWheels = ToBytes(lyreColorWheels);
        b.lyreR = (byte)Mathf.Clamp(lyreR, 0, 255);
        b.lyreRChannels = new byte[] { 255, 0, 0, 255 };
        b.lyreGChannels = new byte[] { 0, 255, 0, 180 };
        b.lyreBChannels = new byte[] { 0, 0, 255, 0 };
        b.autoSweep = autoSweep;
        b.sweepSpeed = sweepSpeed;
        b.sweepAmplitudePan = sweepAmplitudePan;
        b.sweepAmplitudeTilt = sweepAmplitudeTilt;
        b.moveSpeed = (byte)Mathf.Clamp(moveSpeed, 0, 255);
    }

    static byte[] ToBytes(int[] src)
    {
        if (src == null || src.Length == 0)
            return new byte[] { 80, 120, 160, 40 };
        var dst = new byte[src.Length];
        for (int i = 0; i < src.Length; i++)
            dst[i] = (byte)Mathf.Clamp(src[i], 0, 255);
        return dst;
    }
}
