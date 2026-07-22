// Clip Timeline — convergence géométrique.
// Transition en 3 phases : lignes verticales -> quadrillage -> matrice de carrés.

using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class ConvergenceGridClip : PlayableAsset
{
    [Header("Phases (fractions de la durée du clip, normalisées ensemble)")]
    [Tooltip("Phase 1 : apparition des lignes verticales convergentes.")]
    public float phase1Weight = 1f;
    [Tooltip("Phase 2 : ajout des lignes horizontales (quadrillage).")]
    public float phase2Weight = 1f;
    [Tooltip("Phase 3 : morphing du quadrillage en matrice de carrés.")]
    public float phase3Weight = 1f;

    [Header("Structure de la grille")]
    [Tooltip("Nombre de colonnes de carrés. Le nombre de rangées est déduit " +
             "automatiquement pour garder des carrés parfaits (cellules LED carrées).")]
    public int squareColumns = 8;
    [Tooltip("Épaisseur d'un trait, en cellules LED.")]
    public float lineThicknessCells = 1.6f;

    [Header("Phase 3 — carrés")]
    [Range(0.2f, 0.9f)]
    [Tooltip("Proportion du module occupée par le carré. Plus petit = plus d'espace noir entre les carrés.")]
    public float squareFillRatio = 0.5f;

    [Header("Rendu / émission")]
    [Tooltip("Multiplicateur d'intensité (HDR) pour le bloom. >1 = fort effet d'émission.")]
    public float glowIntensity = 2.6f;
    [Tooltip("Teinte de la lumière. Blanc pur par défaut.")]
    public Color tint = Color.white;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<ConvergenceGridBehaviour>.Create(graph);
        var b = playable.GetBehaviour();
        b.phase1Weight = phase1Weight;
        b.phase2Weight = phase2Weight;
        b.phase3Weight = phase3Weight;
        b.squareColumns = Mathf.Max(1, squareColumns);
        b.lineThicknessCells = Mathf.Max(0.5f, lineThicknessCells);
        b.squareFillRatio = squareFillRatio;
        b.glowIntensity = glowIntensity;
        b.tint = tint;
        b.clipDuration = (float)duration;
        return playable;
    }
}
