// Assets/Scripts/Timeline/ParticleField.cs
//
// Nuage de particules qui dansent dans le chaos puis convergent pour dessiner
// un mot. C'est une alternative a la silhouette : au lieu d'un corps, une nuee
// de points lumineux qui se rassemble.
//
// Rendu : chaque particule est "splattee" une fois par trame dans un tampon
// d'intensite avec un petit noyau doux (anticrénelage), puis le mur echantillonne
// ce tampon. On ne parcourt donc jamais les particules par pixel : cout maitrise
// meme sur 16 384 LED.

using UnityEngine;

public class ParticleField
{
    private int _columns, _rows;
    private float[] _buffer;

    // Donnees par particule, pre-calculees une fois.
    private int _count;
    private float[] _targetX, _targetY;   // position finale, sur une lettre
    private float[] _homeX, _homeY;       // ancrage du chaos
    private float[] _orbitR, _orbitSpd, _orbitPhase;
    private float[] _delay;               // decalage d'assemblage par particule

    private float _lastTime = -1f;

    /// <summary>
    /// Prepare les particules pour un mot donne. Les cibles sont les pixels
    /// allumes du mot, agrandi et centre sur le mur.
    /// </summary>
    public void Build(int columns, int rows, bool[][] band, int scale, float centerYRatio, int seed)
    {
        _columns = columns;
        _rows = rows;
        _buffer = new float[columns * rows];

        // Cibles = pixels allumes de la banniere de texte.
        int bandW = band[0].Length;
        int textW = bandW * scale;
        int textH = PixelFont.GlyphHeight * scale;
        int originX = (columns - textW) / 2;
        int originY = Mathf.RoundToInt(rows * centerYRatio) - textH / 2;

        // On liste d'abord les cibles pour connaitre leur nombre.
        int targetCount = 0;
        for (int r = 0; r < PixelFont.GlyphHeight; r++)
            for (int c = 0; c < bandW; c++)
                if (band[r][c]) targetCount++;
        targetCount *= scale * scale;
        if (targetCount == 0) targetCount = 1;

        // ~1.4 particule par pixel cible : lettres bien remplies, sans exces.
        _count = Mathf.Clamp(Mathf.RoundToInt(targetCount * 1.4f), 200, 4000);

        _targetX = new float[_count]; _targetY = new float[_count];
        _homeX = new float[_count]; _homeY = new float[_count];
        _orbitR = new float[_count]; _orbitSpd = new float[_count]; _orbitPhase = new float[_count];
        _delay = new float[_count];

        var rng = new System.Random(seed);
        float Rand() => (float)rng.NextDouble();

        // Table plate des pixels cibles pour tirage aleatoire.
        var tx = new float[targetCount];
        var ty = new float[targetCount];
        int k = 0;
        for (int r = 0; r < PixelFont.GlyphHeight; r++)
        {
            for (int c = 0; c < bandW; c++)
            {
                if (!band[r][c]) continue;
                for (int sy = 0; sy < scale; sy++)
                for (int sx = 0; sx < scale; sx++)
                {
                    tx[k] = originX + c * scale + sx;
                    // La banniere est stockee haut->bas ; le mur a le bas en 0.
                    int rowFromBottom = originY + (PixelFont.GlyphHeight - 1 - r) * scale + sy;
                    ty[k] = rowFromBottom;
                    k++;
                }
            }
        }

        for (int i = 0; i < _count; i++)
        {
            int t = rng.Next(targetCount);
            _targetX[i] = tx[t];
            _targetY[i] = ty[t];

            // Chaos : ancrage n'importe ou sur le mur.
            _homeX[i] = Rand() * columns;
            _homeY[i] = Rand() * rows;

            _orbitR[i] = 4f + Rand() * 14f;
            _orbitSpd[i] = 1.5f + Rand() * 4f;
            _orbitPhase[i] = Rand() * 6.2832f;

            // Assemblage etale : toutes n'arrivent pas exactement ensemble.
            _delay[i] = Rand() * 0.35f;
        }
    }

    /// <summary>
    /// Calcule le tampon d'intensite pour l'instant donne, une seule fois par
    /// trame (les appels suivants au meme temps sont ignores).
    ///
    ///   phase 0.0 -> chaosEnd : les particules tourbillonnent (la danse)
    ///   convergence           : elles filent vers leur lettre
    ///   apres                 : le mot est forme, il scintille doucement
    /// </summary>
    public void Simulate(float time, float chaosEnd, float convergeDur)
    {
        if (Mathf.Abs(time - _lastTime) < 1e-4f) return;
        _lastTime = time;

        System.Array.Clear(_buffer, 0, _buffer.Length);

        for (int i = 0; i < _count; i++)
        {
            // Position "chaos" : orbite autour d'un ancrage qui derive lentement.
            float ang = _orbitPhase[i] + time * _orbitSpd[i];
            float driftX = _homeX[i] + Mathf.Sin(time * 0.6f + _orbitPhase[i]) * 10f;
            float driftY = _homeY[i] + Mathf.Cos(time * 0.5f + _orbitPhase[i]) * 8f;
            float chaosX = driftX + Mathf.Cos(ang) * _orbitR[i];
            float chaosY = driftY + Mathf.Sin(ang) * _orbitR[i];

            // Progression vers la cible, decalee et adoucie par particule.
            float local = (time - chaosEnd - _delay[i]) / Mathf.Max(0.05f, convergeDur);
            float toTarget = Mathf.Clamp01(local);
            toTarget = toTarget * toTarget * (3f - 2f * toTarget); // smoothstep

            float px = Mathf.Lerp(chaosX, _targetX[i], toTarget);
            float py = Mathf.Lerp(chaosY, _targetY[i], toTarget);

            // Une fois assemblee, la particule frissonne legerement.
            if (toTarget >= 1f)
            {
                px += Mathf.Sin(time * 7f + i) * 0.35f;
                py += Mathf.Cos(time * 6f + i * 1.3f) * 0.35f;
            }

            Splat(px, py, Mathf.Lerp(0.6f, 1f, toTarget));
        }
    }

    /// <summary>Depose une particule douce (noyau 2x2 + halo) dans le tampon.</summary>
    private void Splat(float x, float y, float intensity)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0;
        float fy = y - y0;

        // Repartition bilineaire sur les 4 LED voisines : deplacement fluide,
        // sans scintillement de pixel.
        Add(x0,     y0,     intensity * (1 - fx) * (1 - fy));
        Add(x0 + 1, y0,     intensity * fx * (1 - fy));
        Add(x0,     y0 + 1, intensity * (1 - fx) * fy);
        Add(x0 + 1, y0 + 1, intensity * fx * fy);

        // Petit halo pour donner de la matiere lumineuse.
        Add(x0,     y0 - 1, intensity * 0.18f);
        Add(x0,     y0 + 2, intensity * 0.18f);
        Add(x0 - 1, y0,     intensity * 0.18f);
        Add(x0 + 2, y0,     intensity * 0.18f);
    }

    private void Add(int x, int y, float v)
    {
        if (x < 0 || x >= _columns || y < 0 || y >= _rows) return;
        _buffer[y * _columns + x] += v;
    }

    /// <summary>Intensite lue au pixel (col, row), saturee a 1.</summary>
    public float Sample(int column, int row)
    {
        if (_buffer == null) return 0f;
        if (column < 0 || column >= _columns || row < 0 || row >= _rows) return 0f;
        return Mathf.Clamp01(_buffer[row * _columns + column]);
    }

    public bool Matches(int columns, int rows) => _buffer != null && _columns == columns && _rows == rows;
}
