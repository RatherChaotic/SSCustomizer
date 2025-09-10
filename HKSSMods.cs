using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace HKSSMods;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class HKSSMods : BaseUnityPlugin
{
    internal static HKSSMods Instance;
    
    [HarmonyPatch(typeof(HeroController), "Start")]
    class HeroControllerPrePatch
    {
        private static void Prefix(HeroController __instance)
        {
            var gameObject = __instance.gameObject;
            var sprite = gameObject.GetComponent(typeof(tk2dSprite)) as tk2dSprite;
            if (sprite != null)
            {
                string pngPath = @"D:\Projects\HKSSMods\skinchanger\assets\texture\atlas3.png";
                if (File.Exists(pngPath))
                {
                    byte[] pngData = File.ReadAllBytes(pngPath);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(pngData))
                    { 
                        sprite.Collection.materials[3].mainTexture = texture;
                    }
                }
            }
        }
    }

    private void Awake()
    {
        Instance = this;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} has loaded!");
        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}