// Charge wall-bands : Hub HTTP :6456 → cache local → Resources embarqué.

using System;
using System.Collections;
using UnityEngine;

public class InstallationLoader : MonoBehaviour
{
    public enum MappingSource
    {
        None,
        Hub,
        Cache,
        Resources,
    }

    [Header("Resources (fallback offline)")]
    [SerializeField] private string wallBandsResourcePath = "Configs/wall-bands";

    [Header("Routing Hub — API config HTTP")]
    [Tooltip("Base URL HTTP (pas l’UDP state 6455). Défaut : http://127.0.0.1:6456")]
    [SerializeField] private string configBaseUrl = HubConfigClient.DefaultConfigBaseUrl;

    [SerializeField] private bool syncFromHubOnStart = true;
    [SerializeField] private float hubTimeoutSeconds = 2f;

    public string ConfigBaseUrl => configBaseUrl;
    public MappingSource LastSource { get; private set; } = MappingSource.None;
    public string LastProfileId { get; private set; }
    public string LastError { get; private set; }

    /// <summary>Chargement synchrone Resources uniquement (éditeur / ExecuteAlways).</summary>
    public WallBandsConfig LoadWallBandsConfig()
    {
        return LoadFromResources();
    }

    /// <summary>
    /// Hub → cache → Resources. Ne bloque jamais l’authoring si le Hub est down.
    /// </summary>
    public IEnumerator LoadWallBandsConfigRoutine(Action<WallBandsConfig> onDone)
    {
        LastError = null;
        LastProfileId = null;
        LastSource = MappingSource.None;

        if (syncFromHubOnStart && Application.isPlaying)
        {
            WallBandsConfig fromHub = null;
            string hubError = null;

            yield return HubConfigClient.FetchWallBands(
                configBaseUrl,
                hubTimeoutSeconds,
                cfg => fromHub = cfg,
                err => hubError = err);

            if (fromHub != null)
            {
                WallBandsCache.Save(fromHub);
                LastSource = MappingSource.Hub;
                LastProfileId = fromHub.profile;
                Debug.Log(
                    $"[InstallationLoader] Sync Hub OK — {fromHub.columns} colonnes" +
                    (string.IsNullOrEmpty(fromHub.profile) ? "" : $", profil {fromHub.profile}") +
                    $" ({HubConfigClient.TrimSlash(configBaseUrl)})");
                onDone?.Invoke(fromHub);
                yield break;
            }

            LastError = hubError;
            Debug.LogWarning($"[InstallationLoader] Hub unreachable — {hubError}. Fallback cache/Resources.");
        }

        if (WallBandsCache.TryLoad(out var cached))
        {
            LastSource = MappingSource.Cache;
            LastProfileId = cached.profile;
            Debug.Log($"[InstallationLoader] Cache local — {cached.columns} colonnes ({WallBandsCache.CachePath})");
            onDone?.Invoke(cached);
            yield break;
        }

        var embedded = LoadFromResources();
        if (embedded != null)
            LastSource = MappingSource.Resources;

        onDone?.Invoke(embedded);
    }

    public WallBandsConfig LoadFromResources()
    {
        var textAsset = Resources.Load<TextAsset>(wallBandsResourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"[InstallationLoader] Fichier introuvable : Resources/{wallBandsResourcePath}.json");
            return null;
        }

        try
        {
            return WallBandsValidator.ParseAndValidate(textAsset.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"[InstallationLoader] {e.Message}");
            return null;
        }
    }

    public void ApplyFetchedConfig(WallBandsConfig config, MappingSource source)
    {
        if (config == null) return;
        LastSource = source;
        LastProfileId = config.profile;
        if (source == MappingSource.Hub)
            WallBandsCache.Save(config);
    }
}
