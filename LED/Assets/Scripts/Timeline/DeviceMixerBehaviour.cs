// Assets/Scripts/Timeline/DeviceMixerBehaviour.cs
//
// Chef d'orchestre de la piste des lyres. Melange les clips actifs ponderes
// par GetInputWeight, puis ecrit les octets dans le DeviceManager.
//
// Le melange se fait en flottant, avant quantification : c'est ce qui permet
// des fondus de dimmer et des transitions de position propres quand deux clips
// se chevauchent dans la timeline.

using UnityEngine;
using UnityEngine.Playables;

public class DeviceMixerBehaviour : PlayableBehaviour
{
    private DeviceFrame[] _accumulator;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var deviceManager = playerData as DeviceManager;
        if (deviceManager == null) return;

        int deviceCount = deviceManager.Devices.Count;
        if (deviceCount == 0) return;

        if (_accumulator == null || _accumulator.Length != deviceCount)
            _accumulator = new DeviceFrame[deviceCount];

        for (int i = 0; i < deviceCount; i++)
            _accumulator[i] = default;

        int inputCount = playable.GetInputCount();
        float totalWeight = 0f;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var inputPlayable = (ScriptPlayable<DeviceBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();
            if (behaviour == null) continue;

            totalWeight += weight;
            float localTime = (float)inputPlayable.GetTime();

            for (int d = 0; d < deviceCount; d++)
            {
                DeviceFrame f = behaviour.Evaluate(d, deviceCount, localTime);
                Accumulate(ref _accumulator[d], f, weight);
            }
        }

        if (totalWeight <= 0f) return;

        // Les positions et couleurs sont des moyennes ponderees ; le dimmer,
        // lui, est deja cumule (deux clips a 50 % donnent bien 100 %).
        float normalize = 1f / totalWeight;

        for (int d = 0; d < deviceCount; d++)
        {
            DeviceState device = deviceManager.Get(d);
            if (device == null) continue;

            DeviceFrame f = _accumulator[d];

            device.Dimmer = ToByte(f.Dimmer);
            device.Pan = ToByte(f.Pan * normalize);
            device.Tilt = ToByte(f.Tilt * normalize);
            device.R = ToByte(f.R * normalize);
            device.G = ToByte(f.G * normalize);
            device.B = ToByte(f.B * normalize);
            device.W = ToByte(f.W * normalize);
            device.Shutter = ToByte(f.Shutter * normalize);
            device.ColorWheel = ToByte(f.ColorWheel * normalize);
            device.MoveSpeed = ToByte(f.MoveSpeed * normalize);
        }
    }

    private static void Accumulate(ref DeviceFrame target, DeviceFrame source, float weight)
    {
        target.Dimmer += source.Dimmer * weight;
        target.Pan += source.Pan * weight;
        target.Tilt += source.Tilt * weight;
        target.R += source.R * weight;
        target.G += source.G * weight;
        target.B += source.B * weight;
        target.W += source.W * weight;
        target.Shutter += source.Shutter * weight;
        target.ColorWheel += source.ColorWheel * weight;
        target.MoveSpeed += source.MoveSpeed * weight;
    }

    private static byte ToByte(float normalized)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(normalized * 255f), 0, 255);
    }

    /// <summary>Extinction propre quand la timeline s'arrete.</summary>
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        // Le DeviceManager est atteint via le binding, indisponible ici :
        // l'extinction de securite est faite par DeviceExporter.OnDisable().
    }
}
