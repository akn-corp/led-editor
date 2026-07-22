// Encodeur LEDS/DEVS réutilisable — zéro alloc par frame une fois le layout figé.
// Contrat binaire inchangé (protocole-state.md).

using System;
using System.Collections.Generic;

public sealed class LedFrameWriter
{
    private StateProtocol.LedChunk[] _chunks = Array.Empty<StateProtocol.LedChunk>();
    private byte[][] _packets = Array.Empty<byte[]>();
    private int _chunkCount;
    private int _layoutEntityCount = -1;
    private int _wallLayoutVersion = -1;
    private bool _layoutFromWall;
    private byte[] _devsPacket;

    public int ChunkCount => _chunkCount;
    public byte[][] Packets => _packets;

    /// <summary>
    /// (Re)construit les buffers si le mapping mur / le nombre d’entités a changé.
    /// </summary>
    public void EnsureLayout(EntityManager entityManager)
    {
        if (entityManager == null) return;

        bool fromWall = WallMapping.IsInitialized;
        int entityCountHint = fromWall
            ? WallMapping.TotalEntityCount()
            : CountEntities(entityManager);
        int wallVersion = fromWall ? WallMapping.LayoutVersion : 0;

        if (_packets.Length > 0
            && fromWall == _layoutFromWall
            && entityCountHint == _layoutEntityCount
            && wallVersion == _wallLayoutVersion
            && _chunkCount > 0)
            return;

        IReadOnlyList<StateProtocol.LedChunk> defs = fromWall
            ? WallMapping.GetAllWallLedChunks()
            : StateProtocol.GetChunksForEntities(entityManager.AllEntityIds);

        _chunkCount = defs.Count;
        _chunks = new StateProtocol.LedChunk[_chunkCount];
        _packets = new byte[_chunkCount][];

        for (int i = 0; i < _chunkCount; i++)
        {
            var chunk = defs[i];
            _chunks[i] = chunk;
            _packets[i] = new byte[StateProtocol.LedHeaderSize + chunk.EntryCount * StateProtocol.LedEntrySize];
        }

        int devsSize = StateProtocol.DevsHeaderSize + DeviceManager.DeviceCount * StateProtocol.DeviceBlockSize;
        if (_devsPacket == null || _devsPacket.Length != devsSize)
            _devsPacket = new byte[devsSize];

        _layoutFromWall = fromWall;
        _layoutEntityCount = entityCountHint;
        _wallLayoutVersion = wallVersion;
    }

    /// <summary>
    /// Remplit les paquets LEDS préalloués. Retourne le nombre de chunks (0 si rien).
    /// </summary>
    public int EncodeLeds(int frameId, EntityManager entityManager)
    {
        EnsureLayout(entityManager);
        if (_chunkCount == 0 || entityManager == null) return 0;

        ushort fid = (ushort)(frameId & 0xffff);
        byte chunkCountByte = (byte)(_chunkCount & 0xff);

        for (int chunkIndex = 0; chunkIndex < _chunkCount; chunkIndex++)
        {
            var chunk = _chunks[chunkIndex];
            byte[] buf = _packets[chunkIndex];
            StateProtocol.WriteLedsChunkHeader(
                buf,
                fid,
                (byte)(chunkIndex & 0xff),
                chunkCountByte,
                (ushort)(chunk.StartEntityId & 0xffff),
                (ushort)(chunk.EntryCount & 0xffff));

            int offset = StateProtocol.LedHeaderSize;
            int startId = chunk.StartEntityId;
            int entryCount = chunk.EntryCount;

            for (int i = 0; i < entryCount; i++)
            {
                var state = entityManager.GetColor(startId + i);
                if (state != null)
                {
                    buf[offset] = state.R;
                    buf[offset + 1] = state.G;
                    buf[offset + 2] = state.B;
                }
                else
                {
                    buf[offset] = 0;
                    buf[offset + 1] = 0;
                    buf[offset + 2] = 0;
                }
                offset += StateProtocol.LedEntrySize;
            }
        }

        return _chunkCount;
    }

    /// <summary>Remplit le buffer DEVS préalloué (sans SnapshotAll).</summary>
    public byte[] EncodeDevs(int frameId, DeviceManager deviceManager)
    {
        if (_devsPacket == null)
        {
            int devsSize = StateProtocol.DevsHeaderSize + DeviceManager.DeviceCount * StateProtocol.DeviceBlockSize;
            _devsPacket = new byte[devsSize];
        }

        StateProtocol.EncodeDevsPacketInto(_devsPacket, frameId, deviceManager);
        return _devsPacket;
    }

    static int CountEntities(EntityManager entityManager)
    {
        int n = 0;
        foreach (var _ in entityManager.AllEntityIds)
            n++;
        return n;
    }
}
