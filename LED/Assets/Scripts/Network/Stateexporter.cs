// Envoie l'état complet du mur LED au logiciel de routage via le protocole
// maison attendu par le back : UDP port 6455, magic "LEDS", version 1,
// chunking de 400 entrées max, full state.

using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class StateExporter : MonoBehaviour
{
    [SerializeField] private EntityManager entityManager;

    [Header("Cible réseau du routage")]
    [Tooltip("IP du back led-routing-hub")]
    [SerializeField] private string targetIp = "127.0.0.1";
    [Tooltip("Port UDP utilisé par le back. Doit être 6455.")]
    [SerializeField] private int targetPort = 6455; // back led-routing-hub attend 6455

    [SerializeField] private float sendIntervalSeconds = 1f / 40f; // ~40Hz

    private const int LedHeaderSize = 13;
    private const int MaxLedEntriesPerChunk = 400;
    private const byte ProtocolVersion = 1;

    private UdpClient _client;
    private IPEndPoint _endpoint;
    private float _timer;
    private ushort _frameId;

    void OnValidate()
    {
        if (targetPort != 6455)
        {
            targetPort = 6455;
            Debug.LogWarning("[StateExporter] targetPort forcé à 6455. Le routage back écoute sur UDP 6455.");
        }
    }

    void OnEnable()
    {
        if (entityManager == null)
        {
            Debug.LogError("[StateExporter] EntityManager manquant dans l'Inspector.");
            enabled = false;
            return;
        }

        if (targetPort != 6455)
        {
            Debug.LogWarning("[StateExporter] targetPort incorrect, correction vers 6455.");
            targetPort = 6455;
        }

        _client = new UdpClient();
        _endpoint = new IPEndPoint(IPAddress.Parse(targetIp), targetPort);
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
        _timer -= sendIntervalSeconds;

        SendFullState();
    }

    private void SendFullState()
    {
        var ids = new List<int>(entityManager.AllEntityIds);
        if (ids.Count == 0) return;

        ids.Sort();
        var chunks = BuildContiguousChunks(ids);
        if (chunks.Count == 0) return;

        var currentFrameId = _frameId;
        for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
        {
            var packet = BuildLedsPacket(currentFrameId, chunkIndex, chunks.Count, ids, chunks[chunkIndex]);
            _client.Send(packet, packet.Length, _endpoint);
        }

        Debug.Log($"[StateExporter] Envoyé frame={currentFrameId} entités={ids.Count} chunks={chunks.Count} vers {targetIp}:{targetPort}");

        _frameId = (ushort)((currentFrameId + 1) & 0xffff);
    }

    private struct LedChunk
    {
        public int startEntityId;
        public int entryCount;
    }

    private List<LedChunk> BuildContiguousChunks(List<int> sortedIds)
    {
        var chunks = new List<LedChunk>();
        if (sortedIds.Count == 0) return chunks;

        int startId = sortedIds[0];
        int lastId = startId;
        int count = 1;

        for (int i = 1; i < sortedIds.Count; i++)
        {
            int id = sortedIds[i];
            if (id == lastId + 1 && count < MaxLedEntriesPerChunk)
            {
                lastId = id;
                count++;
                continue;
            }

            chunks.Add(new LedChunk { startEntityId = startId, entryCount = count });
            startId = id;
            lastId = id;
            count = 1;
        }

        chunks.Add(new LedChunk { startEntityId = startId, entryCount = count });
        return chunks;
    }

    private byte[] BuildLedsPacket(ushort frameId, int chunkIndex, int chunkCount, List<int> sortedIds, LedChunk chunk)
    {
        int packetSize = LedHeaderSize + chunk.entryCount * 3;
        var packet = new byte[packetSize];

        packet[0] = (byte)'L';
        packet[1] = (byte)'E';
        packet[2] = (byte)'D';
        packet[3] = (byte)'S';
        packet[4] = ProtocolVersion;

        packet[5] = (byte)(frameId & 0xff);
        packet[6] = (byte)((frameId >> 8) & 0xff);
        packet[7] = (byte)(chunkIndex & 0xff);
        packet[8] = (byte)(chunkCount & 0xff);

        packet[9] = (byte)(chunk.startEntityId & 0xff);
        packet[10] = (byte)((chunk.startEntityId >> 8) & 0xff);
        packet[11] = (byte)(chunk.entryCount & 0xff);
        packet[12] = (byte)((chunk.entryCount >> 8) & 0xff);

        int writeOffset = LedHeaderSize;
        for (int i = 0; i < chunk.entryCount; i++)
        {
            int entityId = chunk.startEntityId + i;
            var state = entityManager.GetColor(entityId);
            packet[writeOffset++] = state != null ? state.R : (byte)0;
            packet[writeOffset++] = state != null ? state.G : (byte)0;
            packet[writeOffset++] = state != null ? state.B : (byte)0;
        }

        return packet;
    }
}
