// Mapping mur Glassworks — port de led-studio-editor/src/engine/wall-mapping.ts

using System.Collections.Generic;

public static class WallMapping
{
    public const int VisibleRows = 128;
    public const int ColumnsPerPhysicalBand = 2;
    public const int AscendingLastVisibleOffset = 128;
    public const int DescendingFirstVisibleOffset = 130;

    private static WallBandsConfig _config;
    private static List<StateProtocol.LedChunk> _cachedChunks;
    private static int _layoutVersion;

    public static bool IsInitialized => _config?.bands != null && _config.bands.Length > 0;

    /// <summary>Incrémenté à chaque Initialize — LedFrameWriter invalide ses buffers.</summary>
    public static int LayoutVersion => _layoutVersion;

    public static int Columns => _config?.columns ?? 0;

    public static int EntityIdStart =>
        _config?.bands != null && _config.bands.Length > 0
            ? _config.bands[0].entityStart
            : 100;

    public static void Initialize(WallBandsConfig config)
    {
        _config = config;
        _cachedChunks = null;
        _layoutVersion++;
    }

    /// <summary>
    /// Bande physique en U : 64 bandes de 259 LEDs (base cachée, 128 montant,
    /// sommet caché, 128 descendant, base cachée).
    /// </summary>
    public static int? EntityIdForCell(int row, int column)
    {
        if (_config?.bands == null) return null;
        if (row < 0 || row >= VisibleRows || column < 0 || column >= _config.columns) return null;

        int physicalBandIndex = column / ColumnsPerPhysicalBand;
        int bandIndex = physicalBandIndex * ColumnsPerPhysicalBand;
        if (bandIndex >= _config.bands.Length) return null;

        var firstUniverse = _config.bands[bandIndex];

        if (column % ColumnsPerPhysicalBand == 0)
            return firstUniverse.entityStart + AscendingLastVisibleOffset - row;

        return firstUniverse.entityStart + DescendingFirstVisibleOffset + row;
    }

    /// <summary>
    /// Plages contiguës par bande (colonne) — aligné sur led-studio-editor et le router.
    /// </summary>
    public static IReadOnlyList<StateProtocol.LedChunk> GetAllWallLedChunks()
    {
        if (_cachedChunks != null) return _cachedChunks;

        _cachedChunks = new List<StateProtocol.LedChunk>();
        if (_config?.bands == null) return _cachedChunks;

        foreach (var band in _config.bands)
        {
            int end = band.entityStart + band.entityCount - 1;
            AppendChunksForRange(_cachedChunks, band.entityStart, end);
        }

        return _cachedChunks;
    }

    public static int TotalEntityCount()
    {
        if (_config?.bands == null) return 0;
        int total = 0;
        foreach (var band in _config.bands)
            total += band.entityCount;
        return total;
    }

    public static void RegisterAllEntities(EntityManager entityManager)
    {
        if (_config?.bands == null) return;
        foreach (var band in _config.bands)
        {
            for (int id = band.entityStart; id < band.entityStart + band.entityCount; id++)
                entityManager.RegisterEntity(id);
        }
    }

    private static void AppendChunksForRange(List<StateProtocol.LedChunk> chunks, int start, int end)
    {
        int cursor = start;
        while (cursor <= end)
        {
            int count = System.Math.Min(StateProtocol.MaxLedEntriesPerChunk, end - cursor + 1);
            chunks.Add(new StateProtocol.LedChunk { StartEntityId = cursor, EntryCount = count });
            cursor += count;
        }
    }
}
