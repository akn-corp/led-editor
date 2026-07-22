// Client HTTP config Routing Hub — GET /api/health, /api/active-profile, /api/wall-bands
// Port HTTP 6456 (distinct de l’UDP state 6455).

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class HubConfigClient
{
    public const string DefaultConfigBaseUrl = "http://127.0.0.1:6456";

    [Serializable]
    public class HubHealth
    {
        public bool ok;
        public int version;
        public bool running;
        public int configApiPort;
        public int statePort;
    }

    [Serializable]
    public class HubActiveProfile
    {
        public string id;
        public string label;
    }

    public static string TrimSlash(string url)
    {
        if (string.IsNullOrEmpty(url)) return DefaultConfigBaseUrl;
        return url.TrimEnd('/');
    }

    public static IEnumerator FetchWallBands(
        string baseUrl,
        float timeoutSeconds,
        Action<WallBandsConfig> onSuccess,
        Action<string> onError)
    {
        string url = $"{TrimSlash(baseUrl)}/api/wall-bands";
        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.Max(1, Mathf.CeilToInt(timeoutSeconds));
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"Hub wall-bands : {req.error} ({url})");
            yield break;
        }

        try
        {
            var config = WallBandsValidator.ParseAndValidate(req.downloadHandler.text);
            onSuccess?.Invoke(config);
        }
        catch (Exception e)
        {
            onError?.Invoke(e.Message);
        }
    }

    public static IEnumerator FetchHealth(
        string baseUrl,
        float timeoutSeconds,
        Action<HubHealth> onSuccess,
        Action<string> onError)
    {
        string url = $"{TrimSlash(baseUrl)}/api/health";
        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.Max(1, Mathf.CeilToInt(timeoutSeconds));
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"Hub health : {req.error}");
            yield break;
        }

        try
        {
            onSuccess?.Invoke(JsonUtility.FromJson<HubHealth>(req.downloadHandler.text));
        }
        catch (Exception e)
        {
            onError?.Invoke(e.Message);
        }
    }

    public static IEnumerator FetchActiveProfile(
        string baseUrl,
        float timeoutSeconds,
        Action<HubActiveProfile> onSuccess,
        Action<string> onError)
    {
        string url = $"{TrimSlash(baseUrl)}/api/active-profile";
        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.Max(1, Mathf.CeilToInt(timeoutSeconds));
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"Hub profile : {req.error}");
            yield break;
        }

        try
        {
            onSuccess?.Invoke(JsonUtility.FromJson<HubActiveProfile>(req.downloadHandler.text));
        }
        catch (Exception e)
        {
            onError?.Invoke(e.Message);
        }
    }

#if UNITY_EDITOR
    /// <summary>Appel bloquant pour menus Editor uniquement.</summary>
    public static bool TryFetchWallBandsSync(
        string baseUrl,
        float timeoutSeconds,
        out WallBandsConfig config,
        out string error)
    {
        config = null;
        error = null;
        string url = $"{TrimSlash(baseUrl)}/api/wall-bands";
        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.Max(1, Mathf.CeilToInt(timeoutSeconds));
        var op = req.SendWebRequest();
        while (!op.isDone) { }

        if (req.result != UnityWebRequest.Result.Success)
        {
            error = $"Hub wall-bands : {req.error} ({url})";
            return false;
        }

        try
        {
            config = WallBandsValidator.ParseAndValidate(req.downloadHandler.text);
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }
#endif
}
