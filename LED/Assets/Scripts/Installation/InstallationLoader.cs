// Charge wall-bands.json — mur Glassworks (entités ≥ 100, 16 576 LEDs).

using UnityEngine;

public class InstallationLoader : MonoBehaviour
{
    [SerializeField] private string wallBandsResourcePath = "Configs/wall-bands";

    public WallBandsConfig LoadWallBandsConfig()
    {
        var textAsset = Resources.Load<TextAsset>(wallBandsResourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"[InstallationLoader] Fichier introuvable : Resources/{wallBandsResourcePath}.json");
            return null;
        }

        WallBandsConfig config;
        try
        {
            config = JsonUtility.FromJson<WallBandsConfig>(textAsset.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InstallationLoader] JSON wall-bands invalide : {e.Message}");
            return null;
        }

        if (config?.bands == null || config.bands.Length == 0)
        {
            Debug.LogError("[InstallationLoader] wall-bands.json vide ou mal formé.");
            return null;
        }

        return config;
    }
}
