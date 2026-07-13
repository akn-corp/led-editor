// Assets/Scripts/Installation/SceneBuilder.cs
//
// [ExecuteAlways] : ce script s'exécute aussi en mode Édition (hors Play
// Mode), pas seulement pendant le jeu. C'est ce qui permet à la grille
// LED de servir de "canevas" visible en permanence dans l'éditeur, sur
// lequel on peut composer une Timeline en la scrubant sans lancer le Play.
//
// Remplace QuickVisualTest.cs (script jetable de l'étape précédente).
// Instancie un GameObject (quad plat) par entité chargée depuis la config,
// avec un matériau "Unlit" (non affecté par l'éclairage de la scène) pour
// obtenir un vrai effet "écran de LED" plutôt que des cubes 3D éclairés.

using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
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
    private bool _built;

    void Start()
    {
        // Garde anti-duplication : Start() peut être appelé plusieurs fois
        // en mode Édition (recompilation de script, activation/désactivation
        // du GameObject...). Sans cette garde, on recréerait une grille
        // entière à chaque fois.
        if (_built) return;

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

            // En mode Édition, on ne veut pas que ces 1000+ objets générés
            // soient sauvegardés dans le fichier .unity à chaque Ctrl+S —
            // ce sont des visuels reconstruits à la volée, pas des données
            // de scène réelles. DontSaveInEditor les exclut de la sauvegarde
            // sans les cacher de la Hierarchy pendant que tu travailles.
            if (!Application.isPlaying)
                quad.hideFlags = HideFlags.DontSaveInEditor;

            // Matériau Unlit : couleur affichée telle quelle, sans ombre ni
            // reflet — comme un vrai pixel de LED. On construit le matériau
            // dans une variable locale avant de l'assigner (via
            // sharedMaterial) pour éviter que Unity ne le considère comme
            // "à dupliquer" en mode Édition.
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = Color.black; // éteint par défaut
            quad.GetComponent<Renderer>().sharedMaterial = material;

            _entityVisuals[entityData.id] = quad;

            minX = Mathf.Min(minX, entityData.x);
            maxX = Mathf.Max(maxX, entityData.x);
            minY = Mathf.Min(minY, entityData.y);
            maxY = Mathf.Max(maxY, entityData.y);
        }

        CreateBackground(minX, maxX, minY, maxY);

        entityManager.OnColorChanged += OnEntityColorChanged;
        _built = true;

        Debug.Log($"[SceneBuilder] {config.entities.Length} entités chargées et affichées.");

        // Le test de validation automatique n'a de sens qu'en Play Mode
        // (Invoke ne fonctionne pas hors Play, et on ne veut pas de
        // changement automatique de couleur pendant l'édition).
        if (runQuickValidationTest && Application.isPlaying)
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

        if (!Application.isPlaying)
            background.hideFlags = HideFlags.DontSaveInEditor;

        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = backgroundColor;
        background.GetComponent<Renderer>().sharedMaterial = material;
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
        quad.GetComponent<Renderer>().sharedMaterial.color = color;
    }

    void OnDestroy()
    {
        if (entityManager != null)
            entityManager.OnColorChanged -= OnEntityColorChanged;
    }
}