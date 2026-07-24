// Assets/Scripts/Timeline/WallEffectMixerBehaviour.cs
//
// Mixer de la piste Wall Effect. Binding : LedWallVisualizer.
// Accumule les clips actifs (fondu) puis ecrit via EntityManager + ApplyDisplayPixels.

using UnityEngine;
using UnityEngine.Playables;

public class WallEffectMixerBehaviour : PlayableBehaviour
{
    private int _rows;
    private int _columns;
    private int?[,] _entityGrid;
    private Color[] _accumulator;
    private Color[] _displayPixels;
    private LedWallVisualizer _visualizer;
    private bool _warnedNoWall;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var wall = playerData as LedWallVisualizer;
        if (wall == null || !wall.IsBuilt || wall.EntityManager == null)
        {
            if (!_warnedNoWall && playerData != null)
            {
                _warnedNoWall = true;
                Debug.LogWarning("[WallEffectMixer] Binding LedWall manquant ou mur non construit.");
            }
            return;
        }

        var entityManager = wall.EntityManager;
        _visualizer = wall;

        AudioReactive.GetOrCreate();

        if (!WallMapping.IsInitialized)
        {
            if (!_warnedNoWall)
            {
                _warnedNoWall = true;
                Debug.LogWarning("[WallEffectMixer] Mur non initialise — l'installation n'est pas encore chargee.");
            }
            return;
        }

        EnsureBuffers();

        for (int i = 0; i < _accumulator.Length; i++)
            _accumulator[i] = Color.black;

        int inputCount = playable.GetInputCount();
        bool anyActive = false;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var inputPlayable = (ScriptPlayable<WallEffectBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();
            if (behaviour == null) continue;

            anyActive = true;
            float localTime = (float)inputPlayable.GetTime();

            for (int row = 0; row < _rows; row++)
            {
                int rowOffset = row * _columns;
                for (int col = 0; col < _columns; col++)
                {
                    Color c = behaviour.Evaluate(col, row, _columns, _rows, localTime);
                    _accumulator[rowOffset + col] += c * weight;
                }
            }
        }

        if (!anyActive) return;

        for (int row = 0; row < _rows; row++)
        {
            int rowOffset = row * _columns;
            int textureRow = _rows - 1 - row;

            for (int col = 0; col < _columns; col++)
            {
                Color c = _accumulator[rowOffset + col];
                _displayPixels[textureRow * _columns + col] = c;

                int? entityId = _entityGrid[row, col];
                if (!entityId.HasValue) continue;

                entityManager.SetColorSilent(
                    entityId.Value,
                    ToByte(c.r),
                    ToByte(c.g),
                    ToByte(c.b));
            }
        }

        _visualizer.ApplyDisplayPixels(_displayPixels);
    }

    private static byte ToByte(float value)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
    }

    private void EnsureBuffers()
    {
        int columns = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;

        if (_entityGrid != null && _columns == columns && _rows == rows)
            return;

        _columns = columns;
        _rows = rows;
        _entityGrid = new int?[rows, columns];

        for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
                _entityGrid[row, col] = WallMapping.EntityIdForCell(row, col);

        _accumulator = new Color[rows * columns];
        _displayPixels = new Color[rows * columns];
    }
}
