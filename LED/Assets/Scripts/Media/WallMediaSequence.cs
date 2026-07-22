// Séquence d'images pour le mur LED (GIF / frames extraites).

using UnityEngine;

[CreateAssetMenu(menuName = "LED/Wall Media Sequence", fileName = "WallMediaSequence")]
public class WallMediaSequence : ScriptableObject
{
    [Tooltip("Frames dans l'ordre de lecture")]
    public Texture2D[] frames;

    [Min(1f)]
    public float framesPerSecond = 12f;

    [Tooltip("Boucler la séquence sur la durée du clip Timeline")]
    public bool loop = true;

    public int FrameCount => frames != null ? frames.Length : 0;

    public Texture2D GetFrameAtTime(float timeSeconds)
    {
        if (frames == null || frames.Length == 0) return null;
        float fps = Mathf.Max(0.01f, framesPerSecond);
        int index;
        if (loop)
        {
            float t = timeSeconds * fps;
            index = Mathf.FloorToInt(t) % frames.Length;
            if (index < 0) index += frames.Length;
        }
        else
        {
            index = Mathf.Clamp(Mathf.FloorToInt(timeSeconds * fps), 0, frames.Length - 1);
        }
        return frames[index];
    }
}
