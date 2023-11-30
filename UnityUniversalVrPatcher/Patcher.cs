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
    public static IEnumerable<string> TargetDLLs { get; } = new[] {"Assembly-CSharp.dll"};

    public static void Patch(AssemblyDefinition assembly)
    {
    }

    public static void Initialize()
    {
        Console.WriteLine("Patching Unity Universal VR...");
        
        var installerPath = Assembly.GetExecutingAssembly().Location;
        Console.WriteLine("installerPath " + installerPath);

        var gameExePath = Process.GetCurrentProcess().MainModule.FileName;
        Console.WriteLine("gameExePath " + gameExePath);

        var gamePath = Path.GetDirectoryName(gameExePath);
        var gameName = Path.GetFileNameWithoutExtension(gameExePath);
        var dataPath = Path.Combine(gamePath, $"{gameName}_Data/");
        var gameManagersPath = Path.Combine(dataPath, $"globalgamemanagers");
        var gameManagersBackupPath = CreateGameManagersBackup(gameManagersPath);
        var patcherPath = Path.GetDirectoryName(installerPath);
        var classDataPath = Path.Combine(patcherPath, "classdata.tpk");

        CopyPlugins(patcherPath, dataPath);
        PatchVR(gameManagersBackupPath, gameManagersPath, classDataPath);

        Console.WriteLine($"");
        Console.WriteLine("Installed successfully, probably.");
    }

    private static string CreateGameManagersBackup(string gameManagersPath)
    {
        Console.WriteLine($"Backing up '{gameManagersPath}'...");
        var backupPath = gameManagersPath + ".bak";
        if (File.Exists(backupPath))
        {
            Console.WriteLine($"Backup already exists.");
            return backupPath;
        }

        File.Copy(gameManagersPath, backupPath);
        Console.WriteLine($"Created backup in '{backupPath}'");
        return backupPath;
    }

    private static void PatchVR(string gameManagersBackupPath, string gameManagersPath, string classDataPath)
    {
        Console.WriteLine($"Using classData file from path '{classDataPath}'");

        var am = new AssetsManager();
        am.LoadClassPackage(classDataPath);
        var ggm = am.LoadAssetsFile(gameManagersBackupPath, false);
        var ggmFile = ggm.file;
        var ggmTable = ggm.table;
        am.LoadClassDatabaseFromPackage(ggmFile.typeTree.unityVersion);

        var replacers = new List<AssetsReplacer>();

        var buildSettings = ggmTable.GetAssetInfo(11);
        var buildSettingsBase = am.GetATI(ggmFile, buildSettings).GetBaseField();
        var enabledVRDevices = buildSettingsBase.Get("enabledVRDevices").Get("Array");
        var stringTemplate = enabledVRDevices.templateField.children[1];
        var vrDevicesList = new[] {StringField("OpenVR", stringTemplate)};
        enabledVRDevices.SetChildrenList(vrDevicesList);

        replacers.Add(new AssetsReplacerFromMemory(0, buildSettings.index, (int) buildSettings.curFileType, 0xffff,
            buildSettingsBase.WriteToByteArray()));

        using (var writer = new AssetsFileWriter(File.OpenWrite(gameManagersPath)))
        {
            ggmFile.Write(writer, 0, replacers, 0);
        }
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
    
    static void CopyPlugins(string patcherPath, string dataPath)
    {
        Console.WriteLine("Copying plugins...");

        var gamePluginsPath = Path.Combine(dataPath, "Plugins");
        if (!Directory.Exists(gamePluginsPath))
        {
            Directory.CreateDirectory(gamePluginsPath);
        }
        var patcherPluginsPath = Path.Combine(patcherPath, "GamePlugins");
        var pluginFilePaths = Directory.GetFiles(patcherPluginsPath);
        Console.WriteLine($"Found plugins:\n {string.Join(",\n", pluginFilePaths)}");
        foreach (var filePath in pluginFilePaths)
        {
            File.Copy(filePath, Path.Combine(gamePluginsPath, Path.GetFileName(filePath)), true);
        }
    }
}
