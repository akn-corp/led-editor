// Structures pour désérialiser wall-bands.json (mur Glassworks / viewport).

using System;

[Serializable]
public class WallBandData
{
    public int column;
    public int entityStart;
    public int entityCount;
}

[Serializable]
public class WallBandsConfig
{
    public int columns;
    public string generatedFrom;
    public string profile;

    /// <summary>Lignes authoring (0 = défaut 128 côté WallMapping).</summary>
    public int visibleRows;

    /// <summary>Ligne physique du haut du viewport (carré centré → 48 pour 32×32).</summary>
    public int originRow;

    public int physicalVisibleRows;
    public int ascendingLastVisibleOffset;
    public int descendingFirstVisibleOffset;

    public WallBandData[] bands;
}
