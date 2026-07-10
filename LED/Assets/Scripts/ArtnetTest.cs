// Test de connectivité ArtNet — diagnostic uniquement, sous forme de MonoBehaviour Unity.
//
// Usage :
//   1. Créer un GameObject vide dans une scène de test.
//   2. Attacher ce script dessus.
//   3. Ajuster ControllerIp dans l'Inspector si besoin.
//   4. Lancer le Play Mode.
//

using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class ArtnetTest : MonoBehaviour
{
    [Header("Paramètres réseau")]
    [SerializeField] private string controllerIp = "192.168.1.45";
    [SerializeField] private int artnetPort = 6454;
    [SerializeField] private int universe = 0;

    [Header("Couleur de test (canaux 1, 2, 3)")]
    [SerializeField] private byte red = 255;
    [SerializeField] private byte green = 0;
    [SerializeField] private byte blue = 0;

    [SerializeField] private float sendIntervalSeconds = 1f;

    private const int DmxUniverseSize = 512;
    private UdpClient _client;
    private IPEndPoint _endpoint;
    private byte[] _packet;
    private float _timer;

    void Start()
    {
        _client = new UdpClient();
        _endpoint = new IPEndPoint(IPAddress.Parse(controllerIp), artnetPort);

        var dmxData = new byte[DmxUniverseSize];
        dmxData[0] = red;
        dmxData[1] = green;
        dmxData[2] = blue;

        _packet = BuildArtnetDmxPacket(universe, dmxData);

        Debug.Log($"[ArtnetTest] Cible : {controllerIp}:{artnetPort}, univers {universe}, couleur ({red},{green},{blue})");
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= sendIntervalSeconds)
        {
            _timer = 0f;
            _client.Send(_packet, _packet.Length, _endpoint);
            Debug.Log($"[ArtnetTest] Paquet envoyé ({_packet.Length} octets)");
        }
    }

    void OnDestroy()
    {
        _client?.Close();
    }

    private static byte[] BuildArtnetDmxPacket(int universeId, byte[] dmxData)
    {
        var header = System.Text.Encoding.ASCII.GetBytes("Art-Net\0");
        byte[] opcode = { 0x00, 0x50 };
        byte[] protocolVersion = { 0x00, 0x0E };
        byte sequence = 0;
        byte physical = 0;
        byte subUni = (byte)(universeId & 0xFF);
        byte net = (byte)((universeId >> 8) & 0xFF);
        byte[] length = {
            (byte)((dmxData.Length >> 8) & 0xFF),
            (byte)(dmxData.Length & 0xFF)
        };

        using var stream = new System.IO.MemoryStream();
        stream.Write(header, 0, header.Length);
        stream.Write(opcode, 0, opcode.Length);
        stream.Write(protocolVersion, 0, protocolVersion.Length);
        stream.WriteByte(sequence);
        stream.WriteByte(physical);
        stream.WriteByte(subUni);
        stream.WriteByte(net);
        stream.Write(length, 0, length.Length);
        stream.Write(dmxData, 0, dmxData.Length);
        return stream.ToArray();
    }
}