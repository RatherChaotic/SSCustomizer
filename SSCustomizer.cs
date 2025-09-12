using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SSCustomizer;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class SSCustomizer : BaseUnityPlugin
{
    private static SSCustomizer _instance = null!;
    private static readonly string ModDir = Path.Combine(Application.dataPath, "Mods", "Customizer");
    private static readonly List<tk2dSpriteCollectionData> LoadedCollectionsList = new List<tk2dSpriteCollectionData>();
    
    [HarmonyPatch(typeof(HeroController), "Start")]
    class HeroControllerPrePatch
    {
        private static void Prefix(HeroController __instance)
        {
            var gameObject = __instance.gameObject;
            _instance.Logger.LogInfo("HeroController Start Detected");
            var sprite = gameObject.GetComponent(typeof(tk2dSprite)) as tk2dSprite;
            var collections = Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>();
            UpdateLoadedAssets();
        }
    }

    private static void UpdateAsset(tk2dSpriteCollectionData spriteCollectionData, String pngfile)
    {
        if (spriteCollectionData != null)
        {
            if (File.Exists(pngfile))
            {
                byte[] pngData = File.ReadAllBytes(pngfile);
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(pngData))
                {
                    spriteCollectionData.materials[0].mainTexture = texture;
                }
            }
        }
    }
    private static void UpdateMultiAsset(tk2dSpriteCollectionData spriteCollectionData, String pngfile, int index)
    {
        if (spriteCollectionData != null)
        {
            if (File.Exists(pngfile))
            {
                byte[] pngData = File.ReadAllBytes(pngfile);
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(pngData))
                {
                    spriteCollectionData.materials[index].mainTexture = texture;
                }
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
        foreach (var output in Directory.GetDirectories(ModDir))
        {
            if (!File.Exists(output + "/info.json")) continue;
            foreach (var dir in Directory.GetDirectories(Path.Combine(ModDir, output)))
            {
                var dirname = dir[(dir.LastIndexOf("\\", StringComparison.Ordinal) + 1)..];
                if (collections.All(collection => collection.name != dirname)) continue;
                {
                    var collection = collections.Find(collection => collection.name == dirname);
                    foreach (var atlas in collection.materials)
                    {
                        if (!File.Exists(Path.Combine(ModDir, output, dirname, atlas.mainTexture.name + ".png")))
                            continue;
                        var pngData = File.ReadAllBytes(Path.Combine(ModDir, output, dirname, atlas.mainTexture.name + ".png"));
                        var texture = new Texture2D(2, 2);
                        if (texture.LoadImage(pngData))
                        {
                            atlas.mainTexture = texture;
                        }
                    }
                }
            }
        }
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
    }
}