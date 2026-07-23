// Assets/Scripts/Timeline/FireworkField.cs
//
// Feu d'artifice en trois temps, rendu dans un tampon couleur puis echantillonne
// par le mur (aucun parcours de particules par pixel : cout maitrise).
//
//   1. TIR       : une fusee monte du bas vers le haut, traînee de fumee derriere.
//   2. EXPLOSION : au sommet, elle projette des particules rouge / bleu / orange
//                  (drapeau armenien), avec vitesse radiale + gravite.
//   3. RASSEMBLEMENT : les particules ralentissent puis sont attirees vers des
//                  cibles qui dessinent HETIC (orientation native, ligne 0 = haut,
//                  donc les lettres sont a l'endroit).
//
// Repere : (x = colonne, y = ligne) avec y = 0 EN HAUT, comme l'affichage.

using UnityEngine;

public class FireworkField
{
    private int _cols, _rows;
    private Color[] _buf;
    private float _lastTime = -1f;

    // Etapes (secondes depuis le debut de la phase feu d'artifice).
    public float LaunchDur = 1.0f;   // duree de la montee de la fusee
    public float FreeDur = 1.1f;     // vol libre : la boule s'etale et retombe (feu d'artifice)
    public float GatherDur = 1.7f;   // rassemblement des particules vers le logo

    private float _apexX, _apexY;    // point d'explosion
    private const float Gravity = 34f;

    private int _n;
    private float[] _vx, _vy;        // vitesse d'explosion
    private float[] _tx, _ty;        // cible (lettre)
    private Color[] _col;            // couleur de la particule
    private float[] _phase;          // dephasage de scintillement

    public bool Matches(int c, int r) => _buf != null && _cols == c && _rows == r;

    public void Build(int cols, int rows, bool[][] band, int scale, float centerYRatio, Color[] palette, int seed)
    {
        _cols = cols; _rows = rows;
        _buf = new Color[cols * rows];

        _apexX = cols * 0.5f;
        _apexY = rows * 0.30f;       // explosion dans le tiers haut

        // Cibles : pixels allumes de HETIC, agrandis et centres. En orientation
        // native (br = 0 -> haut du glyphe -> haut du mur), donc a l'endroit.
        int bw = band[0].Length;
        int textW = bw * scale;
        int textH = PixelFont.GlyphHeight * scale;
        int ox = (cols - textW) / 2;
        int oy = Mathf.RoundToInt(rows * centerYRatio) - textH / 2;

        int targetCount = 0;
        for (int r = 0; r < PixelFont.GlyphHeight; r++)
            for (int c = 0; c < bw; c++)
                if (band[r][c]) targetCount++;
        targetCount *= scale * scale;
        if (targetCount == 0) targetCount = 1;

        var txAll = new float[targetCount];
        var tyAll = new float[targetCount];
        int k = 0;
        for (int r = 0; r < PixelFont.GlyphHeight; r++)
            for (int c = 0; c < bw; c++)
            {
                if (!band[r][c]) continue;
                for (int sy = 0; sy < scale; sy++)
                for (int sx = 0; sx < scale; sx++)
                {
                    txAll[k] = ox + c * scale + sx;
                    tyAll[k] = oy + r * scale + sy;   // r=0 -> haut : lettres a l'endroit
                    k++;
                }
            }

        _n = Mathf.Clamp(Mathf.RoundToInt(targetCount * 2.0f), 300, 5000);
        _vx = new float[_n]; _vy = new float[_n];
        _tx = new float[_n]; _ty = new float[_n];
        _col = new Color[_n]; _phase = new float[_n];

        var rng = new System.Random(seed);
        float R() => (float)rng.NextDouble();

        for (int i = 0; i < _n; i++)
        {
            int t = rng.Next(targetCount);
            _tx[i] = txAll[t];
            _ty[i] = tyAll[t];

            // Explosion radiale : angle uniforme, vitesse variable.
            float ang = R() * 6.2832f;
            float spd = 42f + R() * 72f;
            _vx[i] = Mathf.Cos(ang) * spd;
            _vy[i] = Mathf.Sin(ang) * spd - 12f; // petit biais vers le haut

            _col[i] = palette[rng.Next(palette.Length)];
            _phase[i] = R() * 6.2832f;
        }
    }

    /// <summary>
    /// Variante : les cibles viennent d'une grille 128x128 (logo), pas d'un mot.
    /// grid[row][col], row 0 en haut => logo a l'endroit.
    /// </summary>
    public void BuildFromGrid(int cols, int rows, bool[][] grid, Color[] palette, int seed)
    {
        _cols = cols; _rows = rows;
        _buf = new Color[cols * rows];
        _apexX = cols * 0.5f;
        _apexY = rows * 0.30f;

        // Collecte des cibles = cellules allumees de la grille.
        int gh = grid.Length;
        int gw = grid.Length > 0 ? grid[0].Length : 0;
        // Recentre/echelle la grille sur le mur si tailles differentes.
        float sx = cols / (float)gw;
        float sy = rows / (float)gh;

        var txList = new System.Collections.Generic.List<float>();
        var tyList = new System.Collections.Generic.List<float>();
        for (int y = 0; y < gh; y++)
            for (int x = 0; x < gw; x++)
                if (grid[y][x])
                {
                    txList.Add(x * sx);
                    tyList.Add(y * sy);
                }

        int targetCount = Mathf.Max(1, txList.Count);

        _n = Mathf.Clamp(Mathf.RoundToInt(targetCount * 1.6f), 300, 6000);
        _vx = new float[_n]; _vy = new float[_n];
        _tx = new float[_n]; _ty = new float[_n];
        _col = new Color[_n]; _phase = new float[_n];

        var rng = new System.Random(seed);
        float R() => (float)rng.NextDouble();

        for (int i = 0; i < _n; i++)
        {
            int t = rng.Next(targetCount);
            _tx[i] = txList[t];
            _ty[i] = tyList[t];

            float ang = R() * 6.2832f;
            float spd = 42f + R() * 72f;
            _vx[i] = Mathf.Cos(ang) * spd;
            _vy[i] = Mathf.Sin(ang) * spd - 12f;

            _col[i] = palette[rng.Next(palette.Length)];
            _phase[i] = R() * 6.2832f;
        }
    }

