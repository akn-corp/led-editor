#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sync manuel wall-bands depuis le Routing Hub (HTTP :6456).
/// Menu : LED > Sync Wall Bands from Hub
/// </summary>
public static class HubConfigSyncMenu
{
    const string PrefsKeyUrl = "LED.HubConfigBaseUrl";

    [MenuItem("LED/Sync Wall Bands from Hub")]
    public static void SyncFromHub()
    {
        string baseUrl = EditorPrefs.GetString(PrefsKeyUrl, HubConfigClient.DefaultConfigBaseUrl);

        bool ok = EditorUtility.DisplayDialog(
            "Sync Wall Bands",
            $"Interroger le Hub ?\n\n{HubConfigClient.TrimSlash(baseUrl)}/api/wall-bands\n\n" +
            "Pour changer l’URL : menu LED > Set Hub Config URL…",
            "Sync",
            "Annuler");
        if (!ok) return;

        if (!HubConfigClient.TryFetchWallBandsSync(baseUrl, 3f, out var config, out var error))
        {
            EditorUtility.DisplayDialog(
                "Routing Hub",
                $"Sync échoué.\n\n{error}\n\nVérifie que le Hub tourne (npm start) et l’API :6456.",
                "OK");
            return;
        }

        WallBandsCache.Save(config);

        string resourcesPath = "Assets/Resources/Configs/wall-bands.json";
        bool writeResources = EditorUtility.DisplayDialog(
            "Routing Hub — Sync OK",
            $"{config.columns} colonnes" +
            (string.IsNullOrEmpty(config.profile) ? "" : $", profil « {config.profile} »") +
            $"\n\nCache : {WallBandsCache.CachePath}\n\n" +
            "Écrire aussi dans Resources/Configs/wall-bands.json ?",
            "Oui, écrire Resources",
            "Cache seulement");

        if (writeResources)
        {
            System.IO.File.WriteAllText(resourcesPath, WallBandsValidator.ToJson(config) + "\n");
            AssetDatabase.Refresh();
            Debug.Log($"[HubConfigSync] Resources mis à jour → {resourcesPath}");
        }

        Debug.Log(
            $"[HubConfigSync] OK — {config.columns} bandes, source Hub {HubConfigClient.TrimSlash(baseUrl)}");
    }

    [MenuItem("LED/Set Hub Config URL…")]
    public static void SetHubConfigUrl()
    {
        string current = EditorPrefs.GetString(PrefsKeyUrl, HubConfigClient.DefaultConfigBaseUrl);
        // Unity n’a pas d’input dialog natif : on log + reset possible via dialog
        bool reset = EditorUtility.DisplayDialog(
            "Hub Config URL",
            $"URL actuelle :\n{current}\n\n" +
            "Remettre le défaut http://127.0.0.1:6456 ?\n\n" +
            "(Sinon change EditorPrefs key LED.HubConfigBaseUrl, " +
            "ou le champ sur InstallationLoader en Play.)",
            "Défaut 6456",
            "Garder");
        if (reset)
        {
            EditorPrefs.SetString(PrefsKeyUrl, HubConfigClient.DefaultConfigBaseUrl);
            Debug.Log($"[HubConfigSync] URL = {HubConfigClient.DefaultConfigBaseUrl}");
        }
    }

    [MenuItem("LED/Open Wall Bands Cache Folder")]
    public static void OpenCacheFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
#endif
