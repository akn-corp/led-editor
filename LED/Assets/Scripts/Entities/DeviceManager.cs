// Détient l'état runtime des devices logiques (DEVS).
// deviceId 0 = projecteur RGBW ; 1–4 = lyres.
// Parallèle à EntityManager — ne mélange pas mur LED et devices.

using UnityEngine;

[ExecuteAlways]
public class DeviceManager : MonoBehaviour
{
    public const int DeviceCount = 5;

    private readonly DeviceState[] _devices = new DeviceState[DeviceCount];

    void OnEnable()
    {
        EnsureDefaults();
    }

    void Awake()
    {
        EnsureDefaults();
    }

    public void EnsureDefaults()
    {
        for (byte id = 0; id < DeviceCount; id++)
            _devices[id] = DeviceState.Blackout(id);
    }

    public void SetDevice(DeviceState state)
    {
        if (state.deviceId >= DeviceCount) return;
        _devices[state.deviceId] = state;
    }

    public void SetDevice(byte deviceId, DeviceState state)
    {
        if (deviceId >= DeviceCount) return;
        state.deviceId = deviceId;
        _devices[deviceId] = state;
    }

    public DeviceState GetDevice(byte deviceId)
    {
        if (deviceId >= DeviceCount)
            return DeviceState.Blackout(deviceId);
        return _devices[deviceId];
    }

    public DeviceState[] SnapshotAll()
    {
        var copy = new DeviceState[DeviceCount];
        for (int i = 0; i < DeviceCount; i++)
            copy[i] = _devices[i];
        return copy;
    }
}
