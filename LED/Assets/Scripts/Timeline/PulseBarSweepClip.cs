// Clip Timeline — chorégraphie ligne fine + barre pleine pulsante.
//   1. La ligne fine descend du haut jusqu'à la barre (impact).
//   2. À l'impact, la barre s'allume (Or) et pulse.
//   3. La barre s'éteint, la ligne remonte vers le haut.
//   4. La barre pulsante réapparaît.
//   5. À la fin, la barre passe de l'Or au Cyan.

using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class PulseBarSweepClip : PlayableAsset
{
    [Header("Phases (fractions de la durée du clip, normalisées ensemble)")]
    [Tooltip("1 — La ligne fine descend du haut jusqu'à la barre.")]
    public float phaseDescendWeight = 1.2f;
    [Tooltip("2 — Impact : la barre s'allume (Or) et pulse.")]
    public float phasePulseWeight = 1.5f;
    [Tooltip("3 — La barre s'éteint, la ligne remonte.")]
    public float phaseRiseWeight = 1.2f;
    [Tooltip("4 — La barre pulsante réapparaît.")]
    public float phaseRelightWeight = 1f;
    [Tooltip("5 — Inversion de couleur : Or -> Cyan.")]
    public float phaseInvertWeight = 1f;

    [Header("Barre pleine (basse)")]
    [Tooltip("Épaisseur de la barre, en cellules LED.")]
    public int barHeightCells = 4;
    [Tooltip("Décalage de la barre au-dessus du bas du mur, en cellules.")]
    public int barBottomMarginCells = 22;
    [Tooltip("Étendue du halo au-dessus/en-dessous de la barre, en cellules.")]
    public float glowCells = 3f;

    [Header("Balayage (point lumineux sur la barre)")]
    public float sweepSpeed = 0.5f;
    [Range(0f, 2f)] public float sweepStrength = 0.7f;
    [Range(0.03f, 0.4f)] public float sweepWidth = 0.12f;

    [Header("Pulsation (tout au long, quand la barre est allumée)")]
    public float pulseSpeed = 2f;
    [Range(0f, 1f)] public float pulseDepth = 0.6f;

    [Header("Ligne fine mobile")]
    [Tooltip("Épaisseur de la ligne fine, en cellules.")]
    public float lineWidthCells = 1f;
    [Tooltip("Marge depuis le HAUT du mur pour le départ de la ligne, en cellules.")]
    public int lineTopMarginCells = 6;

    [Header("Couleurs / émission")]
    public Color goldColor = new Color(1f, 0.72f, 0.12f);
    public Color cyanColor = new Color(0.15f, 0.85f, 1f);
    [Tooltip("Couleur de la ligne fine.")]
    public Color lineColor = new Color(1f, 0.82f, 0.35f);
    public float glowIntensity = 3.2f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<PulseBarSweepBehaviour>.Create(graph);
        var b = playable.GetBehaviour();
        b.phaseDescendWeight = Mathf.Max(0.01f, phaseDescendWeight);
        b.phasePulseWeight = Mathf.Max(0.01f, phasePulseWeight);
        b.phaseRiseWeight = Mathf.Max(0.01f, phaseRiseWeight);
        b.phaseRelightWeight = Mathf.Max(0.01f, phaseRelightWeight);
        b.phaseInvertWeight = Mathf.Max(0.01f, phaseInvertWeight);
        b.barHeightCells = Mathf.Max(1, barHeightCells);
        b.barBottomMarginCells = Mathf.Max(0, barBottomMarginCells);
        b.glowCells = Mathf.Max(0f, glowCells);
        b.sweepSpeed = sweepSpeed;
        b.sweepStrength = Mathf.Max(0f, sweepStrength);
        b.sweepWidth = Mathf.Clamp(sweepWidth, 0.03f, 0.4f);
        b.pulseSpeed = pulseSpeed;
        b.pulseDepth = Mathf.Clamp01(pulseDepth);
        b.lineWidthCells = Mathf.Max(0.4f, lineWidthCells);
        b.lineTopMarginCells = Mathf.Max(0, lineTopMarginCells);
        b.goldColor = goldColor;
        b.cyanColor = cyanColor;
        b.lineColor = lineColor;
        b.glowIntensity = glowIntensity;
        return playable;
    }
}
