// Relie automatiquement les pistes Timeline créées au runtime
// (LedWall / EntityManager n’existent pas toujours en Edit Mode).

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public static class EntityTextTrackBinder
{
    public static void BindAll(PlayableDirector director, LedWallTextPainter painter)
    {
        BindAll(director, painter, null);
    }

    public static void BindAll(PlayableDirector director, LedWallTextPainter painter, EntityManager entityManager)
    {
        if (director == null) return;

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) return;

        int textBound = 0;
        int entityBound = 0;

        foreach (var track in timeline.GetOutputTracks())
        {
            if (track is EntityTextTrack && painter != null)
            {
                director.SetGenericBinding(track, painter);
                textBound++;
                Debug.Log($"[TimelineBinder] EntityText « {track.name} » → {painter.gameObject.name}");
            }
            else if (entityManager != null &&
                     (track is FluidWallTrack || track is PalomaRumbaTrack || track is EntityColorTrack))
            {
                director.SetGenericBinding(track, entityManager);
                entityBound++;
                Debug.Log($"[TimelineBinder] {track.GetType().Name} « {track.name} » → EntityManager");
            }
        }

        if (textBound == 0 && entityBound == 0)
        {
            Debug.LogWarning("[TimelineBinder] Aucune piste EntityText / Fluid / Paloma / EntityColor à binder.");
            return;
        }

        double t = director.time;
        bool wasPlaying = director.state == PlayState.Playing;
        director.RebuildGraph();
        director.time = t;
        director.Evaluate();
        if (wasPlaying || director.state != PlayState.Playing)
            director.Play();

        Debug.Log($"[TimelineBinder] text={textBound}, entity={entityBound}, director reconstruit @ t={t:F2}s");
    }
}
