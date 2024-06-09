using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
#if CPP
using BepInEx.Preloader.Core.Patching;
#elif MONO
using Mono.Cecil;
#endif

#if CPP
[PatcherPluginInfo("com.raicuparta.uuvr", "UUVR", "0.1.0")]
#endif
public class Patcher
#if CPP
    : BasePatcher
#endif
{
    private static readonly List<string> GlobalSettingsFileNames =
        new()
        {
            "globalgamemanagers", "mainData", "data.unity3d"
        };

    private static readonly List<string> PluginsToDeleteBeforePatch =
        new()
        {
            "openvr_api", "openxr_loader", "UnityOpenXR", "ucrtbased.dll", "XRSDKOpenVR"
        };

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

#if MONO
    public static void Patch(AssemblyDefinition assembly)
    {
    }
#endif

#if CPP
    public override void Initialize()
#elif MONO
    public static void Initialize()
#endif
    {
        Console.WriteLine("Patching UUVR...");

        var installerPath = Assembly.GetExecutingAssembly().Location;

        var gameExePath = Process.GetCurrentProcess().MainModule.FileName;

        var gamePath = Path.GetDirectoryName(gameExePath);
        var gameName = Path.GetFileNameWithoutExtension(gameExePath);
        var dataPath = Path.Combine(gamePath, $"{gameName}_Data/");
        var patcherPath = Path.GetDirectoryName(installerPath);
        
        CopyFilesToGame(patcherPath, dataPath);

#if LEGACY
        string globalSettingsFilePath = GetGlobalSettingsFilePath(dataPath);
        string globalSettingsBackupPath = CreateGlobalSettingsBackup(globalSettingsFilePath);
        string classDataPath = Path.Combine(patcherPath, "classdata.tpk");
        PatchVR(globalSettingsBackupPath, globalSettingsFilePath, classDataPath);
#endif

        Console.WriteLine("");
        Console.WriteLine("Installed successfully, probably.");
    }

    private static string GetGlobalSettingsFilePath(string dataPath)
    {
        foreach (var globalSettingsFielName in GlobalSettingsFileNames)
        {
            var path = Path.Combine(dataPath, globalSettingsFielName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new Exception("Failed to find global settings file path");
    }

    private static string CreateGlobalSettingsBackup(string globalSettingsFilePath)
    {
        Console.WriteLine($"Backing up '{globalSettingsFilePath}'...");
        var backupPath = globalSettingsFilePath + ".bak";
        if (File.Exists(backupPath))
        {
            Console.WriteLine($"Backup already exists.");
            return backupPath;
        }

        File.Copy(globalSettingsFilePath, backupPath);
        Console.WriteLine($"Created backup in '{backupPath}'");
        return backupPath;
    }

    private static void PatchVR(string globalSettingsBackupPath, string globalSettingsFilePath, string classDataPath)
    {
        Console.WriteLine($"Using classData file from path '{classDataPath}'");

        AssetsManager am = new();
        am.LoadClassPackage(classDataPath);
        var ggm = am.LoadAssetsFile(globalSettingsBackupPath, false);
        var ggmFile = ggm.file;
        var ggmTable = ggm.table;
        am.LoadClassDatabaseFromPackage(ggmFile.typeTree.unityVersion);

        List<AssetsReplacer> replacers = new();
        
        // TODO: Read inputs from globalgamemanagers, store map somewhere, patch in-game?
        // AssetFileInfoEx inputManager = ggmTable.GetAssetInfo(2);
        // AssetTypeValueField inputManagerBase = am.GetATI(ggmFile, inputManager).GetBaseField();
        // AssetTypeValueField axes = inputManagerBase.Get("m_Axes").Get("Array");
        // Console.WriteLine($"#### Found axes: {axes.children.Length}, looping...");
        //
        // foreach (AssetTypeValueField? child in axes.children)
        // {
        //     int axis = child.Get("axis").value.AsInt();
        //     int type = child.Get("type").value.AsInt();
        //     int joyNum = child.Get("joyNum").value.AsInt();
        //     string? name = child.Get("m_Name").value.AsString();
        //     string? positiveButton = child.Get("positiveButton").value.AsString();
        //     string? negativeButton = child.Get("negativeButton").value.AsString();
        //     string? altNegativeButton = child.Get("altNegativeButton").value.AsString();
        //     string? altPositiveButton = child.Get("altPositiveButton").value.AsString();
        //     float gravity = child.Get("gravity").value.AsFloat();
        //     float dead = child.Get("dead").value.AsFloat();
        //     float sensitivity = child.Get("sensitivity").value.AsFloat();
        //     bool snap = child.Get("snap").value.AsBool();
        //     bool invert = child.Get("invert").value.AsBool();
        //
        //     if (string.IsNullOrEmpty(positiveButton)) continue;
        //
        //     if (!positiveButton.StartsWith("joystick")) continue;
        //
        //     Console.WriteLine($"name:{name} | positiveButton:{positiveButton} ");
        // }
        
        var buildSettings = ggmTable.GetAssetInfo(11);
        #pragma warning disable CS0618 // Type or member is obsolete
        var buildSettingsBase = am.GetATI(ggmFile, buildSettings).GetBaseField();
        #pragma warning restore CS0618 // Type or member is obsolete
        var enabledVRDevices = buildSettingsBase.Get("enabledVRDevices").Get("Array");
        var stringTemplate = enabledVRDevices.templateField.children[1];
        
        // AssetTypeValueField[] vrDevicesList = { StringField("None", stringTemplate), StringField("OpenVR", stringTemplate), StringField("Oculus", stringTemplate) };
        AssetTypeValueField[] vrDevicesList = { StringField("OpenVR", stringTemplate), StringField("Oculus", stringTemplate) };
        enabledVRDevices.SetChildrenList(vrDevicesList);

        replacers.Add(new AssetsReplacerFromMemory(0, buildSettings.index, (int)buildSettings.curFileType, 0xffff,
            buildSettingsBase.WriteToByteArray()));

        using AssetsFileWriter writer = new(File.OpenWrite(globalSettingsFilePath));
        ggmFile.Write(writer, 0, replacers, 0);
    }

    private static AssetTypeValueField StringField(string str, AssetTypeTemplateField template)
    {
        return new AssetTypeValueField()
        {
            children = null,
            childrenCount = 0,
            templateField = template,
            value = new AssetTypeValue(EnumValueTypes.ValueType_String, str)
        };
    }

    private static void CopyFilesToGame(string patcherPath, string dataPath)
    {
        var copyToGameFolderPath = Path.Combine(patcherPath, "CopyToGame");

        Console.WriteLine($"Copying mod files to game... These files get overwritten every time the game starts. If you want to change them manually, replace them in the mod folder instead: {copyToGameFolderPath}");

        CopyDirectory(Path.Combine(copyToGameFolderPath, "Data"), dataPath);

        var gamePluginsPath = Path.Combine(dataPath, "Plugins");
        Directory.CreateDirectory(gamePluginsPath);

        var uuvrPluginsPath = Path.Combine(copyToGameFolderPath, "Plugins");

        DeleteExistingVrPlugins(gamePluginsPath);

        // IntPtr size is 4 on x86, 8 on x64.
        var is64Bit = IntPtr.Size == 8;
        Console.WriteLine($"Detected game as being {(is64Bit ? "x64" : "x86")}");

        // Unity plugins are often in a subfolder of the Plugins folder, but they also get detected from the root folder,
        // so we don't need to worry about the subfolders.
        CopyDirectory(is64Bit ? Path.Combine(uuvrPluginsPath, "x64") : Path.Combine(uuvrPluginsPath, "x86"), gamePluginsPath);
    }

    // There might be leftover stuff from previous UUVR versions, or from other filthy VR mods,
    // and they might be in different subfolders, which could cause conflicts.
    // So we should make sure to nuke them all before replacing with our own.
    private static void DeleteExistingVrPlugins(string gamePluginsPath)
    {
        var pluginPaths = Directory
            .GetFiles(gamePluginsPath, "*.dll", SearchOption.AllDirectories)
            .Where(pluginPath => PluginsToDeleteBeforePatch
                .Select(pluginToDelete => $"{pluginToDelete.ToLower()}.dll")
                .Contains(Path.GetFileName(pluginPath).ToLower()));

        Console.WriteLine($"### Found {pluginPaths.Count()} plugins");

        foreach (var pluginPath in pluginPaths)
        {
            try
            {
                Console.WriteLine($"Deleting plugin `{pluginPath}`");
                File.Delete(pluginPath);
            } catch (Exception exception)
            {
                Console.WriteLine($"Failed to delete plugin before patching. Path: `{pluginPath}`. Exception: `{exception}`");
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        DirectoryInfo dir = new(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        var dirs = dir.GetDirectories();

        Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }

        Console.WriteLine($"Copied files from:\n> {sourceDir}\nto:\n> {destinationDir}");
    }

#if CPP
    public override void Finalizer() { }
#endif
}
