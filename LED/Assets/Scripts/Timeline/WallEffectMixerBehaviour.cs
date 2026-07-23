// Assets/Scripts/Timeline/WallEffectMixerBehaviour.cs
//
// Le chef d'orchestre de la piste d'effets. A chaque frame, Unity fournit tous
// les clips actifs avec leur poids (GetInputWeight). Contrairement au mixer
// EntityColor ou "le dernier gagne", ici on accumule les couleurs ponderees :
// quand deux clips se chevauchent dans la timeline, on obtient un vrai fondu
// enchaine, sans ecrire une ligne de code specifique a cette transition.
//
// Ecriture des couleurs : on passe par SetColorSilent pour ne pas declencher
// l'evenement OnColorChanged 16 384 fois par frame. Le StateExporter relit de
// toute facon l'etat complet a chaque envoi, et l'affichage 3D est mis a jour
// en une seule fois via ApplyDisplayPixels.

using UnityEngine;
using UnityEngine.Playables;

public class WallEffectMixerBehaviour : PlayableBehaviour
{
    private int _rows;
    private int _columns;
    private int?[,] _entityGrid;      // (row, col) -> id d'entite
    private Color[] _accumulator;     // couleur ponderee en cours de calcul
    private Color[] _displayPixels;   // tampon envoye au visualiseur
    private LedWallVisualizer _visualizer;
    private bool _warnedNoWall;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var entityManager = playerData as EntityManager;
        if (entityManager == null) return;

        // Garantit que le moteur audio-reactif tourne (auto-instancie).
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

        // 1. Remise a zero de l'accumulateur.
        for (int i = 0; i < _accumulator.Length; i++)
            _accumulator[i] = Color.black;

        // 2. Accumulation de chaque clip actif, pondere par son poids.
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
            float localTime = (float)inputPlayable.GetTime(); // temps depuis le debut du clip

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

        // 3. Ecriture vers les entites + le tampon d'affichage.
        for (int row = 0; row < _rows; row++)
        {
            int rowOffset = row * _columns;
            int textureRow = _rows - 1 - row; // la texture a son origine en haut

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

        if (_visualizer != null)
            _visualizer.ApplyDisplayPixels(_displayPixels);
    }

    private static byte ToByte(float value)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
    }

    /// <summary>
    /// Pre-calcule la grille (row, col) -> entityId une seule fois. C'est le
    /// meme principe que FluidWallAnimator.Initialize().
    /// </summary>
    private void EnsureBuffers()
    {
        int columns = WallMapping.Columns;
        int rows = WallMapping.VisibleRows;

        if (_entityGrid != null && _columns == columns && _rows == rows)
        {
            if (_visualizer == null)
                _visualizer = Object.FindAnyObjectByType<LedWallVisualizer>();
            return;
        }

        _columns = columns;
        _rows = rows;
        _entityGrid = new int?[rows, columns];

        for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
                _entityGrid[row, col] = WallMapping.EntityIdForCell(row, col);

        _accumulator = new Color[rows * columns];
        _displayPixels = new Color[rows * columns];
        _visualizer = Object.FindAnyObjectByType<LedWallVisualizer>();
    }
}
