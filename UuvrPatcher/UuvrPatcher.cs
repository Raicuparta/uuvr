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
            "openvr_api", "openxr_loader", "UnityOpenXR"
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

        string installerPath = Assembly.GetExecutingAssembly().Location;

        string gameExePath = Process.GetCurrentProcess().MainModule.FileName;

        string gamePath = Path.GetDirectoryName(gameExePath);
        string gameName = Path.GetFileNameWithoutExtension(gameExePath);
        string dataPath = Path.Combine(gamePath, $"{gameName}_Data/");
        string patcherPath = Path.GetDirectoryName(installerPath);
        
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
        foreach (string globalSettingsFielName in GlobalSettingsFileNames)
        {
            string path = Path.Combine(dataPath, globalSettingsFielName);
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
        string backupPath = globalSettingsFilePath + ".bak";
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
        AssetsFileInstance ggm = am.LoadAssetsFile(globalSettingsBackupPath, false);
        AssetsFile ggmFile = ggm.file;
        AssetsFileTable ggmTable = ggm.table;
        am.LoadClassDatabaseFromPackage(ggmFile.typeTree.unityVersion);

        List<AssetsReplacer> replacers = new();

        AssetFileInfoEx buildSettings = ggmTable.GetAssetInfo(11);
        AssetTypeValueField buildSettingsBase = am.GetATI(ggmFile, buildSettings).GetBaseField();
        AssetTypeValueField enabledVRDevices = buildSettingsBase.Get("enabledVRDevices").Get("Array");
        AssetTypeTemplateField stringTemplate = enabledVRDevices.templateField.children[1];
        AssetTypeValueField[] vrDevicesList = { StringField("OpenVR", stringTemplate) };
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
        string copyToGameFolderPath = Path.Combine(patcherPath, "CopyToGame");

        Console.WriteLine($"Copying mod files to game... These files get overwritten every time the game starts. If you want to change them manually, replace them in the mod folder instead: {copyToGameFolderPath}");

        CopyDirectory(Path.Combine(copyToGameFolderPath, "Data"), dataPath);

        string gamePluginsPath = Path.Combine(dataPath, "Plugins");
        string uuvrPluginsPath = Path.Combine(copyToGameFolderPath, "Plugins");

        DeleteExistingVrPlugins(gamePluginsPath);

        // IntPtr size is 4 on x86, 8 on x64.
        bool is64Bit = IntPtr.Size == 8;
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
        IEnumerable<string> pluginPaths = Directory
            .GetFiles(gamePluginsPath, "*.dll", SearchOption.AllDirectories)
            .Where(pluginPath => PluginsToDeleteBeforePatch
                .Select(pluginToDelete => $"{pluginToDelete.ToLower()}.dll")
                .Contains(Path.GetFileName(pluginPath).ToLower()));

        Console.WriteLine($"### Found {pluginPaths.Count()} plugins");

        foreach (string pluginPath in pluginPaths)
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

        DirectoryInfo[] dirs = dir.GetDirectories();

        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }

        Console.WriteLine($"Copied files from:\n> {sourceDir}\nto:\n> {destinationDir}");
    }

#if CPP
    public override void Finalizer() { }
#endif
}
