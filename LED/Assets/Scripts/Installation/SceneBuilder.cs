// Mur Glassworks 128×128 — texture LED + animations.
//
// Montage Timeline (recommandé) :
//   animationMode = None
//   → LedWallTextPainter est toujours créé sur LedWall
//   → EntityTextTrackBinder relie auto EntityText / Fluid / Paloma / EntityColor
//   → Entity Text = paroles ; FluidWall = vague ; PalomaRumba = bandeau or/rouge

using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

[ExecuteAlways]
public class SceneBuilder : MonoBehaviour
{
    [SerializeField] private EntityManager entityManager;
    [SerializeField] private InstallationLoader installationLoader;
    [SerializeField] private float wallCellSize = 0.05f;

    public enum WallAnimationMode
    {
        None,
        Fluid,
        KineticTypography,
    }

    [Header("Animation mur")]
    [Tooltip("None = Timeline pilote le mur (EntityText / FluidWall / Paloma / EntityColor). Fluid/Kinetic = démo auto hors Timeline.")]
    [SerializeField] private WallAnimationMode animationMode = WallAnimationMode.None;

    [Header("Test de validation (à désactiver une fois vérifié)")]
    [SerializeField] private bool runQuickValidationTest = false;
    [SerializeField] private int testEntityId = 228;
    [SerializeField] private float testDelaySeconds = 2f;

    private LedWallVisualizer _wallVisualizer;
    private LedWallTextPainter _textPainter;
    private bool _built;

    void Start()
    {
        if (_built) return;

        if (entityManager == null || installationLoader == null)
        {
            Debug.LogError("[SceneBuilder] Références manquantes dans l'Inspector (EntityManager / InstallationLoader).");
            return;
        }

        if (Application.isPlaying)
            StartCoroutine(BuildRoutine());
        else
            BuildWithConfig(installationLoader.LoadWallBandsConfig());
    }

    IEnumerator BuildRoutine()
    {
        WallBandsConfig config = null;
        yield return installationLoader.LoadWallBandsConfigRoutine(cfg => config = cfg);
        BuildWithConfig(config);
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
                // Fluid/Paloma Timeline poussent via ApplyDisplayPixels ; EntityColor via OnColorChanged.
                break;
        }

        var director = FindFirstObjectByType<PlayableDirector>();
        if (director != null)
            EntityTextTrackBinder.BindAll(director, _textPainter, entityManager);

        if (Application.isPlaying)
            FitCameraToWall(config.columns);

        _built = true;

        string source = installationLoader != null
            ? installationLoader.LastSource.ToString()
            : "?";
        Debug.Log(
            $"[SceneBuilder] Mur Glassworks chargé — anim={animationMode}, source={source}" +
            (string.IsNullOrEmpty(config.profile) ? "" : $", profil={config.profile}") +
            ". Timeline : EntityText + FluidWall + PalomaRumba auto-bindés.");

        if (runQuickValidationTest && Application.isPlaying && animationMode == WallAnimationMode.None)
            Invoke(nameof(TriggerValidationColor), testDelaySeconds);
    }

    private void FitCameraToWall(int columns)
    {
        var cam = Camera.main;
        if (cam == null) return;

        float worldWidth = columns * wallCellSize;
        float worldHeight = WallMapping.VisibleRows * wallCellSize;
        float margin = 0.15f;
        Vector3 wallCenter = transform.position;

        cam.orthographic = true;
        cam.transform.position = new Vector3(wallCenter.x, wallCenter.y, -8f);
        cam.transform.rotation = Quaternion.identity;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f);

        float halfHeight = worldHeight * 0.5f + margin;
        float halfWidth = worldWidth * 0.5f + margin;
        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / cam.aspect);

        Debug.Log($"[SceneBuilder] Caméra ajustée — orthoSize={cam.orthographicSize:F2}");
    }

    private void TriggerValidationColor()
    {
        Debug.Log($"[SceneBuilder] Test : entité {testEntityId} → rouge.");
        entityManager.SetColor(testEntityId, 255, 0, 0);
    }
}
