// Assets/Scripts/Network/DeviceProtocol.cs
//
// Encodeur binaire du message DEVS (projecteur RGBW + lyres), aligne sur
// Docs/protocole-state.md.
//
// Volontairement place dans un fichier separe de StateProtocol.cs pour eviter
// les conflits de merge : le canal LEDS et le canal DEVS evoluent chacun de
// leur cote.
//
//   Entete (8 octets)          Bloc device (16 octets, x N)
//   0-3  magic "DEVS"          0  deviceId (0 = projecteur RGBW, 1-4 = lyres)
//   4    version = 1           1  pan          9  G
//   5-6  frameId (uint16 LE)   2  panFine     10  B
//   7    deviceCount (uint8)   3  tilt        11  W
//                              4  tiltFine    12  moveSpeed
//                              5  dimmer      13  function
//                              6  shutter     14-15 reserve (0)
//                              7  colorWheel
//                              8  R

using System.Collections.Generic;
using System.Text;

public static class DeviceProtocol
{
    public const int DevicePort = 6455;      // meme port que les chunks LEDS
    public const byte Version = 1;
    public const int HeaderSize = 8;
    public const int DeviceBlockSize = 16;

    private static readonly byte[] DevMagic = Encoding.ASCII.GetBytes("DEVS");

    /// <summary>
    /// Construit le paquet DEVS complet pour tous les appareils connus.
    /// Retourne null si la liste est vide.
    /// </summary>
    public static byte[] EncodeDeviceFrame(int frameId, IReadOnlyList<DeviceState> devices)
    {
        if (devices == null || devices.Count == 0) return null;

        int deviceCount = devices.Count > 255 ? 255 : devices.Count;
        var packet = new byte[HeaderSize + deviceCount * DeviceBlockSize];

        // --- Entete ---
        packet[0] = DevMagic[0];
        packet[1] = DevMagic[1];
        packet[2] = DevMagic[2];
        packet[3] = DevMagic[3];
        packet[4] = Version;

        ushort id = (ushort)(frameId & 0xFFFF);
        packet[5] = (byte)(id & 0xFF);          // little endian
        packet[6] = (byte)((id >> 8) & 0xFF);
        packet[7] = (byte)deviceCount;

        // --- Blocs device ---
        for (int i = 0; i < deviceCount; i++)
        {
            DeviceState d = devices[i];
            int offset = HeaderSize + i * DeviceBlockSize;

            packet[offset + 0] = d.DeviceId;
            packet[offset + 1] = d.Pan;
            packet[offset + 2] = d.PanFine;
            packet[offset + 3] = d.Tilt;
            packet[offset + 4] = d.TiltFine;
            packet[offset + 5] = d.Dimmer;
            packet[offset + 6] = d.Shutter;
            packet[offset + 7] = d.ColorWheel;
            packet[offset + 8] = d.R;
            packet[offset + 9] = d.G;
            packet[offset + 10] = d.B;
            packet[offset + 11] = d.W;
            packet[offset + 12] = d.MoveSpeed;
            packet[offset + 13] = d.Function;
            packet[offset + 14] = 0;            // reserve
            packet[offset + 15] = 0;            // reserve
        }

        return packet;
    }
}
