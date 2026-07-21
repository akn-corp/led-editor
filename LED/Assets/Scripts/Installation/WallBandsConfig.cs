// Structures pour désérialiser wall-bands.json (mur Glassworks 128×128).

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
    public WallBandData[] bands;
}
