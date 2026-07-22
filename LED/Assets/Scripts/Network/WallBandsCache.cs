// Cache local wall-bands (fallback si Hub down) — persistentDataPath.

using System.IO;
using UnityEngine;

public static class WallBandsCache
{
    const string FileName = "wall-bands-cache.json";

    public static string CachePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool TryLoad(out WallBandsConfig config)
    {
        config = null;
        try
        {
            if (!File.Exists(CachePath)) return false;
            string json = File.ReadAllText(CachePath);
            config = WallBandsValidator.ParseAndValidate(json);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[WallBandsCache] Lecture échouée : {e.Message}");
            return false;
        }
    }

    public static void Save(WallBandsConfig config)
    {
        if (config == null) return;
        try
        {
            string json = WallBandsValidator.ToJson(config);
            File.WriteAllText(CachePath, json);
            Debug.Log($"[WallBandsCache] Écrit → {CachePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[WallBandsCache] Écriture échouée : {e.Message}");
        }
    }
}
