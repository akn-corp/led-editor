// Assets/Scripts/Installation/SceneBuilder.cs
//
// Remplace QuickVisualTest.cs (script jetable de l'étape précédente).
// Instancie un GameObject (cube) par entité chargée depuis la config, et
// garde le lien id -> GameObject pour mettre à jour la bonne couleur quand
// EntityManager notifie un changement.

using System.Collections.Generic;
using UnityEngine;

public class SceneBuilder : MonoBehaviour
{
    [SerializeField] private EntityManager entityManager;
    [SerializeField] private InstallationLoader installationLoader;
    [SerializeField] private float cubeScale = 0.3f;

    [Header("Test de validation (à désactiver une fois vérifié)")]
    [SerializeField] private bool runQuickValidationTest = true;
    [SerializeField] private int testEntityId = 7;
    [SerializeField] private float testDelaySeconds = 2f;

    private readonly Dictionary<int, GameObject> _entityVisuals = new Dictionary<int, GameObject>();

    void Start()
    {
        if (entityManager == null || installationLoader == null)
        {
            Debug.LogError("[SceneBuilder] Références manquantes dans l'Inspector (EntityManager / InstallationLoader).");
            return;
        }

        var config = installationLoader.LoadConfig();
        if (config == null) return; // l'erreur détaillée est déjà loggée par InstallationLoader

        foreach (var entityData in config.entities)
        {
            entityManager.RegisterEntity(entityData.id);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Entity_{entityData.id}";
            cube.transform.position = new Vector3(entityData.x, entityData.y, entityData.z);
            cube.transform.localScale = Vector3.one * cubeScale;
            cube.transform.SetParent(transform);

            _entityVisuals[entityData.id] = cube;
        }

        entityManager.OnColorChanged += OnEntityColorChanged;

        Debug.Log($"[SceneBuilder] {config.entities.Length} entités chargées et affichées.");

        // Test de validation : change la couleur d'UNE SEULE entité parmi
        // toutes celles chargées, pour vérifier que le bon cube (et lui
        // seul) réagit.
        if (runQuickValidationTest)
        {
            Invoke(nameof(TriggerValidationColor), testDelaySeconds);
        }
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

    private void OnEntityColorChanged(int id)
    {
        if (!_entityVisuals.TryGetValue(id, out var cube)) return;

        var state = entityManager.GetColor(id);
        if (state == null) return;

        var color = new Color(state.R / 255f, state.G / 255f, state.B / 255f);
        cube.GetComponent<Renderer>().material.color = color;
    }

    void OnDestroy()
    {
        if (entityManager != null)
            entityManager.OnColorChanged -= OnEntityColorChanged;
    }
}