    public void Simulate(float t, float beat, float high)
    {
        if (Mathf.Abs(t - _lastTime) < 1e-4f) return;
        _lastTime = t;

        // Trainees : on ESTOMPE au lieu d'effacer -> queues lumineuses, rendu feu d'artifice.
        const float trail = 0.55f;
        for (int i = 0; i < _buf.Length; i++)
        {
            _buf[i].r *= trail; _buf[i].g *= trail; _buf[i].b *= trail;
        }

        // --- 1. TIR : la fusee monte, traînee de fumee derriere ---
        if (t < LaunchDur)
        {
            float u = t / LaunchDur;
            float ease = 1f - (1f - u) * (1f - u);
            float ry = Mathf.Lerp(_rows - 1, _apexY, ease);
            for (int sIdx = 0; sIdx < 26; sIdx++)
            {
                float sy = ry + sIdx * 1.6f;
                if (sy >= _rows) break;
                float fd = (1f - sIdx / 26f);
                float jit = Mathf.Sin(t * 20f + sIdx) * (0.6f + sIdx * 0.12f);
                Splat(_apexX + jit, sy, new Color(0.5f, 0.48f, 0.5f) * (fd * fd * 0.5f));
            }
            Splat(_apexX, ry, new Color(1f, 0.95f, 0.7f) * 1.4f);
            return;
        }

        // --- 2. Explosion : la boule s'etale et retombe, puis rassemblement en logo ---
        float te = t - LaunchDur;

        for (int i = 0; i < _n; i++)
        {
            float px, py, settle = 1f, formGlow = 0f;

            float freeX = _apexX + _vx[i] * te;
            float freeY = _apexY + _vy[i] * te + 0.5f * Gravity * te * te;

            if (te < FreeDur)
            {
                px = freeX; py = freeY;
            }
            else
            {
                float endX = _apexX + _vx[i] * FreeDur;
                float endY = _apexY + _vy[i] * FreeDur + 0.5f * Gravity * FreeDur * FreeDur;

                float raw = Mathf.Clamp01((te - FreeDur) / GatherDur);
                float g = raw * raw * raw * (raw * (raw * 6f - 15f) + 10f); // smootherstep
                px = Mathf.Lerp(endX, _tx[i], g);
                py = Mathf.Lerp(endY, _ty[i], g);
                settle = 1f - g;

                formGlow = Mathf.SmoothStep(0.7f, 1f, g);
                if (raw >= 1f)
                {
                    float sinceForm = te - (FreeDur + GatherDur);
                    formGlow = Mathf.Lerp(1f, 0.18f, Mathf.Clamp01(sinceForm * 1.3f));
                }
            }

            float jx = Mathf.Sin(te * 18f + _phase[i]) * settle * 0.7f;
            float jy = Mathf.Cos(te * 15f + _phase[i] * 1.3f) * settle * 0.7f;
            float spark = 0.82f + high * 0.4f + beat * 0.2f
                          + 0.18f * Mathf.Sin(te * 22f + _phase[i]) * (0.5f + settle);
            Color c = _col[i] * spark + Color.white * (formGlow * 0.55f);
            Splat(px + jx, py + jy, c);
        }
    }

    private void Splat(float x, float y, Color c)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;

        Add(x0,     y0,     c * ((1 - fx) * (1 - fy)));
        Add(x0 + 1, y0,     c * (fx * (1 - fy)));
        Add(x0,     y0 + 1, c * ((1 - fx) * fy));
        Add(x0 + 1, y0 + 1, c * (fx * fy));
        // petit halo
        Add(x0, y0 - 1, c * 0.15f);
        Add(x0, y0 + 2, c * 0.15f);
        Add(x0 - 1, y0, c * 0.15f);
        Add(x0 + 2, y0, c * 0.15f);
    }

    private void Add(int x, int y, Color c)
    {
        if (x < 0 || x >= _cols || y < 0 || y >= _rows) return;
        int i = y * _cols + x;
        _buf[i].r += c.r; _buf[i].g += c.g; _buf[i].b += c.b;
    }

    public Color Sample(int column, int row)
    {
        if (_buf == null || column < 0 || column >= _cols || row < 0 || row >= _rows)
            return Color.black;
        Color c = _buf[row * _cols + column];
        return new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b));
    }
}
