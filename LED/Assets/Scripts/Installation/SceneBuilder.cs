// Mur Glassworks 128×128 — texture LED + animations.
//
// Montage Timeline (recommandé) :
//   animationMode = None
//   → LedWallTextPainter est toujours créé sur LedWall
//   → EntityTextTrackBinder relie auto EntityText / Fluid / Paloma / EntityColor / Device
//   → EntityText = paroles ; FluidWall = vague ; PalomaRumba = bandeau ; Device = lyres/RGBW

using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

[ExecuteAlways]
public class SceneBuilder : MonoBehaviour
{
    [SerializeField] private EntityManager entityManager;
    [SerializeField] private DeviceManager deviceManager;
    [SerializeField] private InstallationLoader installationLoader;
    [SerializeField] private float wallCellSize = 0.05f;

    public enum WallAnimationMode
    {
        None,
        Fluid,
        KineticTypography,
    }

    [Header("Animation mur")]
    [Tooltip("None = Timeline pilote le mur + devices (EntityText / FluidWall / Paloma / EntityColor / Device). Fluid/Kinetic = démo auto hors Timeline.")]
    [SerializeField] private WallAnimationMode animationMode = WallAnimationMode.None;

    [Header("Test de validation (à désactiver une fois vérifié)")]
    [SerializeField] private bool runQuickValidationTest = false;
    [SerializeField] private int testEntityId = 228;
    [SerializeField] private float testDelaySeconds = 2f;

    private LedWallVisualizer _wallVisualizer;
    private LedWallTextPainter _textPainter;
    private bool _built;

    /// <summary>Construit mur/devices/bindings pour Preview Timeline Edit Mode (= même rendu que Play).</summary>
    public void EnsureBuiltForTimelinePreview()
    {
        // Après domain reload / destruction DontSave : forcer rebuild
        if (_built && (_wallVisualizer == null || _textPainter == null))
            _built = false;

        if (_built) return;
        if (entityManager == null || installationLoader == null) return;

        if (deviceManager == null)
            deviceManager = FindFirstObjectByType<DeviceManager>();
        deviceManager?.EnsureDefaults();

        var config = installationLoader.LoadWallBandsConfig();
        BuildWithConfig(config);
    }

    void OnEnable()
    {
        // Edit Mode : préparer dès l’ouverture de scène pour Timeline (espace)
        if (!Application.isPlaying && !_built)
            EnsureBuiltForTimelinePreview();
    }

    void Start()
    {
        if (_built) return;

        if (entityManager == null || installationLoader == null)
        {
            Debug.LogError("[SceneBuilder] Références manquantes dans l'Inspector (EntityManager / InstallationLoader).");
            return;
        }

        if (deviceManager == null)
            deviceManager = FindFirstObjectByType<DeviceManager>();
        deviceManager?.EnsureDefaults();

        if (Application.isPlaying)
            StartCoroutine(BuildRoutine());
        else
            BuildWithConfig(installationLoader.LoadWallBandsConfig());
    }

    IEnumerator BuildRoutine()
    {
        var director = FindFirstObjectByType<PlayableDirector>();
        double resumeTime = 0;
        bool shouldPlay = false;
        if (director != null)
        {
            resumeTime = director.time;
            shouldPlay = director.state == PlayState.Playing || director.initialTime >= 0;
            // Empêche la Timeline de tourner avant que le mur soit prêt
            director.Stop();
        }

        WallBandsConfig config = null;
        yield return installationLoader.LoadWallBandsConfigRoutine(cfg => config = cfg);
        BuildWithConfig(config);

        if (director != null && Application.isPlaying)
        {
            director.time = resumeTime;
            director.Evaluate();
            director.Play();
        }
    }

