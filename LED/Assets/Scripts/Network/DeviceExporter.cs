// Assets/Scripts/Network/DeviceExporter.cs
//
// Envoie l'etat des 5 appareils (projecteur RGBW + 4 lyres) en UDP a 40 Hz
// vers led-routing-hub, sous forme de paquets DEVS.
//
// Composant separe du StateExporter volontairement : le protocole precise que
// LEDS et DEVS ont chacun leur propre compteur de frameId, et que le receiver
// ne valide pas l'ordre d'arrivee. Les deux canaux peuvent donc vivre
// independamment, ce qui evite de toucher au fichier de Dev 2.

using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class DeviceExporter : MonoBehaviour
{
    [SerializeField] private DeviceManager deviceManager;

    [Header("Cible reseau (led-routing-hub)")]
    [SerializeField] private string targetIp = "127.0.0.1";
    [SerializeField] private int targetPort = DeviceProtocol.DevicePort;

    [SerializeField] private float sendIntervalSeconds = 1f / 40f;

    [Header("Debug")]
    [SerializeField] private bool logEveryFrame;

    private UdpClient _client;
    private IPEndPoint _endpoint;
    private float _timer;
    private ushort _frameId;
    private bool _loggedFirstSend;

    void OnEnable()
    {
        if (deviceManager == null)
        {
            Debug.LogError("[DeviceExporter] DeviceManager manquant dans l'Inspector.");
            enabled = false;
            return;
        }

        if (targetPort != DeviceProtocol.DevicePort)
        {
            Debug.LogWarning($"[DeviceExporter] targetPort corrige {targetPort} -> {DeviceProtocol.DevicePort}");
            targetPort = DeviceProtocol.DevicePort;
        }

        _client = new UdpClient();
        _endpoint = new IPEndPoint(IPAddress.Parse(targetIp), targetPort);
        Debug.Log($"[DeviceExporter] Pret — cible {targetIp}:{targetPort} @ {1f / sendIntervalSeconds:F0} Hz");
    }

    void OnDisable()
    {
        // Securite scenique : on eteint avant de couper la liaison, sinon les
        // lyres restent figees sur leur derniere valeur.
        SendBlackout();

        _client?.Close();
        _client = null;
    }

    void LateUpdate()
    {
        _timer += Time.deltaTime;
        if (_timer < sendIntervalSeconds) return;
        _timer = 0f;

        if (deviceManager == null || _client == null) return;

        byte[] packet = DeviceProtocol.EncodeDeviceFrame(_frameId, deviceManager.Devices);
        int currentFrameId = _frameId;
        _frameId++;

        if (packet == null) return;

        _client.Send(packet, packet.Length, _endpoint);

        if (logEveryFrame || !_loggedFirstSend)
        {
            _loggedFirstSend = true;
            Debug.Log(
                $"[DeviceExporter] DEVS frameId={currentFrameId}, " +
                $"{deviceManager.Devices.Count} appareils, {packet.Length} octets");
        }
    }

    private void SendBlackout()
    {
        if (_client == null || deviceManager == null) return;

        deviceManager.BlackoutAll();
        byte[] packet = DeviceProtocol.EncodeDeviceFrame(_frameId, deviceManager.Devices);
        if (packet != null) _client.Send(packet, packet.Length, _endpoint);
    }
}
