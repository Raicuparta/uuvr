using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

public static class Patcher
{
    private static readonly List<string> GlobalSettingsFileNames =
        new()
        {
            "globalgamemanagers", "mainData", "data.unity3d"
        };
    
    public static IEnumerable<string> TargetDLLs { get; } = new[] {"Assembly-CSharp.dll"};

    public static void Patch(AssemblyDefinition assembly)
    {
    }

    public static void Initialize()
    {
        Console.WriteLine("Patching Unity Universal VR...");
        
        string installerPath = Assembly.GetExecutingAssembly().Location;
        Console.WriteLine("installerPath " + installerPath);

        string gameExePath = Process.GetCurrentProcess().MainModule.FileName;
        Console.WriteLine("gameExePath " + gameExePath);

        string gamePath = Path.GetDirectoryName(gameExePath);
        string gameName = Path.GetFileNameWithoutExtension(gameExePath);
        string dataPath = Path.Combine(gamePath, $"{gameName}_Data/");
        
        string globalSettingsFilePath = GetGlobalSettingsFilePath(dataPath);
        string globalSettingsBackupPath = CreateGlobalSettingsBackup(globalSettingsFilePath);
        string patcherPath = Path.GetDirectoryName(installerPath);
        string classDataPath = Path.Combine(patcherPath, "classdata.tpk");

        CopyPlugins(patcherPath, dataPath);
        PatchVR(globalSettingsBackupPath, globalSettingsFilePath, classDataPath);

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
        AssetTypeValueField[] vrDevicesList = {StringField("OpenVR", stringTemplate)};
        enabledVRDevices.SetChildrenList(vrDevicesList);

        replacers.Add(new AssetsReplacerFromMemory(0, buildSettings.index, (int) buildSettings.curFileType, 0xffff,
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

    private static void CopyPlugins(string patcherPath, string dataPath)
    {
        Console.WriteLine("Copying plugins...");

        string gamePluginsPath = Path.Combine(dataPath, "Plugins");
        if (!Directory.Exists(gamePluginsPath))
        {
            Directory.CreateDirectory(gamePluginsPath);
        }
        string patcherPluginsPath = Path.Combine(patcherPath, "GamePlugins");
        string[] pluginFilePaths = Directory.GetFiles(patcherPluginsPath);
        Console.WriteLine($"Found plugins:\n {string.Join(",\n", pluginFilePaths)}");
        foreach (string filePath in pluginFilePaths)
        {
            File.Copy(filePath, Path.Combine(gamePluginsPath, Path.GetFileName(filePath)), true);
        }
    }
}
