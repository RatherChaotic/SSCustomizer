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
    private static readonly List<tk2dSpriteCollectionData> LoadedCollectionsList = new();
    
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
            var infoPath = Path.Combine(output, "active.txt");
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
                    if (!texture.LoadImage(pngData)) continue;
                    texture.name = atlas.mainTexture.name;
                    atlas.mainTexture = texture;
                }
            }
        }
    }

private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateLoadedAssets();
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
        //_instance.Logger.LogInfo("Customizer Loaded : DEBUG");
        InitializeMod();
        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
}