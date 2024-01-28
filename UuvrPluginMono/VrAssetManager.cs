using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace ShipbreakerVr;

public static class VrAssetManager
{
    private const string AssetsDir = "ShipbreakerVr/AssetBundles";

    public static AssetBundle LoadBundle(string assetName)
    {
        Debug.Log($"loading bundle {assetName} in {Paths.PluginPath}...");
        var bundle = AssetBundle.LoadFromFile(Path.Combine(Paths.PluginPath, Path.Combine(AssetsDir, assetName)));

        if (bundle == null) throw new Exception("Failed to load asset bundle " + assetName);

        Debug.Log($"Loaded bundle {bundle.name}");

        return bundle;
    }
}
