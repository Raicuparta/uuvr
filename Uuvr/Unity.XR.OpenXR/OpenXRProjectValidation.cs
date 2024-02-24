#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;


namespace UnityEditor.XR.OpenXR
{
    /// <summary>
    /// OpenXR project validation details.
    /// </summary>
    public static class OpenXRProjectValidation
    {
        private static readonly OpenXRFeature.ValidationRule[] BuiltinValidationRules =
        {
            new OpenXRFeature.ValidationRule
            {
                message = "The OpenXR package has been updated and Unity must be restarted to complete the update.",
                checkPredicate = () => (!OpenXRSettings.Instance.versionChanged),
                fixIt = RequireRestart,
                error = true,
                errorEnteringPlaymode = true,
                buildTargetGroup = BuildTargetGroup.Standalone,
            },

            new OpenXRFeature.ValidationRule()
            {
                message = "Gamma Color Space is not supported when using OpenGLES.",
                checkPredicate = () =>
                {
                    if (PlayerSettings.colorSpace == ColorSpace.Linear)
                        return true;

                    return !Enum.GetValues(typeof(BuildTarget))
                        .Cast<BuildTarget>()
                        .Where(t =>
                        {
                            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(t);
                            if(!BuildPipeline.IsBuildTargetSupported(buildTargetGroup, t))
                                return false;

                            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
                            if(null == settings)
                                return false;

                            var manager = settings.Manager;
                            if(null == manager)
                                return false;

                            return manager.activeLoaders.OfType<OpenXRLoader>().Any();
                        })
                        .Any(buildTarget => PlayerSettings.GetGraphicsAPIs(buildTarget).Any(g => g == GraphicsDeviceType.OpenGLES2 || g == GraphicsDeviceType.OpenGLES3));
                },
                fixIt = () => PlayerSettings.colorSpace = ColorSpace.Linear,
                fixItMessage = "Set PlayerSettings.colorSpace to ColorSpace.Linear",
                error = true,
                errorEnteringPlaymode = true,
                buildTargetGroup = BuildTargetGroup.Android,
            },
            new OpenXRFeature.ValidationRule()
            {
                message = "At least one interaction profile must be added.  Please select which controllers you will be testing against in the Features menu.",
                checkPredicate = () => OpenXRSettings.GetSettingsForBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup).GetFeatures<OpenXRInteractionFeature>().Any(f => f.enabled),
                fixIt = OpenProjectSettings,
                fixItAutomatic = false,
                fixItMessage = "Open Project Settings to select one or more interaction profiles."
            },
            new OpenXRFeature.ValidationRule()
            {
                message = "Only arm64 is supported on Android with OpenXR.  Other architectures are not supported.",
                checkPredicate = () => (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) || (PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64),
                fixIt = () =>
                {
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                },
                fixItMessage = "Change android build to arm64 and enable il2cpp.",
                error = true,
                buildTargetGroup = BuildTargetGroup.Android,
            },
            new OpenXRFeature.ValidationRule()
            {
                message = "The only standalone target supported is Windows x64 with OpenXR.  Other architectures and operating systems are not supported at this time.",
                checkPredicate = () => (BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) != BuildTargetGroup.Standalone) || (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64),
                fixIt = () => EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64),
                fixItMessage = "Switch active build target to StandaloneWindows64.",
                error = true,
                errorEnteringPlaymode = true,
                buildTargetGroup = BuildTargetGroup.Standalone,
            },
            new OpenXRFeature.ValidationRule()
            {
                message = "Lock Input to Game View in order for tracked pose driver to work in editor playmode.",
                checkPredicate = () =>
                {
                    var cls = typeof(UnityEngine.InputSystem.InputDevice).Assembly.GetType("UnityEngine.InputSystem.Editor.InputEditorUserSettings");
                    if (cls == null) return true;
                    var prop = cls.GetProperty("lockInputToGameView", BindingFlags.Static | BindingFlags.Public);
                    if (prop == null) return true;
                    return (bool)prop.GetValue(null);
                },
                fixItMessage =  "Enables the 'Lock Input to Game View' setting in Window -> Analysis -> Input Debugger -> Options",
                fixIt = () =>
                {
                    var cls = typeof(UnityEngine.InputSystem.InputDevice).Assembly.GetType("UnityEngine.InputSystem.Editor.InputEditorUserSettings");
                    if (cls == null) return;
                    var prop = cls.GetProperty("lockInputToGameView", BindingFlags.Static | BindingFlags.Public);
                    if (prop == null) return;
                    prop.SetValue(null, true);
                },
                errorEnteringPlaymode = true,
                buildTargetGroup = BuildTargetGroup.Standalone,
            },
            new OpenXRFeature.ValidationRule()
            {
                message = "Active Input Handling must be set to Input System Package (New) for OpenXR.",
                checkPredicate = () =>
                {
                    // There is no public way to check if the input handling backend is set correctly .. so resorting to non-public way for now.
                    var ps = (SerializedObject) typeof(PlayerSettings).GetMethod("GetSerializedObject", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
                    var newInputEnabledProp = ps?.FindProperty("activeInputHandler");
                    return newInputEnabledProp?.intValue != 0;
                },
                fixIt = () =>
                {
                    var ps = (SerializedObject) typeof(PlayerSettings).GetMethod("GetSerializedObject", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
                    if (ps == null)
                        return;

                    ps.Update();
                    var newInputEnabledProp = ps.FindProperty("activeInputHandler");
                    if (newInputEnabledProp != null)
                        newInputEnabledProp.intValue = 1;
                    ps.ApplyModifiedProperties();

                    RequireRestart();
                },
                error = true,
                errorEnteringPlaymode = true,
            },
            new OpenXRFeature.ValidationRule()
            {
                message = "If targeting HoloLens V2 devices either Run In Background should be enabled or you should install the Microsoft Mixed Reality OpenXR Plug-in and enable the Mixed Reality Features group.",
                checkPredicate = () =>
                {
#if MICROSOFT_OPENXR_PACKAGE
                    var hololensFeatures = OpenXRSettings.ActiveBuildTargetInstance.GetFeatures<OpenXRFeature>();
                    foreach (var hlf in hololensFeatures)
                    {
                        if (String.CompareOrdinal(hlf.featureIdInternal, "com.microsoft.openxr.feature.hololens") == 0)
                            return hlf.enabled;
                    }
#endif //MICROSOFT_OPENXR_PACKAGE

                    return EditorUserBuildSettings.activeBuildTarget != BuildTarget.WSAPlayer || PlayerSettings.runInBackground;
                },
                fixIt = () =>
                {
#if MICROSOFT_OPENXR_PACKAGE
                    var hololensFeatures = OpenXRSettings.ActiveBuildTargetInstance.GetFeatures<OpenXRFeature>();
                    foreach (var hlf in hololensFeatures)
                    {
                        if (String.CompareOrdinal(hlf.featureIdInternal, "com.microsoft.openxr.feature.hololens") == 0)
                            hlf.enabled = true;
                    }
#endif //MICROSOFT_OPENXR_PACKAGE
                    PlayerSettings.runInBackground = true;
                },
                fixItMessage = "Change Run In Background to True.",
                error = false,
                buildTargetGroup = BuildTargetGroup.WSA,
            },
        };

        private static readonly List<OpenXRFeature.ValidationRule> CachedValidationList = new List<OpenXRFeature.ValidationRule>(BuiltinValidationRules.Length);

        /// <summary>
        /// Open the OpenXR project settings
        /// </summary>
        internal static void OpenProjectSettings() => SettingsService.OpenProjectSettings("Project/XR Plug-in Management/OpenXR");

        internal static void GetAllValidationIssues(List<OpenXRFeature.ValidationRule> issues, BuildTargetGroup buildTargetGroup)
        {
            issues.Clear();
            issues.AddRange(BuiltinValidationRules.Where(s => s.buildTargetGroup == buildTargetGroup || s.buildTargetGroup == BuildTargetGroup.Unknown));
            OpenXRFeature.GetFullValidationList(issues, buildTargetGroup);
        }

        /// <summary>
        /// Gathers and evaluates validation issues and adds them to a list.
        /// </summary>
        /// <param name="issues">List of validation issues to populate. List is cleared before populating.</param>
        /// <param name="buildTarget">Build target group to check for validation issues</param>
        public static void GetCurrentValidationIssues(List<OpenXRFeature.ValidationRule> issues, BuildTargetGroup buildTargetGroup)
        {
            CachedValidationList.Clear();
            CachedValidationList.AddRange(BuiltinValidationRules.Where(s => s.buildTargetGroup == buildTargetGroup || s.buildTargetGroup == BuildTargetGroup.Unknown));
            OpenXRFeature.GetValidationList(CachedValidationList, buildTargetGroup);

            issues.Clear();
            foreach (var validation in CachedValidationList)
            {
                if (!validation.checkPredicate?.Invoke() ?? false)
                {
                    issues.Add(validation);
                }
            }
        }

        /// <summary>
        /// Logs validation issues to console.
        /// </summary>
        /// <param name="targetGroup"></param>
        /// <returns>true if there were any errors that should stop the build</returns>
        internal static bool LogBuildValidationIssues(BuildTargetGroup targetGroup)
        {
            var failures = new List<OpenXRFeature.ValidationRule>();
            GetCurrentValidationIssues(failures, targetGroup);

            if (failures.Count <= 0) return false;

            bool anyErrors = false;
            foreach (var result in failures)
            {
                if (result.error)
                    Debug.LogError(result.message);
                else
                    Debug.LogWarning(result.message);
                anyErrors |= result.error;
            }

            if (anyErrors)
            {
                Debug.LogError("Double click to fix OpenXR Project Validation Issues.");
            }

            return anyErrors;
        }

        /// <summary>
        /// Logs playmode validation issues (anything rule that fails with errorEnteringPlaymode set to true).
        /// </summary>
        /// <returns>true if there were any errors that should prevent openxr starting in editor playmode</returns>
        internal static bool LogPlaymodeValidationIssues()
        {
            var failures = new List<OpenXRFeature.ValidationRule>();
            GetCurrentValidationIssues(failures, BuildTargetGroup.Standalone);

            if (failures.Count <= 0) return false;

            bool playmodeErrors = false;
            foreach (var result in failures)
            {
                if (result.errorEnteringPlaymode)
                    Debug.LogError(result.message);
                playmodeErrors |= result.errorEnteringPlaymode;
            }

            return playmodeErrors;
        }

        private static void RequireRestart()
        {
            // There is no public way to change the input handling backend .. so resorting to non-public way for now.
            if (!EditorUtility.DisplayDialog("Unity editor restart required", "The Unity editor must be restarted for this change to take effect.  Cancel to revert changes.", "Apply", "Cancel"))
                return;

            typeof(EditorApplication).GetMethod("RestartEditorAndRecompileScripts", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
        }
    }
}
#endif
