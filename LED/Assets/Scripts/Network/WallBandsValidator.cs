// Validation / parse wall-bands (contrat Hub ↔ authoring).

using System;
using UnityEngine;

public static class WallBandsValidator
{
    public static WallBandsConfig ParseAndValidate(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new Exception("JSON wall-bands vide");

        WallBandsConfig config;
        try
        {
            config = JsonUtility.FromJson<WallBandsConfig>(json);
        }
        catch (Exception e)
        {
            throw new Exception($"JSON wall-bands invalide : {e.Message}");
        }

        var errors = Validate(config);
        if (errors.Length > 0)
            throw new Exception(string.Join("; ", errors));

        return config;
    }

    public static string[] Validate(WallBandsConfig config)
    {
        if (config == null)
            return new[] { "config null" };
        if (config.bands == null || config.bands.Length == 0)
            return new[] { "bands vide" };
        if (config.columns <= 0)
            return new[] { "columns <= 0" };
        if (config.columns != config.bands.Length)
            return new[] { $"columns ({config.columns}) ≠ bands.Length ({config.bands.Length})" };

        for (int i = 0; i < config.bands.Length; i++)
        {
            var b = config.bands[i];
            if (b == null)
                return new[] { $"band[{i}] null" };
            if (b.entityStart < 1)
                return new[] { $"band[{i}].entityStart invalide" };
            if (b.entityCount < 1)
                return new[] { $"band[{i}].entityCount invalide" };
        }

        return Array.Empty<string>();
    }

    public static string ToJson(WallBandsConfig config)
    {
        return JsonUtility.ToJson(config, true);
    }
}
