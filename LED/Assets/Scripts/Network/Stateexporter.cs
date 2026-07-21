// Envoie le full state LEDS en UDP à 40 Hz vers led-routing-hub (port 6455).
// Chaque frame contient tous les chunks LEDS (pas de delta).

using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class StateExporter : MonoBehaviour
{
    [SerializeField] private EntityManager entityManager;

    [Header("Cible réseau (led-routing-hub)")]
    [SerializeField] private string targetIp = "127.0.0.1";
    [SerializeField] private int targetPort = StateProtocol.StatePort;

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
        if (entityManager == null)
        {
            Debug.LogError("[StateExporter] EntityManager manquant dans l'Inspector.");
            enabled = false;
            return;
        }

        if (targetPort != StateProtocol.StatePort)
        {
            Debug.LogWarning($"[StateExporter] targetPort corrigé {targetPort} → {StateProtocol.StatePort}");
            targetPort = StateProtocol.StatePort;
        }

        _client = new UdpClient();
        _endpoint = new IPEndPoint(IPAddress.Parse(targetIp), targetPort);
        Debug.Log($"[StateExporter] Prêt — cible {targetIp}:{targetPort} @ {1f / sendIntervalSeconds:F0} Hz");
    }

    void OnDisable()
    {
        _client?.Close();
        _client = null;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < sendIntervalSeconds) return;
        _timer = 0f;

        if (entityManager == null || _client == null) return;

        List<byte[]> packets = StateProtocol.EncodeLedFrame(_frameId, entityManager);
        int currentFrameId = _frameId;
        _frameId++;

        if (packets.Count == 0) return;

        foreach (byte[] packet in packets)
        {
            _client.Send(packet, packet.Length, _endpoint);
        }

        if (logEveryFrame || !_loggedFirstSend)
        {
            _loggedFirstSend = true;
            byte[] first = packets[0];
            Debug.Log(
                $"[StateExporter] LEDS frameId={currentFrameId}, chunks={packets.Count}, " +
                $"1er paquet={first.Length} octets, magic={System.Text.Encoding.ASCII.GetString(first, 0, 4)}");
        }
    }
}
