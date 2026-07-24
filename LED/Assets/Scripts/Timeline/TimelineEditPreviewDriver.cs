// Assure mur + devices pour Preview Timeline Edit Mode (= Play Mode).
//
// LedWall est DontSaveInEditor : FindFirstObjectByType le rate souvent.
// On passe par SceneBuilder.WallVisualizer + Resources.FindObjectsOfTypeAll.

using UnityEngine;
using UnityEngine.Playables;

[ExecuteAlways]
[RequireComponent(typeof(PlayableDirector))]
public class TimelineEditPreviewDriver : MonoBehaviour
{
    [SerializeField] private SceneBuilder sceneBuilder;
    [SerializeField] private DeviceManager deviceManager;
    [SerializeField] private bool ensureBuiltInEditMode = true;

    private PlayableDirector _director;
    private bool _editHooks;
    private double _lastSyncedTime = double.NaN;
    private double _lastLoggedTime = double.MinValue;
    private float _nextSyncRealtime;

    void OnEnable()
    {
        _director = GetComponent<PlayableDirector>();
        if (sceneBuilder == null)
            sceneBuilder = FindFirstObjectByType<SceneBuilder>();
        if (deviceManager == null)
            deviceManager = FindFirstObjectByType<DeviceManager>();

#if UNITY_EDITOR
        if (!Application.isPlaying && !_editHooks)
        {
            UnityEditor.EditorApplication.update += EditorUpdate;
            _editHooks = true;
        }
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        if (_editHooks)
        {
            UnityEditor.EditorApplication.update -= EditorUpdate;
            _editHooks = false;
        }
#endif
    }

    void Start()
    {
        if (Application.isPlaying) return;
        EnsureEditPreviewReady();
#if UNITY_EDITOR
        // Force un premier rendu preview même sans scrub
        _lastSyncedTime = double.NaN;
        SyncDevicesForEditPreview();
#endif
    }

#if UNITY_EDITOR
    void EditorUpdate()
    {
        if (Application.isPlaying || !ensureBuiltInEditMode) return;
        if (Time.realtimeSinceStartup < _nextSyncRealtime) return;
        _nextSyncRealtime = Time.realtimeSinceStartup + 0.05f;
        SyncDevicesForEditPreview();
    }
#endif

    public void EnsureEditPreviewReady()
    {
        if (sceneBuilder == null)
            sceneBuilder = FindFirstObjectByType<SceneBuilder>();
        sceneBuilder?.EnsureBuiltForTimelinePreview();
    }

    private void SyncDevicesForEditPreview()
    {
        if (_director == null)
            _director = GetComponent<PlayableDirector>();
        if (_director == null || _director.playableAsset == null) return;

        if (!double.IsNaN(_lastSyncedTime) && System.Math.Abs(_director.time - _lastSyncedTime) < 0.0001)
            return;
        _lastSyncedTime = _director.time;

        EnsureEditPreviewReady();

        if (deviceManager == null)
            deviceManager = FindFirstObjectByType<DeviceManager>();

        // LedWall DontSave : prioriser SceneBuilder, puis FindObjectsOfTypeAll
        var wall = ResolveLedWall();
        var painter = wall != null
            ? wall.GetComponent<LedWallTextPainter>()
            : sceneBuilder != null ? sceneBuilder.TextPainter : null;
        var entities = wall != null ? wall.EntityManager : null;
        if (entities == null)
            entities = FindFirstObjectByType<EntityManager>();

        EntityTextTrackBinder.BindAll(_director, painter, entities, deviceManager, rebuildGraph: false);
        if (wall != null)
            EntityTextTrackBinder.DriveWallFromTimeline(_director, wall);

        bool driven = false;
        if (deviceManager != null)
            driven = EntityTextTrackBinder.DriveDevicesFromTimeline(_director, deviceManager);

        if (deviceManager == null) return;

        var preview = deviceManager.GetComponent<DevicePreviewVisualizer>();
        if (preview == null)
            preview = FindPreviewVisualizer();
        if (preview == null) return;

        preview.RefreshFromManager();

        if (driven && System.Math.Abs(_director.time - _lastLoggedTime) >= 1.0)
        {
            _lastLoggedTime = _director.time;
            var d0 = deviceManager.GetDevice(0);
            var d1 = deviceManager.GetDevice(1);
            Debug.Log(
                $"[EditPreview] t={_director.time:F2}s projDim={d0.dimmer} lyre1 pan/tilt={d1.pan}/{d1.tilt}");
        }
    }

    private LedWallVisualizer ResolveLedWall()
    {
        if (sceneBuilder != null)
        {
            if (sceneBuilder.WallVisualizer != null && sceneBuilder.WallVisualizer.IsBuilt)
                return sceneBuilder.WallVisualizer;
        }

        return RippleWaveBehaviour.FindLedWallVisualizer();
    }

    private static DevicePreviewVisualizer FindPreviewVisualizer()
    {
        var all = Resources.FindObjectsOfTypeAll<DevicePreviewVisualizer>();
        for (int i = 0; i < all.Length; i++)
        {
            var p = all[i];
            if (p == null) continue;
            if (!p.gameObject.scene.IsValid()) continue;
            return p;
        }
        return null;
    }
}
