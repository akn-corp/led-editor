// Relie les pistes Timeline :
//   Mur (texte / fluid / paloma / media / wall effect / ripple / convergence) → LedWall
//   Devices (lyres / projecteur) → DeviceManager
//   EntityColor (optionnel) → EntityManager

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
            wall = RippleWaveBehaviour.FindLedWallVisualizer();

        // Edit Mode : LedWall est DontSave — récupère via un binding déjà posé
        if (wall == null)
        {
            foreach (var track in timeline.GetOutputTracks())
            {
                if (!IsLedWallTrack(track)) continue;
                wall = director.GetGenericBinding(track) as LedWallVisualizer;
                if (wall != null) break;
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
                if (director.GetGenericBinding(track) != null)
                    wallBound++;
            }
            else if (IsLedWallTrack(track) && wall != null)
            {
                director.SetGenericBinding(track, wall);
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

    static bool IsLedWallTrack(TrackAsset track)
    {
        return track is FluidWallTrack
            || track is PalomaRumbaTrack
            || track is WallMediaTrack
            || track is RippleWaveTrack
            || track is ConvergenceGridTrack
            || track is WallEffectTrack;
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
    /// Pilote le mur depuis toutes les pistes mur (Edit Preview Timeline).
    /// Les mixers Playable ne sont pas fiables hors Play Mode.
    /// </summary>
    public static bool DriveWallFromTimeline(PlayableDirector director, LedWallVisualizer wall)
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

        // Rebind LedWall (DontSave) à chaque scrub
        foreach (var track in timeline.GetOutputTracks())
        {
            if (IsLedWallTrack(track))
                director.SetGenericBinding(track, wall);
        }

        double time = director.time;
        bool any = false;

        foreach (var track in timeline.GetOutputTracks())
        {
            if (track.muted) continue;

            foreach (var clip in track.GetClips())
            {
                if (time < clip.start || time >= clip.start + clip.duration)
                    continue;

                float localTime = (float)((time - clip.start) * clip.timeScale + clip.clipIn);

                if (clip.asset is WallMediaClip mediaClip)
                {
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
                else if (clip.asset is RippleWaveClip rippleClip)
                {
                    var b = new RippleWaveBehaviour
                    {
                        originX = rippleClip.originX,
                        originY = rippleClip.originY,
                        wavelengthCells = rippleClip.wavelengthCells,
                        wavesPerSecond = rippleClip.wavesPerSecond,
                        lineWidthCells = rippleClip.lineWidthCells,
                        haloCells = rippleClip.haloCells,
                        haloStrength = rippleClip.haloStrength,
                        edgeNoiseCells = rippleClip.edgeNoiseCells,
                        distanceFade = rippleClip.distanceFade,
                        glowIntensity = rippleClip.glowIntensity,
                        tint = rippleClip.tint,
                    };
                    b.Apply(wall.EntityManager, wall, localTime, 1f);
                    any = true;
                }
                else if (clip.asset is FluidWallClip fluidClip)
                {
                    var b = new FluidWallBehaviour
                    {
                        speed = fluidClip.speed,
                        waveScale = fluidClip.waveScale,
                        glowIntensity = fluidClip.glowIntensity,
                    };
                    b.Apply(wall.EntityManager, wall, localTime);
                    any = true;
                }
                else if (clip.asset is ConvergenceGridClip gridClip)
                {
                    var b = new ConvergenceGridBehaviour
                    {
                        phase1Weight = gridClip.phase1Weight,
                        phase2Weight = gridClip.phase2Weight,
                        phase3Weight = gridClip.phase3Weight,
                        squareColumns = Mathf.Max(1, gridClip.squareColumns),
                        lineThicknessCells = gridClip.lineThicknessCells,
                        squareFillRatio = gridClip.squareFillRatio,
                        glowIntensity = gridClip.glowIntensity,
                        tint = gridClip.tint,
                        clipDuration = (float)clip.duration,
                    };
                    b.Apply(wall.EntityManager, wall, localTime, (float)clip.duration);
                    any = true;
                }
                else if (clip.asset is PalomaRumbaTextClip palomaClip)
                {
                    var b = new PalomaRumbaTextBehaviour
                    {
                        mode = palomaClip.mode,
                        bpm = palomaClip.bpm,
                        maxSpeed = palomaClip.maxSpeed,
                        goldColor = palomaClip.goldColor,
                        redColor = palomaClip.redColor,
                        fontType = palomaClip.fontType,
                        bandHeight = palomaClip.bandHeight,
                        palomaDirection = palomaClip.palomaDirection,
                        rumbaDirection = palomaClip.rumbaDirection,
                        palomaSpeed = palomaClip.palomaSpeed,
                        rumbaSpeed = palomaClip.rumbaSpeed,
                        rowPhase = palomaClip.rowPhase,
                        glyphScale = palomaClip.glyphScale,
                        rushEnd = palomaClip.rushEnd,
                        flashEnd = palomaClip.flashEnd,
                        holdEnd = palomaClip.holdEnd,
                    };
                    b.Apply(wall.EntityManager, wall, localTime, (float)clip.duration);
                    any = true;
                }
                else if (clip.asset is WallEffectClip effectClip)
                {
                    effectClip.ApplyToWall(wall, localTime);
                    any = true;
                }
            }
        }

        return any;
    }

    /// <summary>Compat : alias de DriveWallFromTimeline.</summary>
    public static bool DriveWallMediaFromTimeline(PlayableDirector director, LedWallVisualizer wall)
        => DriveWallFromTimeline(director, wall);
}
