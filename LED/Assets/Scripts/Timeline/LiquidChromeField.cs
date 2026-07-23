// Assets/Scripts/Timeline/LiquidChromeField.cs
//
// Mercure liquide / chrome. Plusieurs "gouttes" (metaballs) se deplacent et
// fusionnent ; on en tire un champ scalaire, dont la surface forme le metal.
// L'aspect reflechissant vient d'un faux environnement : on calcule la normale
// de la surface et on la projette dans une rampe chrome (sombre -> reflet blanc
// -> sombre). C'est ce qui donne les crêtes brillantes typiques du chrome.
//
// Le champ est calcule une fois par trame dans un tampon ; le mur echantillonne
// ensuite avec une difference finie pour la normale. Cout maitrise sur 16 384 LED.

using UnityEngine;

public class LiquidChromeField
{
    private int _columns, _rows;
    private float[] _field;
    private float _lastTime = -1f;

    private const int BlobCount = 9;
    private readonly float[] _bx = new float[BlobCount];
    private readonly float[] _by = new float[BlobCount];
    private readonly float[] _br = new float[BlobCount];
    private readonly float[] _p1 = new float[BlobCount];
    private readonly float[] _p2 = new float[BlobCount];
    private readonly float[] _s1 = new float[BlobCount];
    private readonly float[] _s2 = new float[BlobCount];

    public bool Matches(int columns, int rows) => _field != null && _columns == columns && _rows == rows;

    public void Build(int columns, int rows, int seed)
    {
        _columns = columns;
        _rows = rows;
        _field = new float[columns * rows];

        var rng = new System.Random(seed);
        float R() => (float)rng.NextDouble();

        for (int i = 0; i < BlobCount; i++)
        {
            _bx[i] = R() * columns;
            _by[i] = R() * rows;
            _br[i] = (0.10f + R() * 0.14f) * columns; // rayon d'influence
            _p1[i] = R() * 6.2832f;
            _p2[i] = R() * 6.2832f;
            _s1[i] = 0.25f + R() * 0.5f;
            _s2[i] = 0.20f + R() * 0.4f;
        }
    }

    /// <summary>Deplace les gouttes et recalcule le champ, une fois par trame.</summary>
    public void Simulate(float time)
    {
        if (Mathf.Abs(time - _lastTime) < 1e-4f) return;
        _lastTime = time;

        // Positions courantes des gouttes : lissajous lents, mouvement liquide.
        float mx = _columns * 0.5f, my = _rows * 0.5f;
        float ax = _columns * 0.42f, ay = _rows * 0.42f;
        for (int i = 0; i < BlobCount; i++)
        {
            _bx[i] = mx + Mathf.Sin(time * _s1[i] + _p1[i]) * ax;
            _by[i] = my + Mathf.Cos(time * _s2[i] + _p2[i]) * ay;
        }

        for (int y = 0; y < _rows; y++)
        {
            int rowOff = y * _columns;
            for (int x = 0; x < _columns; x++)
            {
                float sum = 0f;
                for (int i = 0; i < BlobCount; i++)
                {
                    float dx = x - _bx[i];
                    float dy = y - _by[i];
                    float d2 = dx * dx + dy * dy + 1f;
                    float r2 = _br[i] * _br[i];
                    sum += r2 / d2;
                }
                _field[rowOff + x] = sum;
            }
        }
    }

    private float F(int x, int y)
    {
        if (x < 0) x = 0; else if (x >= _columns) x = _columns - 1;
        if (y < 0) y = 0; else if (y >= _rows) y = _rows - 1;
        return _field[y * _columns + x];
    }

    /// <summary>
    /// Couleur chrome au pixel (col, row). surface = seuil du champ ; en dessous
    /// c'est le vide (noir). La rampe transforme la normale en reflet metallique.
    /// </summary>
    public Color Shade(int column, int row, float surface, Color tint)
    {
        if (_field == null) return Color.black;

        float v = F(column, row);
        float lo = surface * 0.72f;
        if (v < lo) return Color.black; // hors du metal

        // Normale approximee par difference finie du champ.
        float gx = F(column + 1, row) - F(column - 1, row);
        float gy = F(column, row + 1) - F(column, row - 1);

        // "Coordonnee de reflet" : combine la hauteur du champ et l'inclinaison
        // locale. Meme au centre d'une grosse goutte (ou le gradient est faible),
        // la hauteur varie — donc les bandes brillantes traversent TOUTE la
        // surface, pas seulement les bords. C'est ce qui fait le mercure poli.
        float refl = v * 0.35f + gy * 6f + gx * 2f + _lastTime * 8f;

        // Environnement raye : plusieurs bandes claires qui defilent.
        float chrome = StripedEnv(refl);

        // Liseré de tension de surface : bord très lumineux.
        float edge = Mathf.SmoothStep(lo, surface, v);
        float rim = Mathf.Pow(1f - edge, 2f);

        float lum = Mathf.Clamp01(chrome * 0.9f + rim * 0.8f);

        // Acier teinte + reflet blanc pur sur les crêtes.
        Color metal = tint * (0.12f + 0.5f * lum);
        Color spec = Color.white * Mathf.Pow(lum, 4f);
        return metal + spec;
    }

    /// <summary>Environnement metallique raye : bandes claires periodiques nettes.</summary>
    private static float StripedEnv(float t)
    {
        // Bandes principales serrees + une modulation lente pour la variete.
        float bands = 0.5f + 0.5f * Mathf.Sin(t * 0.45f);
        bands = Mathf.Pow(bands, 2.2f);                 // creuse les noirs
        float sheen = 0.5f + 0.5f * Mathf.Sin(t * 0.13f + 1.3f);
        return Mathf.Clamp01(bands * 0.85f + sheen * 0.25f);
    }

        /// <summary>Environnement chrome : noir, montee, reflet blanc, redescente.</summary>
    private static float ChromeRamp(float t)
    {
        // Deux pics brillants pour imiter un studio a deux sources.
        float a = Mathf.Exp(-Mathf.Pow((t - 0.28f) / 0.10f, 2f));
        float b = Mathf.Exp(-Mathf.Pow((t - 0.72f) / 0.14f, 2f));
        float baseSteel = 0.18f + 0.20f * t;
        return baseSteel + a * 0.9f + b * 0.7f;
    }
}
