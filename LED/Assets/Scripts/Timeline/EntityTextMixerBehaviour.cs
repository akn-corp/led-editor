// Mixer EntityText : chaque frame, peint le(s) clip(s) actifs via LedWallTextPainter.

using UnityEngine;
using UnityEngine.Playables;

public class EntityTextMixerBehaviour : PlayableBehaviour
{
    private bool _wasActive;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var painter = playerData as LedWallTextPainter;
        if (painter == null || !painter.IsReady) return;

        int inputCount = playable.GetInputCount();

        // Dernier clip actif gagne (même règle que EntityColorMixer)
        EntityTextBehaviour best = null;
        Playable bestPlayable = default;
        float bestWeight = 0f;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = (ScriptPlayable<EntityTextBehaviour>)playable.GetInput(i);
            var behaviour = input.GetBehaviour();
            if (behaviour == null) continue;

            if (weight >= bestWeight)
            {
                bestWeight = weight;
                best = behaviour;
                bestPlayable = input;
            }
        }

        if (best != null)
        {
            float opacity = best.EvaluateOpacity(bestPlayable) * bestWeight;
            painter.PaintText(
                best.text,
                best.position,
                best.size,
                best.color,
                opacity,
                best.backgroundColor,
                best.fitToWall);
            _wasActive = true;
        }
        // Ne pas Clear(black) en fin de clip : FluidWall / Paloma peignent le mur ensuite.
        else
        {
            _wasActive = false;
        }
    }
}
