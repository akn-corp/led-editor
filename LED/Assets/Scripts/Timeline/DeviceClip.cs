// Assets/Scripts/Timeline/DeviceClip.cs
//
// Un clip de chorégraphie lumineuse pose sur la timeline : il pilote le
// projecteur central et les quatre lyres, en parallele des animations du mur.
//
// Comme pour WallEffectClip, tous les champs sont editables dans l'Inspector
// quand on selectionne le clip dans la fenetre Timeline.

using UnityEngine;
using UnityEngine.Playables;

/// <summary>Comportement des appareils pendant le clip.</summary>
public enum DeviceEffectKind
{
    Blackout,   // tout eteint — utile pour couper net
    Static,     // allume, fixe, a la position indiquee
    Chase,      // les appareils s'allument l'un apres l'autre sur le tempo
    Strobe,     // clignotement rapide de tous les appareils selectionnes
    Sweep,      // balayage : le pan oscille d'un bord a l'autre
}

public class DeviceClip : PlayableAsset
{
    [Header("Effet")]
    public DeviceEffectKind kind = DeviceEffectKind.Chase;

    [Header("Selection")]
    [Tooltip("Appareil unique vise. -1 = tous les appareils.")]
    public int targetDeviceId = -1;

    [Tooltip("Inclure le projecteur RGBW central (id 0) dans les effets de groupe.")]
    public bool includeCentral = true;

    [Header("Intensite")]
    [Range(0f, 1f)] public float dimmer = 1f;

    [Tooltip("Obturateur : 255 = ouvert. Laisser ouvert et piloter le dimmer.")]
    [Range(0, 255)] public int shutter = 255;

    [Header("Couleur")]
    public Color color = new Color(0.95f, 0.95f, 0.93f);

    [Tooltip("Canal blanc, separe du RGB sur ces appareils.")]
    [Range(0f, 1f)] public float white;

    [Range(0, 255)] public int colorWheel;

    [Header("Position")]
    [Range(0f, 1f)] public float pan = 0.5f;
    [Range(0f, 1f)] public float tilt = 0.5f;

    [Tooltip("Amplitude du balayage en mode Sweep, autour de la valeur de pan.")]
    [Range(0f, 0.5f)] public float sweepAmplitude = 0.25f;

    [Tooltip("Vitesse de deplacement envoyee aux lyres. 0 = le plus rapide.")]
    [Range(0, 255)] public int moveSpeed;

    [Header("Rythme")]
    [Min(1f)] public float bpm = 120f;

    [Tooltip("Nombre de flashs par temps en mode Strobe.")]
    [Min(1f)] public float strobeRate = 4f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DeviceBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();

        behaviour.kind = kind;
        behaviour.targetDeviceId = targetDeviceId;
        behaviour.includeCentral = includeCentral;
        behaviour.dimmer = dimmer;
        behaviour.shutter = shutter;
        behaviour.color = color;
        behaviour.white = white;
        behaviour.colorWheel = colorWheel;
        behaviour.pan = pan;
        behaviour.tilt = tilt;
        behaviour.sweepAmplitude = sweepAmplitude;
        behaviour.moveSpeed = moveSpeed;
        behaviour.bpm = Mathf.Max(1f, bpm);
        behaviour.strobeRate = Mathf.Max(1f, strobeRate);

        return playable;
    }
}
