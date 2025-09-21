using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using TeamCherry.Cinematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Customizer;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Customizer : BaseUnityPlugin
{
    private static Customizer _instance = null!;
    private static readonly string ModDir = Path.Combine(Application.dataPath, "Mods", "Customizer");
    private static readonly List<tk2dSpriteCollectionData> LoadedCollectionsList = [];
    
    [HarmonyPatch(typeof(VideoPlayer), "Play")]
    private class VideoPlayerPlayPatch
    {
        static void Prefix(VideoPlayer __instance)
        {
            try
            {
                var activePack = GetActiveTexturePack();
                if (activePack == null) return;
                var cinemaDir = Path.Combine(activePack, "Cinematics");
                if (!Directory.Exists(cinemaDir)) return;

                string clipName;
                if (__instance.clip != null)
                    clipName = Path.GetFileNameWithoutExtension(__instance.clip.name);
                else if (!string.IsNullOrEmpty(__instance.url))
                    clipName = Path.GetFileNameWithoutExtension(__instance.url);
                else
                    return;

                var customFilePath = Path.Combine(cinemaDir, clipName + ".mp4");
                if (!File.Exists(customFilePath)) return;

                // Unity VideoPlayer on Windows expects a file URI and forward slashes
                var fileUrl = "file:///" + customFilePath.Replace('\\', '/');

                if (__instance.source == VideoSource.Url && string.Equals(__instance.url, fileUrl, StringComparison.OrdinalIgnoreCase))
                    return;

                __instance.source = VideoSource.Url;
                __instance.url = fileUrl;
                __instance.clip = null; // avoid clip/source conflicts
            }
            catch (Exception ex)
            {
                _instance?.Logger.LogError($"Video patch error: {ex}");
            }
        }
    }



    private static void UpdateLoadedAssets()
    {
        LoadedCollectionsList.Clear();
        foreach (var collection in Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>())
        {
            LoadedCollectionsList.Add(collection);
        }
        GetTexturePacks(LoadedCollectionsList);
    }

    private static void GetTexturePacks(List<tk2dSpriteCollectionData> collections)
    {
        var collectionDict = collections.ToDictionary(c => c.name, c => c);
        var activePack = GetActiveTexturePack();
        if (activePack == null) return;
        foreach (var dir in Directory.GetDirectories(activePack))
        {
            var dirname = Path.GetFileName(dir);
            if (!collectionDict.TryGetValue(dirname, out var collection)) continue;

            foreach (var atlas in collection.materials)
            {
                var texturePath = Path.Combine(dir, atlas.mainTexture.name + ".png");
                if (!File.Exists(texturePath)) continue;

                var pngData = File.ReadAllBytes(texturePath);
                var texture = new Texture2D(2, 2);
                if (!texture.LoadImage(pngData)) continue;
                texture.name = atlas.mainTexture.name;
                atlas.mainTexture = texture;
            }
        }
    }

    private static void GetCinematics()
    {
        var activePack = GetActiveTexturePack();
        if (activePack == null) return;
        var cinemaDir = Path.Combine(activePack, "Cinematics");
        if (!Directory.Exists(cinemaDir)) return;
        var files = Directory.GetFiles(cinemaDir)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var player in Resources.FindObjectsOfTypeAll<CinematicPlayer>())
        {
            if (player?.VideoClip == null) continue;
            var clipName = Path.GetFileNameWithoutExtension(player.VideoClip.VideoFileName);
            _instance.Logger.LogInfo($"Clipname : {clipName}");
            if (files.Contains(clipName))
            {
                _instance.Logger.LogInfo($"Match: {clipName}");
            }
        }

        foreach (var sequence in Resources.FindObjectsOfTypeAll<CinematicSequence>())
        {
            if (sequence?.VideoReference == null) continue;
            var clipName = Path.GetFileNameWithoutExtension(sequence.VideoReference.VideoFileName);
            _instance.Logger.LogInfo($"Sequence Clipname : {clipName}");
            if (!files.Contains(clipName)) continue;
            _instance.Logger.LogInfo($"Match Sequence : {clipName}");
            sequence.unityVideoPlayer.source = VideoSource.Url;
            sequence.unityVideoPlayer.url = Path.Combine(cinemaDir, sequence.VideoReference.VideoFileName);
        }
    }

    private static string? GetActiveTexturePack()
    {
        return (from output in Directory.GetDirectories(ModDir) let infoPath = Path.Combine(output, "active.txt") where File.Exists(infoPath) select output).FirstOrDefault();
    }

private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateLoadedAssets();
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static void InitializeMod()
    {
        if (!Directory.Exists(ModDir))
        {
            Directory.CreateDirectory(ModDir);
        }

        
    }
    

    private void Awake()
    {
        _instance = this;
        InitializeMod();
        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
}