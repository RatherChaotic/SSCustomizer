using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SSCustomizer;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class SSCustomizer : BaseUnityPlugin
{
    private static SSCustomizer _instance = null!;
    private static readonly string ModDir = Path.Combine(Application.dataPath, "Mods", "Customizer");
    private static readonly List<tk2dSpriteCollectionData> LoadedCollectionsList = new List<tk2dSpriteCollectionData>();
    

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

        foreach (var output in Directory.GetDirectories(ModDir))
        {
            var infoPath = Path.Combine(output, "info.json");
            if (!File.Exists(infoPath)) continue;

            foreach (var dir in Directory.GetDirectories(output))
            {
                var dirname = Path.GetFileName(dir);
                if (!collectionDict.TryGetValue(dirname, out var collection)) continue;

                foreach (var atlas in collection.materials)
                {
                    var texturePath = Path.Combine(dir, atlas.mainTexture.name + ".png");
                    if (!File.Exists(texturePath)) continue;

                    var pngData = File.ReadAllBytes(texturePath);
                    var texture = new Texture2D(2, 2);
                    if (texture.LoadImage(pngData))
                    {
                        atlas.mainTexture = texture;
                    }
                }
            }
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateLoadedAssets();
        _instance.Logger.LogInfo($"Loaded scene: {scene.name}");
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeMod()
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
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} has loaded!");
        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
}