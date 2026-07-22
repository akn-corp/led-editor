// Assets/Scripts/Timeline/DeviceBehaviour.cs
//
// Donnees d'un clip DeviceClip pendant qu'il est joue, et calcul de l'etat
// d'un appareil donne a un instant donne.
//
// Evaluate() renvoie les valeurs en flottant (0..1) plutot qu'en octets :
// c'est le mixer qui convertit, apres avoir melange les clips qui se
// chevauchent. Melanger des octets deja quantifies donnerait des paliers
// visibles sur les fondus de dimmer.

using UnityEngine;
using UnityEngine.Playables;

/// <summary>Etat calcule d'un appareil, en valeurs normalisees.</summary>
public struct DeviceFrame
{
    public float Dimmer;
    public float Pan;
    public float Tilt;
    public float R, G, B, W;
    public float Shutter;
    public float ColorWheel;
    public float MoveSpeed;
}

public class DeviceBehaviour : PlayableBehaviour
{
    public DeviceEffectKind kind;
    public int targetDeviceId = -1;
    public bool includeCentral = true;

    public float dimmer = 1f;
    public int shutter = 255;
    public Color color;
    public float white;
    public int colorWheel;

    public float pan = 0.5f;
    public float tilt = 0.5f;
    public float sweepAmplitude = 0.25f;
    public int moveSpeed;

    public float bpm = 120f;
    public float strobeRate = 4f;

    /// <summary>
    /// Etat de l'appareil deviceId a localTime secondes apres le debut du clip.
    /// deviceId 0 = projecteur central, 1 a 4 = lyres.
    /// </summary>
    public DeviceFrame Evaluate(int deviceId, int deviceCount, float localTime)
    {
        var frame = new DeviceFrame
        {
            Pan = pan,
            Tilt = tilt,
            R = color.r,
            G = color.g,
            B = color.b,
            W = white,
            Shutter = shutter / 255f,
            ColorWheel = colorWheel / 255f,
            MoveSpeed = moveSpeed / 255f,
            Dimmer = 0f,
        };

        if (!IsTargeted(deviceId)) return frame;

        float beat = localTime * bpm / 60f;

        switch (kind)
        {
            case DeviceEffectKind.Blackout:
                frame.Dimmer = 0f;
                break;

            case DeviceEffectKind.Static:
                frame.Dimmer = dimmer;
                break;

            case DeviceEffectKind.Chase:
            {
                // Les appareils s'allument l'un apres l'autre, un par temps.
                int active = Mathf.FloorToInt(beat) % Mathf.Max(1, CountTargets(deviceCount));
                int rank = RankOf(deviceId);

                // Extinction rapide : la lumiere claque puis retombe.
                float decay = Mathf.Max(0f, 1f - (beat % 1f) * 2.5f);
                frame.Dimmer = (rank == active) ? dimmer * decay : 0f;
                break;
            }

            case DeviceEffectKind.Strobe:
            {
                float phase = (beat * strobeRate) % 1f;
                frame.Dimmer = (phase < 0.5f) ? dimmer : 0f;
                break;
            }

            case DeviceEffectKind.Sweep:
            {
                // Balayage lent d'un bord a l'autre, decale par appareil pour
                // que les faisceaux se croisent au lieu de bouger en bloc.
                float offset = RankOf(deviceId) * 0.35f;
                frame.Pan = Mathf.Clamp01(pan + Mathf.Sin(beat * Mathf.PI * 0.5f + offset) * sweepAmplitude);
                frame.Dimmer = dimmer;
                break;
            }
        }

        return frame;
    }

    private bool IsTargeted(int deviceId)
    {
        if (targetDeviceId >= 0) return deviceId == targetDeviceId;
        if (deviceId == DeviceManager.CentralProjectorId) return includeCentral;
        return true;
    }

    /// <summary>Position de l'appareil dans le groupe vise (pour les chenillards).</summary>
    private int RankOf(int deviceId)
    {
        if (targetDeviceId >= 0) return 0;
        return includeCentral ? deviceId : deviceId - 1;
    }

    private int CountTargets(int deviceCount)
    {
        if (targetDeviceId >= 0) return 1;
        return includeCentral ? deviceCount : deviceCount - 1;
    }
}
