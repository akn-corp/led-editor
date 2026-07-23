// Mapping mur Glassworks — port de led-studio-editor/src/engine/wall-mapping.ts
// Supporte profil plein 128×128 et viewport (ex. 32×32 centré via originRow).

using System.Collections.Generic;

public static class WallMapping
{
    public const int ColumnsPerPhysicalBand = 2;
    public const int DefaultVisibleRows = 128;
    public const int DefaultAscendingLastVisibleOffset = 128;
    public const int DefaultDescendingFirstVisibleOffset = 130;

    private static WallBandsConfig _config;
    private static List<StateProtocol.LedChunk> _cachedChunks;
    private static int _layoutVersion;
    private static int _visibleRows = DefaultVisibleRows;
    private static int _originRow;
    private static int _ascendingLastVisibleOffset = DefaultAscendingLastVisibleOffset;
    private static int _descendingFirstVisibleOffset = DefaultDescendingFirstVisibleOffset;

    public static bool IsInitialized => _config?.bands != null && _config.bands.Length > 0;

    /// <summary>Incrémenté à chaque Initialize — LedFrameWriter invalide ses buffers.</summary>
    public static int LayoutVersion => _layoutVersion;

    public static int Columns => _config?.columns ?? 0;

    /// <summary>Lignes authoring visibles (128 plein mur, 32 viewport, …).</summary>
    public static int VisibleRows => _visibleRows;

    /// <summary>Décalage ligne physique du haut du viewport (0 = plein mur).</summary>
    public static int OriginRow => _originRow;

    public static int AscendingLastVisibleOffset => _ascendingLastVisibleOffset;

    public static int DescendingFirstVisibleOffset => _descendingFirstVisibleOffset;

    /// <summary>
    /// Convertit une distance en cellules authorée pour un mur 128
    /// vers la résolution courante (ex. 22 → ~5.5 sur 32×32).
    /// </summary>
    public static float ScaleCellsFrom128(float cellsAt128)
    {
        int cols = Columns > 0 ? Columns : DefaultVisibleRows;
        return cellsAt128 * (cols / (float)DefaultVisibleRows);
    }

    public static int EntityIdStart =>
        _config?.bands != null && _config.bands.Length > 0
            ? _config.bands[0].entityStart
            : 100;

    public static void Initialize(WallBandsConfig config)
    {
        _config = config;
        _cachedChunks = null;
        _layoutVersion++;

        _visibleRows = config != null && config.visibleRows > 0
            ? config.visibleRows
            : DefaultVisibleRows;
        _originRow = config != null ? System.Math.Max(0, config.originRow) : 0;
        _ascendingLastVisibleOffset = config != null && config.ascendingLastVisibleOffset > 0
            ? config.ascendingLastVisibleOffset
            : DefaultAscendingLastVisibleOffset;
        _descendingFirstVisibleOffset = config != null && config.descendingFirstVisibleOffset > 0
            ? config.descendingFirstVisibleOffset
            : DefaultDescendingFirstVisibleOffset;
    }

    /// <summary>
    /// Bande physique en U : offsets relatifs à la géométrie Glassworks (128 montant /
    /// 128 descendant). Le viewport authoring (row locale) est projeté via originRow.
    /// </summary>
    public static int? EntityIdForCell(int row, int column)
    {
        if (_config?.bands == null) return null;
        if (row < 0 || row >= _visibleRows || column < 0 || column >= _config.columns) return null;

        int physicalBandIndex = column / ColumnsPerPhysicalBand;
        int bandIndex = physicalBandIndex * ColumnsPerPhysicalBand;
        if (bandIndex >= _config.bands.Length) return null;

        var firstUniverse = _config.bands[bandIndex];
        int physicalRow = _originRow + row;

        if (column % ColumnsPerPhysicalBand == 0)
            return firstUniverse.entityStart + _ascendingLastVisibleOffset - physicalRow;

        return firstUniverse.entityStart + _descendingFirstVisibleOffset + physicalRow;
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
