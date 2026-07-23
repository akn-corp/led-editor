// Assets/Scripts/Timeline/SonicRingField.cs
//
// Effet hero synchronise a la musique. A chaque coup de kick detecte par
// AudioReactive, une onde de choc part du centre et se propage vers les bords.
// La basse fait respirer un noyau lumineux central, et les gros coups declenchent
// un flash plein cadre avec un leger decalage RVB (aberration chromatique) —
// le vocabulaire visuel des shows de concert.
//
// Tout est cale sur l'audio reel : rien ne bouge si la musique se tait.

using UnityEngine;

public class SonicRingField
{
    private const int MaxRings = 16;
    private readonly float[] _ringAge = new float[MaxRings];
    private readonly float[] _ringStrength = new float[MaxRings];
    private int _ringHead;

    private float _lastTime = -1f;
    private float _prevBeat;

    // Grandeurs audio de la trame courante.
    private float _bass, _high, _beat, _level;

    public void Simulate(float time, AudioReactive audio)
    {
        if (Mathf.Abs(time - _lastTime) < 1e-4f) return;
        float dt = _lastTime < 0f ? 0.016f : Mathf.Clamp(time - _lastTime, 0f, 0.1f);
        _lastTime = time;

        if (audio != null)
        {
            _bass = audio.Bass;
            _high = audio.High;
            _beat = audio.Beat;
            _level = audio.Level;

            // Front montant du kick -> nouvelle onde de choc.
            if (audio.Beat > 0.9f && _prevBeat < 0.9f)
            {
                _ringHead = (_ringHead + 1) % MaxRings;
                _ringAge[_ringHead] = 0f;
                _ringStrength[_ringHead] = 0.6f + 0.4f * audio.Level;
            }
            _prevBeat = audio.Beat;
        }

        for (int i = 0; i < MaxRings; i++)
            if (_ringStrength[i] > 0f) _ringAge[i] += dt;
    }

    /// <summary>
    /// Couleur du pixel. cx/cy = centre du mur ; ringSpeed en LED/s.
    /// </summary>
    public Color Shade(int column, int row, int columns, int rows,
                       float ringSpeed, Color coreColor, Color accent)
    {
        float cx = columns * 0.5f;
        float cy = rows * 0.5f;
        float dx = column - cx;
        float dy = row - cy;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        // --- Ondes de choc ---
        float rings = 0f;
        for (int i = 0; i < MaxRings; i++)
        {
            float strength = _ringStrength[i];
            if (strength <= 0f) continue;

            float radius = _ringAge[i] * ringSpeed;
            float life = 1f - _ringAge[i] / 1.4f;      // duree de vie ~1.4 s
            if (life <= 0f) continue;

            float band = Mathf.Abs(dist - radius);
            float ring = Mathf.Exp(-(band * band) / 10f);  // anneau fin et net
            rings += ring * life * life * strength;
        }
        rings = Mathf.Clamp01(rings);

        // --- Noyau central qui pulse avec la basse ---
        float coreR = 6f + _bass * 26f;
        float core = Mathf.Exp(-(dist * dist) / (coreR * coreR)) * _bass;

        // --- Scintillement d'aigus, sur les bords ---
        float sparkle = 0f;
        if (_high > 0.2f)
        {
            float n = Hash(column * 131 + row * 17 + Mathf.FloorToInt(_lastTime * 30f) * 977);
            if (n > 1f - _high * 0.10f) sparkle = _high;
        }

        float lum = Mathf.Clamp01(rings + core * 0.9f + sparkle);

        // Base froide + reflet blanc sur les crêtes.
        Color c = coreColor * lum + Color.white * Mathf.Pow(lum, 3f);

        // Flash plein cadre sur les gros coups, teinte accent.
        c += accent * (_beat * _beat * 0.6f);

        return c;
    }

    /// <summary>Decalage RVB (aberration chromatique) proportionnel au kick.</summary>
    public float ChromaticOffset() => _beat * 3.5f;

    private static float Hash(int v)
    {
        unchecked
        {
            v = (v ^ 61) ^ (v >> 16); v += v << 3; v ^= v >> 4;
            v *= 0x27d4eb2d; v ^= v >> 15;
            return (v & 0x7fffffff) / (float)0x7fffffff;
        }
    }
}
