// Assets/Scripts/Timeline/AudioReactive.cs
//
// Moteur audio-reactif : ecoute la sortie master en temps reel et en extrait
// les grandeurs qui pilotent la lumiere dans un vrai show :
//
//   Level  : volume global (0..1)          -> luminosite d'ensemble
//   Bass   : energie des graves            -> taille/force des mouvements
//   Mid    : energie des mediums
//   High   : energie des aigus             -> scintillement, details
//   Beat   : enveloppe de kick (1 -> 0)    -> flashs, coupures, impacts
//
// Detection des coups : flux spectral (somme des hausses d'energie d'une trame
// a l'autre dans le bas du spectre) compare a un seuil adaptatif. C'est la
// methode des logiciels VJ pro ; elle cale les flashs sous les 100 ms.
//
// Le composant s'auto-instancie : la timeline le cree au besoin, aucune mise en
// place manuelle dans la scene. Il lit AudioListener (le mix final), donc il
// fonctionne quelle que soit la source qui joue — y compris une piste Timeline.

using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AudioReactive : MonoBehaviour
{
    public static AudioReactive Instance { get; private set; }

    /// <summary>Recupere l'instance, la cree si elle n'existe pas encore.</summary>
    public static AudioReactive GetOrCreate()
    {
        if (Instance != null) return Instance;
        Instance = FindAnyObjectByType<AudioReactive>();
        if (Instance == null)
        {
            var go = new GameObject("~AudioReactive");
            Instance = go.AddComponent<AudioReactive>();
        }
        return Instance;
    }

    private const int Samples = 1024;
    private readonly float[] _spectrum = new float[Samples];
    private readonly float[] _prevSpectrum = new float[Samples];
    private readonly float[] _wave = new float[512];

    // Historique de flux pour le seuil adaptatif (~1s a 60 fps).
    private readonly float[] _fluxHistory = new float[43];
    private int _fluxIndex;

    // Auto-gain par bande : normalise quel que soit le volume du morceau.
    private float _maxBass = 0.01f, _maxMid = 0.01f, _maxHigh = 0.01f, _maxLevel = 0.01f;

    public float Level { get; private set; }
    public float Bass { get; private set; }
    public float Mid { get; private set; }
    public float High { get; private set; }
    public float Beat { get; private set; }
    public float Flux { get; private set; }
    public int BeatCount { get; private set; }   // incremente a chaque kick detecte

    // Temps ecoule depuis le dernier kick detecte (pour eviter les doublons).
    private float _timeSinceBeat = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        float dt = Mathf.Max(1e-4f, Time.deltaTime);

        // --- Volume global (RMS de la forme d'onde) ---
        AudioListener.GetOutputData(_wave, 0);
        float sum = 0f;
        for (int i = 0; i < _wave.Length; i++) sum += _wave[i] * _wave[i];
        float rms = Mathf.Sqrt(sum / _wave.Length);
        _maxLevel = Mathf.Max(_maxLevel * 0.999f, rms);
        Level = Mathf.Clamp01(rms / _maxLevel);

        // --- Spectre ---
        System.Array.Copy(_spectrum, _prevSpectrum, Samples);
        AudioListener.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);

        // Bandes (48 kHz, ~23.4 Hz par bin).
        float bass = Band(1, 7);       //   ~30 - 160 Hz
        float lowMid = Band(7, 21);    //  160 - 500 Hz
        float mid = Band(21, 85);      //  500 - 2000 Hz
        float high = Band(85, 341);    // 2000 - 8000 Hz

        _maxBass = Mathf.Max(_maxBass * 0.999f, bass);
        _maxMid = Mathf.Max(_maxMid * 0.999f, mid);
        _maxHigh = Mathf.Max(_maxHigh * 0.999f, high);

        // Lissage : montee rapide, descente douce (aspect "vivant").
        Bass = Smooth(Bass, Mathf.Clamp01(bass / _maxBass), 0.55f, 0.12f);
        Mid = Smooth(Mid, Mathf.Clamp01(mid / _maxMid), 0.5f, 0.12f);
        High = Smooth(High, Mathf.Clamp01(high / _maxHigh), 0.7f, 0.2f);

        // --- Flux spectral sur le bas du spectre (kick + basse) ---
        float flux = 0f;
        for (int i = 1; i < 22; i++)
        {
            float d = _spectrum[i] - _prevSpectrum[i];
            if (d > 0f) flux += d;
        }
        Flux = flux;

        // Seuil adaptatif : moyenne recente x sensibilite.
        _fluxHistory[_fluxIndex] = flux;
        _fluxIndex = (_fluxIndex + 1) % _fluxHistory.Length;
        float mean = 0f;
        for (int i = 0; i < _fluxHistory.Length; i++) mean += _fluxHistory[i];
        mean /= _fluxHistory.Length;
        float threshold = mean * 1.6f + 1e-5f;

        _timeSinceBeat += dt;
        bool onset = flux > threshold && _timeSinceBeat > 0.12f && bass / _maxBass > 0.25f;
        if (onset)
        {
            Beat = 1f;
            _timeSinceBeat = 0f;
            BeatCount++;
        }
        else
        {
            // Decroissance du flash de kick, avec un plancher suivant la basse.
            Beat = Mathf.Max(Beat - dt * 4.5f, Bass * 0.35f);
        }
    }

    private float Band(int from, int to)
    {
        float s = 0f;
        for (int i = from; i < to && i < Samples; i++) s += _spectrum[i];
        return s;
    }

    private static float Smooth(float current, float target, float up, float down)
    {
        float k = target > current ? up : down;
        return Mathf.Lerp(current, target, k);
    }
}
