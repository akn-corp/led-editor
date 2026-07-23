#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Câble Main.unity : Installation + scripts + caméra ortho.
/// Menu : LED > Setup Main Scene
/// </summary>
public static class MainSceneSetup
{
    const string MainScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("LED/Setup Main Scene")]
    public static void SetupMainScene()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        EnsureInstallationHierarchy();
        ConfigureMainCamera();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MainSceneSetup] Main.unity câblée et sauvegardée.");
    }

    [MenuItem("LED/Add Viewport Demo Painter (32×32 test)")]
    public static void AddViewportDemoPainter()
    {
        var installation = GameObject.Find("Installation") ?? new GameObject("Installation");
        var painter = GetOrAddComponent<ViewportDemoPainter>(installation, "ViewportDemoPainter");
        Selection.activeGameObject = painter.gameObject;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[MainSceneSetup] ViewportDemoPainter ajouté — activer le profil demo-32x32 puis Play.");
    }

    static void EnsureInstallationHierarchy()
    {
        var installation = GameObject.Find("Installation") ?? new GameObject("Installation");

        var entityManager = GetOrAddComponent<EntityManager>(installation, "EntityManager");
        var deviceManager = GetOrAddComponent<DeviceManager>(installation, "DeviceManager");
        var installationLoader = GetOrAddComponent<InstallationLoader>(installation, "InstallationLoader");
        var sceneBuilder = GetOrAddComponent<SceneBuilder>(installation, "SceneBuilder");
        var stateExporter = GetOrAddComponent<StateExporter>(installation, "StateExporter");

        var builder = sceneBuilder.GetComponent<SceneBuilder>();
        var soBuilder = new SerializedObject(builder);
        soBuilder.FindProperty("entityManager").objectReferenceValue = entityManager;
        soBuilder.FindProperty("deviceManager").objectReferenceValue = deviceManager;
        soBuilder.FindProperty("installationLoader").objectReferenceValue = installationLoader;
        soBuilder.FindProperty("testEntityId").intValue = 228;
        soBuilder.FindProperty("wallCellSize").floatValue = 0.05f;
        soBuilder.ApplyModifiedPropertiesWithoutUndo();

        var soExporter = new SerializedObject(stateExporter);
        soExporter.FindProperty("entityManager").objectReferenceValue = entityManager;
        soExporter.FindProperty("deviceManager").objectReferenceValue = deviceManager;
        soExporter.FindProperty("sendDevices").boolValue = true;
        soExporter.FindProperty("targetPort").intValue = StateProtocol.StatePort;
        soExporter.ApplyModifiedPropertiesWithoutUndo();
    }

    static T GetOrAddComponent<T>(GameObject parent, string childName) where T : Component
    {
        var child = parent.transform.Find(childName)?.gameObject;
        if (child == null)
        {
            child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
        }

        var component = child.GetComponent<T>();
        if (component == null)
            component = child.AddComponent<T>();
        return component;
    }

    static void ConfigureMainCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[MainSceneSetup] Pas de Main Camera trouvée.");
            return;
        }

        cam.transform.position = new Vector3(0f, 0f, -8f);
        cam.transform.rotation = Quaternion.identity;
        cam.orthographic = true;
        cam.orthographicSize = 3.5f;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f);
    }
}
#endif
