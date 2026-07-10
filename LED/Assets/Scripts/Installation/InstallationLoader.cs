// Assets/Scripts/Installation/InstallationLoader.cs
//
// Charge un fichier de config JSON décrivant l'installation (liste d'entités
// + positions). Le fichier doit être placé dans un dossier "Resources" pour
// être garanti disponible aussi bien dans l'éditeur que dans un build final.
//
// Exemple de chemin : Assets/Resources/Configs/installation-test.json
//   -> configResourcePath = "Configs/installation-test" (sans extension)

using UnityEngine;

public class InstallationLoader : MonoBehaviour
{
    [SerializeField] private string configResourcePath = "Configs/installation-test";

    /// <summary>
    /// Charge et parse le fichier de config. Retourne null si le fichier est
    /// introuvable ou invalide (avec un log d'erreur explicite).
    /// </summary>
    public InstallationConfig LoadConfig()
    {
        var textAsset = Resources.Load<TextAsset>(configResourcePath);

        if (textAsset == null)
        {
            Debug.LogError($"[InstallationLoader] Fichier introuvable : Resources/{configResourcePath}.json");
            return null;
        }

        InstallationConfig config;
        try
        {
            config = JsonUtility.FromJson<InstallationConfig>(textAsset.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InstallationLoader] JSON invalide : {e.Message}");
            return null;
        }

        if (config?.entities == null || config.entities.Length == 0)
        {
            Debug.LogError("[InstallationLoader] Config vide ou mal formée.");
            return null;
        }

        return config;
    }
}