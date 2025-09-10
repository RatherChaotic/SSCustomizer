using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SSCustomizer;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class SSCustomizer : BaseUnityPlugin
{
    private static SSCustomizer _instance = null!;
    private static readonly string ModDir = Path.Combine(Application.dataPath, "Mods", "Customizer");
    internal static readonly string[] validTextures = ["atlas0.png", "atlas1.png", "atlas2.png", "atlas3.png"];
    
    [HarmonyPatch(typeof(HeroController), "Start")]
    class HeroControllerPrePatch
    {
        private static void Prefix(HeroController __instance)
        {
            var gameObject = __instance.gameObject;
            var sprite = gameObject.GetComponent(typeof(tk2dSprite)) as tk2dSprite;
            if (sprite != null) GetTexturePacks(sprite.Collection);
        }
    }

    private static void UpdateAsset(tk2dSpriteCollectionData spriteCollectionData, String pngfile, int index)
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

    private static void GetTexturePacks(tk2dSpriteCollectionData spriteCollectionData)
    {
        foreach (var output in Directory.GetDirectories(ModDir))
        {
            if (File.Exists(output + "/info.json"))
            {
                _instance.Logger.LogInfo($"Found texture pack: {Path.Combine(ModDir, output)}");
                foreach (var file in (Directory.GetFiles(Path.Combine(ModDir, output))))
                {
                    if (!Array.Exists(validTextures, texture => texture == Path.GetFileName(file))) continue;
                    _instance.Logger.LogInfo($"Found texture: {Path.GetFileNameWithoutExtension(file)[5]}");
                    UpdateAsset(spriteCollectionData, file, Path.GetFileNameWithoutExtension(file)[5] - '0');
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
        InitializeMod();
        _instance = this;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} has loaded!");
        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}