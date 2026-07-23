#if UNITY_EDITOR
// Menu : LED > Import GIF or Video as Wall Media…
// Extrait les frames (ffmpeg) → PNG à la résolution du mur (WallMapping) + WallMediaSequence.

using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GifToWallMediaImporter
{
    const string DefaultOutRoot = "Assets/Media";

    [MenuItem("LED/Import GIF or Video as Wall Media…")]
    public static void ImportGifDialog()
    {
        string mediaPath = EditorUtility.OpenFilePanel(
            "Choisir un GIF ou une vidéo",
            Application.dataPath,
            "gif,mp4,mov,webm");
        if (string.IsNullOrEmpty(mediaPath)) return;
        ImportMedia(mediaPath, showDialogs: true);
    }

    [MenuItem("LED/Import Samus 8-Bit GIF (Media/Samus8Bit)")]
    public static void ImportBundledSamus()
    {
        string gif = Path.Combine(Application.dataPath, "Media/Samus8Bit/source.gif");
        if (!File.Exists(gif))
        {
            EditorUtility.DisplayDialog(
                "GIF manquant",
                "Place le fichier dans Assets/Media/Samus8Bit/source.gif",
                "OK");
            return;
        }
        ImportMedia(gif, "Samus8Bit", showDialogs: true);
    }

    public static void ImportGif(string absoluteGifPath, string folderName = null)
        => ImportMedia(absoluteGifPath, folderName, showDialogs: true);

    /// <summary>Import silencieux (automation). Retourne un message de statut.</summary>
    public static string ImportMedia(string absoluteMediaPath, string folderName = null, bool showDialogs = false)
    {
        if (!File.Exists(absoluteMediaPath))
        {
            string msg = "Fichier introuvable : " + absoluteMediaPath;
            if (showDialogs) EditorUtility.DisplayDialog("Erreur", msg, "OK");
            return msg;
        }

        string ffmpeg = FindFfmpeg();
        if (ffmpeg == null)
        {
            string msg = "ffmpeg requis (brew install ffmpeg)";
            if (showDialogs) EditorUtility.DisplayDialog("ffmpeg requis", msg, "OK");
            return msg;
        }

        if (string.IsNullOrEmpty(folderName))
            folderName = Path.GetFileNameWithoutExtension(absoluteMediaPath)
                .Replace(" ", "_")
                .Replace(".", "_");

        string ext = Path.GetExtension(absoluteMediaPath).ToLowerInvariant();
        string sourceName = ext == ".gif" ? "source.gif" : "source" + (string.IsNullOrEmpty(ext) ? ".mp4" : ext);

        string relDir = $"{DefaultOutRoot}/{folderName}";
        string absDir = Path.Combine(Application.dataPath, "Media", folderName);
        Directory.CreateDirectory(absDir);

        string destMedia = Path.Combine(absDir, sourceName);
        if (Path.GetFullPath(absoluteMediaPath) != Path.GetFullPath(destMedia))
            File.Copy(absoluteMediaPath, destMedia, true);

        foreach (var old in Directory.GetFiles(absDir, "frame_*.png"))
            File.Delete(old);

        int cols = WallMapping.IsInitialized && WallMapping.Columns > 0
            ? WallMapping.Columns
            : 128;
        int rows = WallMapping.IsInitialized && WallMapping.VisibleRows > 0
            ? WallMapping.VisibleRows
            : 128;

        // Pixel art : neighbor. Vidéo : bilinear (flags=bilinear) pour éviter le crénelage.
        string scaleFlags = ext == ".gif" ? "neighbor" : "bilinear";
        string pattern = Path.Combine(absDir, "frame_%02d.png");
        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = $"-y -i \"{destMedia}\" -vf \"scale={cols}:{rows}:flags={scaleFlags}\" \"{pattern}\"",
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using (var p = Process.Start(psi))
        {
            p.WaitForExit(120000);
            if (p.ExitCode != 0)
            {
                string err = p.StandardError.ReadToEnd();
                if (showDialogs) EditorUtility.DisplayDialog("ffmpeg échoué", err, "OK");
                return "ffmpeg échoué: " + err;
            }
        }

        AssetDatabase.Refresh();

        string[] frameGuids = AssetDatabase.FindAssets("frame_ t:Texture2D", new[] { relDir });
        System.Array.Sort(frameGuids, (a, b) =>
            string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));

        var textures = new System.Collections.Generic.List<Texture2D>();
        foreach (string guid in frameGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileName(path).StartsWith("frame_")) continue;
            EnsurePointFilterReadable(path);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) textures.Add(tex);
        }

        if (textures.Count == 0)
        {
            string msg = "Aucune frame importée.";
            if (showDialogs) EditorUtility.DisplayDialog("Erreur", msg, "OK");
            return msg;
        }

        string seqPath = $"{relDir}/{folderName}_Sequence.asset";
        var seq = AssetDatabase.LoadAssetAtPath<WallMediaSequence>(seqPath);
        if (seq == null)
        {
            seq = ScriptableObject.CreateInstance<WallMediaSequence>();
            AssetDatabase.CreateAsset(seq, seqPath);
        }

        seq.frames = textures.ToArray();
        // GIF 8-bit ~12 fps ; vidéo → 24 fps par défaut (ajustable sur l'asset)
        seq.framesPerSecond = ext == ".gif" ? 12f : 24f;
        seq.loop = true;
        EditorUtility.SetDirty(seq);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string ok = $"{textures.Count} frames → {seqPath} ({cols}×{rows})";
        if (showDialogs)
        {
            EditorUtility.DisplayDialog(
                "Wall Media OK",
                ok + "\n\nTimeline → Add Wall Media Track → binder LedWall → Add Clip → assigner la Sequence.",
                "OK");
            Selection.activeObject = seq;
        }

        return ok;
    }

    static void EnsurePointFilterReadable(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        bool dirty = false;
        if (!importer.isReadable) { importer.isReadable = true; dirty = true; }
        if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            dirty = true;
        }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
        if (dirty) importer.SaveAndReimport();
    }

    static string FindFfmpeg()
    {
        string[] candidates =
        {
            "/opt/homebrew/bin/ffmpeg",
            "/usr/local/bin/ffmpeg",
            "ffmpeg",
        };
        foreach (string c in candidates)
        {
            if (c == "ffmpeg") return c;
            if (File.Exists(c)) return c;
        }
        return null;
    }
}
#endif
