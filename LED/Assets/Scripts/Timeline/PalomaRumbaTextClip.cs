// Clip Timeline — bandeau défilant « PALOMA RUMBA » (or / rouge).

using UnityEngine;
using UnityEngine.Playables;

public class PalomaRumbaTextClip : PlayableAsset
{
    public float bpm = 120f;
    public float maxSpeed = 12f;
    public Color goldColor = new Color(0.9f, 0.76f, 0.16f);
    public Color redColor = new Color(0.85f, 0f, 0.07f);
    public PalomaFontType fontType = PalomaFontType.Bold_8x12;
    [Min(6)] public int bandHeight = 16;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<PalomaRumbaTextBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.bpm = bpm;
        behaviour.maxSpeed = maxSpeed;
        behaviour.goldColor = goldColor;
        behaviour.redColor = redColor;
        behaviour.fontType = fontType;
        behaviour.bandHeight = bandHeight;
        return playable;
    }
}
