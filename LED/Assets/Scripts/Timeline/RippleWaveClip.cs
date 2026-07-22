// Clip Timeline — ondes concentriques (ripple néon électrique).
// Des arcs lumineux partent d'une origine (bas/centre) et se propagent vers les bords,
// un nouvel arc réapparaissant au centre à chaque pulsation.

using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class RippleWaveClip : PlayableAsset
{
    [Header("Origine de l'onde (0..1 sur le mur)")]
    [Range(0f, 1f)]
    [Tooltip("Position horizontale de la source. 0 = gauche, 0.5 = centre, 1 = droite.")]
    public float originX = 0.5f;
    [Range(0f, 1f)]
    [Tooltip("Position verticale de la source. 0 = bas du mur, 1 = haut.")]
    public float originY = 0f;

    [Header("Propagation")]
    [Tooltip("Distance entre deux arcs successifs, en cellules LED. Plus petit = arcs plus rapprochés.")]
    public float wavelengthCells = 34f;
    [Tooltip("Vitesse de propagation, en nombre d'ondes émises par seconde.")]
    public float wavesPerSecond = 0.6f;
    [Range(0.5f, 8f)]
    [Tooltip("Épaisseur du CŒUR lumineux de l'arc, en cellules. Petit = tube néon très fin.")]
    public float lineWidthCells = 1.2f;

    [Header("Halo / lueur néon")]
    [Range(1f, 30f)]
    [Tooltip("Étendue du halo diffus autour du cœur, en cellules. Grand = lueur large.")]
    public float haloCells = 8f;
    [Range(0f, 1f)]
    [Tooltip("Intensité du halo (dégradé sombre). 0 = pas de halo, juste le tube fin.")]
    public float haloStrength = 0.35f;
    [Range(0f, 6f)]
    [Tooltip("Ondulation organique des bords des arcs (cellules). 0 = arcs parfaitement lisses ; " +
             "plus haut = aspect turbulent/atmosphérique.")]
    public float edgeNoiseCells = 1.5f;

    [Header("Atténuation")]
    [Range(0f, 1f)]
    [Tooltip("Fondu des arcs avec la distance à la source (perte d'intensité au coin le plus éloigné). " +
             "0 = intensité constante partout, 1 = disparition au coin.")]
    public float distanceFade = 0.2f;

    [Header("Rendu / émission")]
    [Tooltip("Multiplicateur d'intensité (HDR) pour le bloom néon. >1 = fort effet d'émission.")]
    public float glowIntensity = 3f;
    [Tooltip("Couleur néon. Bleu électrique par défaut.")]
    public Color tint = new Color(0.15f, 0.7f, 1f);

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<RippleWaveBehaviour>.Create(graph);
        var b = playable.GetBehaviour();
        b.originX = originX;
        b.originY = originY;
        b.wavelengthCells = Mathf.Max(2f, wavelengthCells);
        b.wavesPerSecond = wavesPerSecond;
        b.lineWidthCells = Mathf.Max(0.4f, lineWidthCells);
        b.haloCells = Mathf.Max(1f, haloCells);
        b.haloStrength = haloStrength;
        b.edgeNoiseCells = Mathf.Max(0f, edgeNoiseCells);
        b.distanceFade = distanceFade;
        b.glowIntensity = glowIntensity;
        b.tint = tint;
        return playable;
    }
}
