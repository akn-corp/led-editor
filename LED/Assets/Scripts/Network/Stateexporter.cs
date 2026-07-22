// Envoie le full state LEDS puis DEVS en UDP ~40 Hz vers led-routing-hub (port 6455).
// Ordre : tous les chunks LEDS, puis 1 paquet DEVS. frameId séparés.
// Hot path : LedFrameWriter (zéro alloc/frame) + horloge accumulateur (realtime).

using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class StateExporter : MonoBehaviour
{
    [SerializeField] private EntityManager entityManager;
    [SerializeField] private DeviceManager deviceManager;

    [Header("Cible réseau (led-routing-hub)")]
    [SerializeField] private string targetIp = "127.0.0.1";
    [SerializeField] private int targetPort = StateProtocol.StatePort;

    [SerializeField] private float sendIntervalSeconds = 1f / 40f;

    [Tooltip("Max frames rattrapées si le frame time dépasse l’intervalle (évite spiral of death).")]
    [SerializeField] private int maxCatchUpFrames = 2;

    [Header("Devices (DEVS)")]
    [SerializeField] private bool sendDevices = true;

    [Header("Debug")]
    [SerializeField] private bool logEveryFrame;

    private UdpClient _client;
    private IPEndPoint _endpoint;
    private readonly LedFrameWriter _writer = new LedFrameWriter();
    private double _nextSendTime = -1;
    private ushort _frameId;
    private ushort _devsFrameId;
    private bool _loggedFirstSend;
    private bool _loggedFirstDevs;

    void OnEnable()
    {
        if (entityManager == null)
        {
            Debug.LogError("[StateExporter] EntityManager manquant dans l'Inspector.");
            enabled = false;
            return;
        }

        if (sendDevices && deviceManager == null)
            Debug.LogWarning("[StateExporter] DeviceManager manquant — DEVS désactivé jusqu'à assignation.");

        if (targetPort != StateProtocol.StatePort)
        {
            Debug.LogWarning($"[StateExporter] targetPort corrigé {targetPort} → {StateProtocol.StatePort}");
            targetPort = StateProtocol.StatePort;
        }

        _client = new UdpClient();
        _endpoint = new IPEndPoint(IPAddress.Parse(targetIp), targetPort);
        _nextSendTime = -1;
        Debug.Log($"[StateExporter] Prêt — cible {targetIp}:{targetPort} @ {1f / sendIntervalSeconds:F0} Hz (devices={sendDevices}, zero-alloc)");
    }

    void OnDisable()
    {
        _client?.Close();
        _client = null;
        _nextSendTime = -1;
    }

    void Update()
    {
        if (entityManager == null || _client == null) return;

        float interval = Mathf.Max(0.001f, sendIntervalSeconds);
        double now = Time.realtimeSinceStartupAsDouble;

        if (_nextSendTime < 0)
            _nextSendTime = now;

        int sent = 0;
        int maxCatch = Mathf.Max(1, maxCatchUpFrames);
        while (now >= _nextSendTime && sent < maxCatch)
        {
            _nextSendTime += interval;
            SendFrame();
            sent++;
        }

        // Trop de retard : resync (évite rafale après hiccup éditeur / GC)
        if (_nextSendTime < now - interval)
            _nextSendTime = now;
    }

    void SendFrame()
    {
        int chunkCount = _writer.EncodeLeds(_frameId, entityManager);
        int currentFrameId = _frameId;
        _frameId++;

        if (chunkCount == 0) return;

        var packets = _writer.Packets;
        for (int i = 0; i < chunkCount; i++)
        {
            byte[] packet = packets[i];
            _client.Send(packet, packet.Length, _endpoint);
        }

        if (logEveryFrame || !_loggedFirstSend)
        {
            _loggedFirstSend = true;
            byte[] first = packets[0];
            Debug.Log(
                $"[StateExporter] LEDS frameId={currentFrameId}, chunks={chunkCount}, " +
                $"1er paquet={first.Length} octets, magic={System.Text.Encoding.ASCII.GetString(first, 0, 4)}");
        }

        if (!sendDevices || deviceManager == null) return;

        byte[] devs = _writer.EncodeDevs(_devsFrameId, deviceManager);
        int currentDevsFrameId = _devsFrameId;
        _devsFrameId++;

        int devsLen = StateProtocol.DevsHeaderSize + DeviceManager.DeviceCount * StateProtocol.DeviceBlockSize;
        _client.Send(devs, devsLen, _endpoint);

        if (logEveryFrame || !_loggedFirstDevs)
        {
            _loggedFirstDevs = true;
            Debug.Log(
                $"[StateExporter] DEVS frameId={currentDevsFrameId}, " +
                $"paquet={devsLen} octets, deviceCount={devs[7]}, " +
                $"magic={System.Text.Encoding.ASCII.GetString(devs, 0, 4)}");
        }
    }
}
