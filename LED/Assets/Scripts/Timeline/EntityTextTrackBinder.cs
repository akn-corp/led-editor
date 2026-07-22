// Relie les pistes Timeline :
//   Mur (texte / fluid / paloma / media) → composants sur LedWall
//   Devices (lyres / projecteur) → DeviceManager

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public static class EntityTextTrackBinder
{
    public static void BindAll(PlayableDirector director, LedWallTextPainter painter)
    {
        BindAll(director, painter, null, null, true);
    }

    public static void BindAll(PlayableDirector director, LedWallTextPainter painter, EntityManager entityManager)
    {
        BindAll(director, painter, entityManager, null, true);
    }

    public static void BindAll(
        PlayableDirector director,
        LedWallTextPainter painter,
        EntityManager entityManager,
        DeviceManager deviceManager)
    {
        BindAll(director, painter, entityManager, deviceManager, true);
    }

    public static void BindAll(
        PlayableDirector director,
        LedWallTextPainter painter,
        EntityManager entityManager,
        DeviceManager deviceManager,
        bool rebuildGraph)
    {
        if (director == null) return;

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) return;

        var wall = painter != null ? painter.GetComponent<LedWallVisualizer>() : null;
        if (wall == null)
            wall = Object.FindFirstObjectByType<LedWallVisualizer>();

        // Edit Mode : LedWall est DontSave — récupère via un binding déjà posé
        if (wall == null)
        {
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is FluidWallTrack || track is PalomaRumbaTrack || track is WallMediaTrack
                    || track is RippleWaveTrack || track is ConvergenceGridTrack)
                {
                    wall = director.GetGenericBinding(track) as LedWallVisualizer;
                    if (wall != null) break;
                    if (director.GetGenericBinding(track) is EntityManager)
                        break;
                }
            }
        }
        if (wall == null)
        {
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is not EntityTextTrack) continue;
                var painterBound = director.GetGenericBinding(track) as LedWallTextPainter;
                if (painterBound == null) continue;
                wall = painterBound.GetComponent<LedWallVisualizer>();
                if (wall != null) break;
            }
        }

        int wallBound = 0;
        int deviceBound = 0;
        int entityBound = 0;

        foreach (var track in timeline.GetOutputTracks())
        {
            if (track is EntityTextTrack && painter != null)
            {
                director.SetGenericBinding(track, painter);
                wallBound++;
            }
            else if (track is EntityTextTrack && painter == null)
            {
                // Garde le binding existant si painter non résolu
                if (director.GetGenericBinding(track) != null)
                    wallBound++;
            }
            else if ((track is FluidWallTrack || track is PalomaRumbaTrack || track is WallMediaTrack) && wall != null)
            {
                director.SetGenericBinding(track, wall);
                wallBound++;
            }
            else if ((track is RippleWaveTrack || track is ConvergenceGridTrack) && entityManager != null)
            {
                director.SetGenericBinding(track, entityManager);
                wallBound++;
            }
            else if (track is DeviceTrack && deviceManager != null)
            {
                director.SetGenericBinding(track, deviceManager);
                deviceBound++;
            }
            else if (track is EntityColorTrack && entityManager != null)
            {
                director.SetGenericBinding(track, entityManager);
                entityBound++;
            }
        }

        if (wallBound == 0 && entityBound == 0 && deviceBound == 0)
        {
            Debug.LogWarning("[TimelineBinder] Aucune piste à binder.");
            return;
        }

        if (!rebuildGraph)
            return;

        double t = director.time;
        bool wasPlaying = director.state == PlayState.Playing;
        director.RebuildGraph();
        director.time = t;
        director.Evaluate();

        if (Application.isPlaying && (wasPlaying || director.state != PlayState.Playing))
            director.Play();

        Debug.Log($"[TimelineBinder] wall={wallBound}, entityColor={entityBound}, device={deviceBound} @ t={t:F2}s");
    }

    /// <summary>
    /// Pilote DeviceManager depuis les clips (Edit Preview Timeline).
    /// Le mixer Playable n’anime pas toujours les devices hors Play Mode.
    /// </summary>
    public static bool DriveDevicesFromTimeline(PlayableDirector director, DeviceManager deviceManager)
    {
        if (director == null || deviceManager == null) return false;

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) return false;

        double time = director.time;
        bool any = false;

        foreach (var track in timeline.GetOutputTracks())
        {
            if (track is not DeviceTrack deviceTrack) continue;
            if (deviceTrack.muted) continue;

            director.SetGenericBinding(deviceTrack, deviceManager);

            foreach (var clip in deviceTrack.GetClips())
            {
                if (time < clip.start || time >= clip.start + clip.duration)
                    continue;

                float localTime = (float)((time - clip.start) * clip.timeScale + clip.clipIn);

                if (clip.asset is DeviceShowClip showClip)
                {
                    showClip.ApplyTo(deviceManager, localTime);
                    any = true;
                }
                else if (clip.asset is DeviceClip deviceClip)
                {
                    var state = deviceClip.EvaluateAt(localTime);
                    if (state.deviceId < DeviceManager.DeviceCount)
                    {
                        deviceManager.SetDevice(state);
                        any = true;
                    }
                }
            }
        }

        return any;
    }

    /// <summary>
    /// Pilote le mur depuis Wall Media (Edit Preview Timeline).
    /// </summary>
    public static bool DriveWallMediaFromTimeline(PlayableDirector director, LedWallVisualizer wall)
    {
        if (director == null) return false;

        if (wall == null || !wall.IsBuilt)
        {
            var timelineProbe = director.playableAsset as TimelineAsset;
            if (timelineProbe != null)
            {
                foreach (var track in timelineProbe.GetOutputTracks())
                {
                    wall = director.GetGenericBinding(track) as LedWallVisualizer;
                    if (wall != null && wall.IsBuilt) break;
                    wall = null;
                }
            }
        }

        if (wall == null || !wall.IsBuilt || wall.EntityManager == null)
            return false;

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) return false;

        double time = director.time;
        bool any = false;

        foreach (var track in timeline.GetOutputTracks())
        {
            if (track is not WallMediaTrack mediaTrack) continue;
            if (mediaTrack.muted) continue;

            director.SetGenericBinding(mediaTrack, wall);

            foreach (var clip in mediaTrack.GetClips())
            {
                if (time < clip.start || time >= clip.start + clip.duration)
                    continue;

                if (clip.asset is not WallMediaClip mediaClip) continue;

                float localTime = (float)((time - clip.start) * clip.timeScale + clip.clipIn);
                var behaviour = new WallMediaBehaviour
                {
                    sequence = mediaClip.sequence,
                    framesOverride = mediaClip.framesOverride,
                    framesPerSecond = mediaClip.framesPerSecond,
                    loop = mediaClip.loop,
                    sampleFilter = mediaClip.sampleFilter,
                    brightness = mediaClip.brightness,
                    backgroundColor = mediaClip.backgroundColor,
                };
                behaviour.Apply(wall.EntityManager, wall, localTime);
                any = true;
            }
        }

        return any;
    }
}
