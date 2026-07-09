// Assets/Scripts/Test/QuickVisualTest.cs
//
// Script de test JETABLE — sert uniquement à valider que EntityManager
// fonctionne et qu'un changement de couleur est bien visible. À supprimer
// (ou déplacer hors du build) une fois que Dev 1 a le vrai SceneBuilder
// qui instancie les entités depuis un fichier de config.

using UnityEngine;

public class QuickVisualTest : MonoBehaviour
{
    [SerializeField] private EntityManager entityManager;
    [SerializeField] private float delayBeforeColorChange = 2f;

    private const int TestEntityId = 0;
    private GameObject _cube;

    void Start()
    {
        if (entityManager == null)
        {
            Debug.LogError("[QuickVisualTest] Glisse une référence EntityManager dans l'Inspector.");
            return;
        }

        // 1. On enregistre une entité de test.
        entityManager.RegisterEntity(TestEntityId);

        // 2. On crée un cube dans la scène pour représenter visuellement cette entité.
        _cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _cube.transform.position = Vector3.zero;

        // 3. On s'abonne à l'événement : dès que la couleur change dans
        //    EntityManager, on met à jour le matériau du cube.
        entityManager.OnColorChanged += OnEntityColorChanged;

        // 4. Après un petit délai (pour bien voir le changement), on
        //    déclenche un passage au rouge — comme le ferait un clip de
        //    Dev 2 plus tard.
        Invoke(nameof(TriggerRed), delayBeforeColorChange);
    }

    private void TriggerRed()
    {
        Debug.Log("[QuickVisualTest] Passage au rouge...");
        entityManager.SetColor(TestEntityId, 255, 0, 0);
    }

    private void OnEntityColorChanged(int id)
    {
        if (id != TestEntityId) return;

        var state = entityManager.GetColor(id);
        if (state == null) return;

        var color = new Color(state.R / 255f, state.G / 255f, state.B / 255f);
        _cube.GetComponent<Renderer>().material.color = color;

        Debug.Log($"[QuickVisualTest] Entité {id} -> couleur affichée : {color}");
    }

    void OnDestroy()
    {
        if (entityManager != null)
            entityManager.OnColorChanged -= OnEntityColorChanged;
    }
}