// Clip Timeline — Clash procédural PALOMA ↔ RUMBA (ou scroll legacy).

using UnityEngine;
using UnityEngine.Playables;

public class PalomaRumbaTextClip : PlayableAsset
{
    public PalomaAnimMode mode = PalomaAnimMode.Clash;

    [Header("Couleurs")]
    public Color goldColor = new Color(1f, 0.839f, 0f); // #FFD600
    public Color redColor = new Color(1f, 0.839f, 0f);

    [Header("Clash (1 s recommandé)")]
    [Range(1, 3)] public int glyphScale = 1;
    [Range(0.1f, 0.7f)] public float rushEnd = 0.38f;
    [Range(0.2f, 0.8f)] public float flashEnd = 0.46f;
    [Range(0.3f, 0.95f)] public float holdEnd = 0.58f;

    [Header("Scroll legacy")]
    public float bpm = 120f;
    public float maxSpeed = 14f;
    public PalomaFontType fontType = PalomaFontType.Micro_3x5;
    [Min(0)] public int bandHeight;
    public PalomaScrollDirection palomaDirection = PalomaScrollDirection.Right;
    public PalomaScrollDirection rumbaDirection = PalomaScrollDirection.Left;
    public float palomaSpeed;
    public float rumbaSpeed;
    [Range(0f, 1f)] public float rowPhase = 0.35f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<PalomaRumbaTextBehaviour>.Create(graph);
        var b = playable.GetBehaviour();
        b.mode = mode;
        b.goldColor = goldColor;
        b.redColor = redColor;
        b.glyphScale = glyphScale;
        b.rushEnd = rushEnd;
        b.flashEnd = flashEnd;
        b.holdEnd = holdEnd;
        b.bpm = bpm;
        b.maxSpeed = maxSpeed;
        b.fontType = fontType;
        b.bandHeight = bandHeight;
        b.palomaDirection = palomaDirection;
        b.rumbaDirection = rumbaDirection;
        b.palomaSpeed = palomaSpeed;
        b.rumbaSpeed = rumbaSpeed;
        b.rowPhase = rowPhase;
        return playable;
    }
}
