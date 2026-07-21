// Mur Glassworks 128×128 — texture LED + animations (fluide / typo cinétique).

using UnityEngine;

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
    [SerializeField] private WallAnimationMode animationMode = WallAnimationMode.KineticTypography;

    [Header("Test de validation (à désactiver une fois vérifié)")]
    [SerializeField] private bool runQuickValidationTest = false;
    [SerializeField] private int testEntityId = 228;
    [SerializeField] private float testDelaySeconds = 2f;

    private LedWallVisualizer _wallVisualizer;
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

        bool drivesWall = animationMode != WallAnimationMode.None;
        _wallVisualizer.SetSuppressSingleUpdates(drivesWall);

        switch (animationMode)
        {
            case WallAnimationMode.Fluid:
            {
                var fluid = wallGo.AddComponent<FluidWallAnimator>();
                fluid.Initialize(entityManager, _wallVisualizer, config.columns);
                break;
            }
            case WallAnimationMode.KineticTypography:
            {
                var kinetic = wallGo.AddComponent<KineticTypographyAnimator>();
                kinetic.Initialize(entityManager, _wallVisualizer, config.columns);
                break;
            }
        }

        if (Application.isPlaying)
            FitCameraToWall(config.columns);

        _built = true;

        Debug.Log($"[SceneBuilder] Mur Glassworks chargé — anim={animationMode}.");

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
