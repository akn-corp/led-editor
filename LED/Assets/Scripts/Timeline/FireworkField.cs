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
    public float LaunchDur = 1.0f;   // duree de la montee
    public float FreeDur = 0.9f;     // vol libre apres explosion
    public float GatherDur = 1.7f;   // rassemblement vers les lettres

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

        _n = Mathf.Clamp(Mathf.RoundToInt(targetCount * 1.5f), 250, 4000);
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
            float spd = 32f + R() * 60f;
            _vx[i] = Mathf.Cos(ang) * spd;
            _vy[i] = Mathf.Sin(ang) * spd - 12f; // petit biais vers le haut

            _col[i] = palette[rng.Next(palette.Length)];
            _phase[i] = R() * 6.2832f;
        }
    }

    public void Simulate(float t, float beat, float high)
    {
        if (Mathf.Abs(t - _lastTime) < 1e-4f) return;
        _lastTime = t;
        System.Array.Clear(_buf, 0, _buf.Length);

        // --- 1. TIR : la fusee monte, traînee de fumee derriere ---
        if (t < LaunchDur)
        {
            float u = t / LaunchDur;
            float ease = 1f - (1f - u) * (1f - u);          // easeOut
            float ry = Mathf.Lerp(_rows - 1, _apexY, ease); // du bas vers le sommet

            // Traînee/fumee : sous la fusee, en s'estompant.
            for (int s = 0; s < 26; s++)
            {
                float sy = ry + s * 1.6f;
                if (sy >= _rows) break;
                float fade = (1f - s / 26f);
                float jitter = Mathf.Sin(t * 20f + s) * (0.6f + s * 0.12f);
                var smoke = new Color(0.5f, 0.48f, 0.5f) * (fade * fade * 0.5f);
                Splat(_apexX + jitter, sy, smoke);
            }

            // Tete de fusee : point vif jaune-blanc + etincelles.
            Splat(_apexX, ry, new Color(1f, 0.95f, 0.7f) * 1.4f);
            Splat(_apexX + 0.6f, ry - 0.4f, Color.white * 0.6f);
            return;
        }

        // --- 2 & 3. Explosion puis rassemblement ---
        float te = t - LaunchDur;

        for (int i = 0; i < _n; i++)
        {
            float px, py;

            // Position de vol libre : balistique avec gravite.
            float freeX = _apexX + _vx[i] * te;
            float freeY = _apexY + _vy[i] * te + 0.5f * Gravity * te * te;

            if (te < FreeDur)
            {
                px = freeX; py = freeY;
            }
            else
            {
                // Gel de la position a la fin du vol libre, puis attraction.
                float endX = _apexX + _vx[i] * FreeDur + 0f;
                float endY = _apexY + _vy[i] * FreeDur + 0.5f * Gravity * FreeDur * FreeDur;

                float g = Mathf.Clamp01((te - FreeDur) / GatherDur);
                g = g * g * (3f - 2f * g); // smoothstep : ralentit puis se pose

                px = Mathf.Lerp(endX, _tx[i], g);
                py = Mathf.Lerp(endY, _ty[i], g);
            }

            // Scintillement : sur les aigus + oscillation propre.
            float spark = 0.75f + 0.25f * Mathf.Sin(te * 22f + _phase[i]) + high * 0.4f + beat * 0.2f;
            Splat(px, py, _col[i] * spark);
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
