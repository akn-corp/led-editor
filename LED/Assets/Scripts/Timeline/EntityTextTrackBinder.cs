// Relie automatiquement toutes les EntityTextTrack du PlayableDirector
// au LedWallTextPainter créé au runtime (LedWall n’existe pas en Edit Mode).

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public static class EntityTextTrackBinder
{
    public static void BindAll(PlayableDirector director, LedWallTextPainter painter)
    {
        if (director == null || painter == null) return;

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) return;

        int bound = 0;
        foreach (var track in timeline.GetOutputTracks())
        {
            if (track is not EntityTextTrack) continue;

            director.SetGenericBinding(track, painter);
            bound++;
            Debug.Log($"[EntityTextTrackBinder] Binding « {track.name} » → {painter.gameObject.name}");
        }

        if (bound == 0)
        {
            Debug.LogWarning("[EntityTextTrackBinder] Aucune EntityText Track dans la Timeline.");
            return;
        }

        // Le graphe a souvent démarré sans binding : reconstruire
        double t = director.time;
        bool wasPlaying = director.state == PlayState.Playing;
        director.RebuildGraph();
        director.time = t;
        director.Evaluate();
        if (wasPlaying || director.state != PlayState.Playing)
            director.Play();

        Debug.Log($"[EntityTextTrackBinder] {bound} piste(s) Entity Text reliée(s), director reconstruit @ t={t:F2}s");
    }
}