    void BuildWithConfig(WallBandsConfig config)
    {
        if (_built) return;
        if (config == null) return;

        var wallGo = new GameObject("LedWall");
        wallGo.transform.SetParent(transform, false);
        if (!Application.isPlaying)
            wallGo.hideFlags = HideFlags.DontSaveInEditor;

        _wallVisualizer = wallGo.AddComponent<LedWallVisualizer>();
        _wallVisualizer.Build(entityManager, config, wallCellSize);

        // Toujours dispo pour binding Timeline Entity Text Track
        _textPainter = wallGo.AddComponent<LedWallTextPainter>();
        _textPainter.Initialize(entityManager, _wallVisualizer, config.columns);

        bool drivesWall = animationMode != WallAnimationMode.None;
        if (!drivesWall)
            _wallVisualizer.SetSuppressSingleUpdates(false);

        switch (animationMode)
        {
            case WallAnimationMode.Fluid:
            {
                _wallVisualizer.SetSuppressSingleUpdates(true);
                var fluid = wallGo.AddComponent<FluidWallAnimator>();
                fluid.Initialize(entityManager, _wallVisualizer, config.columns);
                break;
            }
            case WallAnimationMode.KineticTypography:
            {
                _wallVisualizer.SetSuppressSingleUpdates(true);
                var kinetic = wallGo.AddComponent<KineticTypographyAnimator>();
                kinetic.Initialize(entityManager, _wallVisualizer, config.columns);
                break;
            }
            case WallAnimationMode.None:
            default:
                // Timeline Fluid/Paloma/EntityText/WallMedia écrivent via ApplyDisplayPixels / painter.
                _wallVisualizer.SetSuppressSingleUpdates(true);
                break;
        }

        var director = FindFirstObjectByType<PlayableDirector>();
        if (director != null)
            EntityTextTrackBinder.BindAll(director, _textPainter, entityManager, deviceManager);

        // Preview 3D sur DeviceManager (GO persisté) — PAS sur LedWall DontSaveInEditor,
        // sinon FindFirstObjectByType / Update Edit Mode ne trouvent/rafraîchissent rien.
        if (deviceManager != null)
        {
            float wallWidth = config.columns * wallCellSize;
            var preview = deviceManager.GetComponent<DevicePreviewVisualizer>();
            if (preview == null)
                preview = deviceManager.gameObject.AddComponent<DevicePreviewVisualizer>();
            preview.Initialize(deviceManager, wallWidth);
        }

        FitCameraToWall(config.columns);

        _built = true;

        string source = installationLoader != null
            ? installationLoader.LastSource.ToString()
            : "?";
        Debug.Log(
            $"[SceneBuilder] Mur Glassworks chargé — anim={animationMode}, source={source}" +
            (string.IsNullOrEmpty(config.profile) ? "" : $", profil={config.profile}") +
                ". Timeline : mur (texte/fluid/paloma) → LedWall ; devices → DeviceManager. Preview ON.");

        if (runQuickValidationTest && Application.isPlaying && animationMode == WallAnimationMode.None)
            Invoke(nameof(TriggerValidationColor), testDelaySeconds);
    }

    private void FitCameraToWall(int columns)
    {
        var cam = Camera.main;
        if (cam == null) return;

        float worldWidth = columns * wallCellSize;
        float worldHeight = WallMapping.VisibleRows * wallCellSize;
        Vector3 wallCenter = transform.position;

        // Cadrer mur + rangée devices (sinon lyres hors cadre avec ortho ~3.5)
        DevicePreviewVisualizer preview = null;
        if (deviceManager != null)
            preview = deviceManager.GetComponent<DevicePreviewVisualizer>();
        if (preview == null)
            preview = FindFirstObjectByType<DevicePreviewVisualizer>();

        Vector3 viewCenter = wallCenter;
        float halfHeight = worldHeight * 0.5f + 0.6f;
        float halfWidth = worldWidth * 0.5f + 0.6f;

        if (preview != null)
        {
            viewCenter = wallCenter + preview.RecommendedViewCenter;
            halfHeight = Mathf.Max(halfHeight, preview.RecommendedHalfHeight);
            halfWidth = Mathf.Max(halfWidth, worldWidth * 0.5f + 1.2f);
        }
        else
        {
            // Fallback : descendre le cadre pour la zone sous le mur
            viewCenter.y -= worldHeight * 0.22f;
            halfHeight += 1.8f;
        }

        cam.orthographic = true;
        cam.transform.position = new Vector3(viewCenter.x, viewCenter.y, -8f);
        cam.transform.rotation = Quaternion.identity;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f);
        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / Mathf.Max(0.1f, cam.aspect));

        Debug.Log($"[SceneBuilder] Caméra ajustée — orthoSize={cam.orthographicSize:F2}, centerY={viewCenter.y:F2}");
    }

    private void TriggerValidationColor()
    {
        Debug.Log($"[SceneBuilder] Test : entité {testEntityId} → rouge.");
        entityManager.SetColor(testEntityId, 255, 0, 0);
    }
}
