// Mur Glassworks 128×128 — texture LED + animations.
//
// Montage Timeline (recommandé) :
//   animationMode = None
//   → LedWallTextPainter est toujours créé sur LedWall
//   → EntityTextTrackBinder relie auto les pistes Entity Text au painter
//   → Add Entity Text Clip (texte, fades, OnStart/OnEnd dans l'Inspector)

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
    [Tooltip("None = Timeline pilote le mur (EntityColor / EntityText). Kinetic = démo auto hors Timeline.")]
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

        var config = installationLoader.LoadWallBandsConfig();
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
        // Painter active déjà suppress ; Color clips Timeline utilisent SetColor → OK si mode None
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
                // Timeline : EntityColorTrack + EntityTextTrack
                break;
        }

        // LedWall est runtime → binder les Entity Text Track maintenant
        var director = FindFirstObjectByType<PlayableDirector>();
        if (director != null)
            EntityTextTrackBinder.BindAll(director, _textPainter);

        if (Application.isPlaying)
            FitCameraToWall(config.columns);

        _built = true;

        Debug.Log(
            $"[SceneBuilder] Mur Glassworks chargé — anim={animationMode}. " +
            "EntityText : binder LedWall (LedWallTextPainter) sur une Entity Text Track.");

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
