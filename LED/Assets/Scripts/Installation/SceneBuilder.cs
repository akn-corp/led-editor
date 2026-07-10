// Assets/Scripts/Installation/SceneBuilder.cs
//
// Remplace QuickVisualTest.cs (script jetable de l'étape précédente).
// Instancie un GameObject (quad plat) par entité chargée depuis la config,
// avec un matériau "Unlit" (non affecté par l'éclairage de la scène) pour
// obtenir un vrai effet "écran de LED" plutôt que des cubes 3D éclairés.

using System.Collections.Generic;
using UnityEngine;

public class SceneBuilder : MonoBehaviour
{
    [SerializeField] private EntityManager entityManager;
    [SerializeField] private InstallationLoader installationLoader;
    [SerializeField] private float ledScale = 0.08f; // plus petit que le spacing pour garder des interstices visibles
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f);
    [SerializeField] private float backgroundMargin = 0.3f;

    [Header("Test de validation (à désactiver une fois vérifié)")]
    [SerializeField] private bool runQuickValidationTest = true;
    [SerializeField] private int testEntityId = 7;
    [SerializeField] private float testDelaySeconds = 2f;

    private readonly Dictionary<int, GameObject> _entityVisuals = new Dictionary<int, GameObject>();
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Unlit

    void Start()
    {
        if (entityManager == null || installationLoader == null)
        {
            Debug.LogError("[SceneBuilder] Références manquantes dans l'Inspector (EntityManager / InstallationLoader).");
            return;
        }

        var config = installationLoader.LoadConfig();
        if (config == null) return;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var entityData in config.entities)
        {
            entityManager.RegisterEntity(entityData.id);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"Entity_{entityData.id}";
            quad.transform.position = new Vector3(entityData.x, entityData.y, entityData.z);
            quad.transform.localScale = Vector3.one * ledScale;
            quad.transform.SetParent(transform);

            // Matériau Unlit : couleur affichée telle quelle, sans ombre ni
            // reflet — comme un vrai pixel de LED.
            var renderer = quad.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            renderer.material.color = Color.black; // éteint par défaut

            _entityVisuals[entityData.id] = quad;

            minX = Mathf.Min(minX, entityData.x);
            maxX = Mathf.Max(maxX, entityData.x);
            minY = Mathf.Min(minY, entityData.y);
            maxY = Mathf.Max(maxY, entityData.y);
        }

        CreateBackground(minX, maxX, minY, maxY);

        entityManager.OnColorChanged += OnEntityColorChanged;

        Debug.Log($"[SceneBuilder] {config.entities.Length} entités chargées et affichées.");

        if (runQuickValidationTest)
        {
            Invoke(nameof(TriggerValidationColor), testDelaySeconds);
        }
    }

    private void CreateBackground(float minX, float maxX, float minY, float maxY)
    {
        var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Background";
        background.transform.SetParent(transform);

        float width = (maxX - minX) + backgroundMargin * 2f;
        float height = (maxY - minY) + backgroundMargin * 2f;
        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        // Légèrement derrière les LED (z plus grand) pour ne pas passer devant.
        background.transform.position = new Vector3(centerX, centerY, 0.05f);
        background.transform.localScale = new Vector3(width, height, 1f);

        var renderer = background.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        renderer.material.color = backgroundColor;
    }

    private void TriggerValidationColor()
    {
        if (!_entityVisuals.ContainsKey(testEntityId))
        {
            Debug.LogWarning($"[SceneBuilder] testEntityId={testEntityId} n'existe pas dans la config chargée.");
            return;
        }

        Debug.Log($"[SceneBuilder] Test : passage au rouge de l'entité {testEntityId} uniquement.");
        entityManager.SetColor(testEntityId, 255, 0, 0);
    }

    [SerializeField] private float glowIntensity = 1.8f; // >1 pour dépasser le seuil Bloom (effet visuel uniquement, ne change pas l'état réel)

    private void OnEntityColorChanged(int id)
    {
        if (!_entityVisuals.TryGetValue(id, out var quad)) return;

        var state = entityManager.GetColor(id);
        if (state == null) return;

        // L'état réel reste 0-255 (inchangé, c'est ce qui part sur le réseau).
        // Seul l'affichage est boosté pour déclencher le Bloom visuellement.
        var color = new Color(
            (state.R / 255f) * glowIntensity,
            (state.G / 255f) * glowIntensity,
            (state.B / 255f) * glowIntensity
        );
        quad.GetComponent<Renderer>().material.color = color;
    }

    void OnDestroy()
    {
        if (entityManager != null)
            entityManager.OnColorChanged -= OnEntityColorChanged;
    }
}