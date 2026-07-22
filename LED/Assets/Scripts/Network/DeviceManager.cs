// Assets/Scripts/Network/DeviceManager.cs
//
// Detient les 5 appareils d'eclairage de l'installation : le projecteur RGBW
// central (id 0) et les quatre lyres (id 1 a 4).
//
// Meme role que EntityManager pour les LED : c'est le point central que la
// timeline modifie et que l'export reseau lit. Cette classe ne connait ni le
// DMX, ni les univers, ni les adresses — uniquement des identifiants d'appareil.

using System.Collections.Generic;
using UnityEngine;

public class DeviceManager : MonoBehaviour
{
    /// <summary>Projecteur RGBW central + 4 lyres.</summary>
    public const int DeviceCount = 5;

    public const byte CentralProjectorId = 0;
    public const byte FirstLyreId = 1;
    public const byte LastLyreId = 4;

    private readonly List<DeviceState> _devices = new List<DeviceState>();

    public IReadOnlyList<DeviceState> Devices => _devices;

    void Awake()
    {
        if (_devices.Count == 0) BuildDevices();
    }

    private void BuildDevices()
    {
        _devices.Clear();
        for (byte id = 0; id < DeviceCount; id++)
            _devices.Add(new DeviceState(id));
    }

    /// <summary>Appareil par identifiant, ou null si l'id est hors plage.</summary>
    public DeviceState Get(int deviceId)
    {
        if (_devices.Count == 0) BuildDevices();
        if (deviceId < 0 || deviceId >= _devices.Count) return null;
        return _devices[deviceId];
    }

    /// <summary>Eteint tous les appareils (dimmer a zero).</summary>
    public void BlackoutAll()
    {
        if (_devices.Count == 0) BuildDevices();
        foreach (var device in _devices) device.Dimmer = 0;
    }
}
