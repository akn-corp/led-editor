// Encodeur binaire LEDS + DEVS (UDP :6455) — aligné sur led-routing-hub/docs/protocole-state.md
// et led-studio-editor/electron/protocol.ts.
//
// Hot path Play Mode : LedFrameWriter (buffers préalloués, zéro alloc/frame).
// Les méthodes Encode* ci-dessous restent disponibles (tests / outils) mais allouent.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class StateProtocol
{
    public const int StatePort = 6455;
    public const int LedHeaderSize = 13;
    public const int LedEntrySize = 3;
    public const int MaxLedEntriesPerChunk = 400;
    public const int DevsHeaderSize = 8;
    public const int DeviceBlockSize = 16;

    private static readonly byte[] LedMagic = Encoding.ASCII.GetBytes("LEDS");
    private static readonly byte[] DevsMagic = Encoding.ASCII.GetBytes("DEVS");

    public struct Rgb
    {
        public byte R;
        public byte G;
        public byte B;

        public static Rgb Black => new Rgb { R = 0, G = 0, B = 0 };
    }

    public struct LedChunk
    {
        public int StartEntityId;
        public int EntryCount;
    }

    /// <summary>
    /// Découpe les IDs triés en plages contiguës, puis en chunks de max 400 entrées.
    /// </summary>
    public static List<LedChunk> GetChunksForEntities(IEnumerable<int> entityIds)
    {
        var sorted = entityIds.OrderBy(id => id).ToList();
        var chunks = new List<LedChunk>();
        if (sorted.Count == 0) return chunks;

        int rangeStart = sorted[0];
        int rangeEnd = sorted[0];

        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] == rangeEnd + 1)
            {
                rangeEnd = sorted[i];
                continue;
            }

            AppendChunksForRange(chunks, rangeStart, rangeEnd);
            rangeStart = rangeEnd = sorted[i];
        }

        AppendChunksForRange(chunks, rangeStart, rangeEnd);
        return chunks;
    }

    private static void AppendChunksForRange(List<LedChunk> chunks, int start, int end)
    {
        int cursor = start;
        while (cursor <= end)
        {
            int count = Math.Min(MaxLedEntriesPerChunk, end - cursor + 1);
            chunks.Add(new LedChunk { StartEntityId = cursor, EntryCount = count });
            cursor += count;
        }
    }

    public static void WriteLedsChunkHeader(
        byte[] buf,
        ushort frameId,
        byte chunkIndex,
        byte chunkCount,
        ushort startEntityId,
        ushort entryCount)
    {
        Buffer.BlockCopy(LedMagic, 0, buf, 0, 4);
        buf[4] = 1;
        WriteUInt16LE(buf, 5, frameId);
        buf[7] = chunkIndex;
        buf[8] = chunkCount;
        WriteUInt16LE(buf, 9, startEntityId);
        WriteUInt16LE(buf, 11, entryCount);
    }

    public static byte[] EncodeLedsChunk(
        int frameId,
        int chunkIndex,
        int chunkCount,
        int startEntityId,
        Rgb[] colors)
    {
        int entryCount = colors.Length;
        var buf = new byte[LedHeaderSize + entryCount * LedEntrySize];

        WriteLedsChunkHeader(
            buf,
            (ushort)(frameId & 0xffff),
            (byte)(chunkIndex & 0xff),
            (byte)(chunkCount & 0xff),
            (ushort)(startEntityId & 0xffff),
            (ushort)(entryCount & 0xffff));

        int offset = LedHeaderSize;
        for (int i = 0; i < entryCount; i++)
        {
            buf[offset] = colors[i].R;
            buf[offset + 1] = colors[i].G;
            buf[offset + 2] = colors[i].B;
            offset += LedEntrySize;
        }

        return buf;
    }

    /// <summary>
    /// Full state allouant (tests). Préférer <see cref="LedFrameWriter"/> en Play Mode.
    /// </summary>
    public static List<byte[]> EncodeLedFrame(int frameId, EntityManager entityManager)
    {
        var writer = new LedFrameWriter();
        int n = writer.EncodeLeds(frameId, entityManager);
        var packets = new List<byte[]>(n);
        for (int i = 0; i < n; i++)
        {
            byte[] src = writer.Packets[i];
            var copy = new byte[src.Length];
            Buffer.BlockCopy(src, 0, copy, 0, src.Length);
            packets.Add(copy);
        }
        return packets;
    }

    /// <summary>
    /// Un paquet DEVS (pas de chunking) — 8 + N×16 octets. frameId indépendant de LEDS.
    /// </summary>
    public static byte[] EncodeDevsPacket(int frameId, DeviceManager deviceManager)
    {
        int count = deviceManager != null ? DeviceManager.DeviceCount : 0;
        var buf = new byte[DevsHeaderSize + count * DeviceBlockSize];
        EncodeDevsPacketInto(buf, frameId, deviceManager);
        return buf;
    }

    public static byte[] EncodeDevsPacket(int frameId, DeviceState[] devices)
    {
        int count = devices != null ? Math.Min(devices.Length, 255) : 0;
        var buf = new byte[DevsHeaderSize + count * DeviceBlockSize];
        EncodeDevsPacketInto(buf, frameId, devices, count);
        return buf;
    }

    public static int EncodeDevsPacketInto(byte[] buf, int frameId, DeviceManager deviceManager)
    {
        int count = deviceManager != null ? DeviceManager.DeviceCount : 0;
        if (buf == null || buf.Length < DevsHeaderSize + count * DeviceBlockSize)
            throw new ArgumentException("Buffer DEVS trop petit", nameof(buf));

        Buffer.BlockCopy(DevsMagic, 0, buf, 0, 4);
        buf[4] = 1;
        WriteUInt16LE(buf, 5, (ushort)(frameId & 0xffff));
        buf[7] = (byte)(count & 0xff);

        int offset = DevsHeaderSize;
        for (int i = 0; i < count; i++)
        {
            var d = deviceManager.GetDevice((byte)i);
            WriteDeviceBlock(buf, offset, d);
            offset += DeviceBlockSize;
        }

        return DevsHeaderSize + count * DeviceBlockSize;
    }

    public static int EncodeDevsPacketInto(byte[] buf, int frameId, DeviceState[] devices, int count)
    {
        if (buf == null || buf.Length < DevsHeaderSize + count * DeviceBlockSize)
            throw new ArgumentException("Buffer DEVS trop petit", nameof(buf));

        Buffer.BlockCopy(DevsMagic, 0, buf, 0, 4);
        buf[4] = 1;
        WriteUInt16LE(buf, 5, (ushort)(frameId & 0xffff));
        buf[7] = (byte)(count & 0xff);

        int offset = DevsHeaderSize;
        for (int i = 0; i < count; i++)
        {
            WriteDeviceBlock(buf, offset, devices[i]);
            offset += DeviceBlockSize;
        }

        return DevsHeaderSize + count * DeviceBlockSize;
    }

    static void WriteDeviceBlock(byte[] buf, int offset, DeviceState d)
    {
        buf[offset] = d.deviceId;
        buf[offset + 1] = d.pan;
        buf[offset + 2] = d.panFine;
        buf[offset + 3] = d.tilt;
        buf[offset + 4] = d.tiltFine;
        buf[offset + 5] = d.dimmer;
        buf[offset + 6] = d.shutter;
        buf[offset + 7] = d.colorWheel;
        buf[offset + 8] = d.r;
        buf[offset + 9] = d.g;
        buf[offset + 10] = d.b;
        buf[offset + 11] = d.w;
        buf[offset + 12] = d.moveSpeed;
        buf[offset + 13] = d.function;
        WriteUInt16LE(buf, offset + 14, 0);
    }

    private static void WriteUInt16LE(byte[] buf, int offset, ushort value)
    {
        buf[offset] = (byte)(value & 0xff);
        buf[offset + 1] = (byte)((value >> 8) & 0xff);
    }
}
