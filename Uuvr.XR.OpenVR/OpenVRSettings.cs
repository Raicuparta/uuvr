using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Unity.XR.OpenVR
{
    [XRConfigurationData("OpenVR", "Unity.XR.OpenVR.Settings")]
    [System.Serializable]
    public class OpenVRSettings : ScriptableObject
    {
        public enum StereoRenderingModes
        {
            MultiPass = 0,
            SinglePassInstanced
        }

        public enum InitializationTypes
        {
            Scene = 1,
            Overlay = 2,
        }

        public enum MirrorViewModes
        {
            None = 0,
            Left,
            Right,
            OpenVR,
        }

        public bool PromptToUpgradePackage = true;

        public bool PromptToUpgradePreviewPackages = true;

        public string SkipPromptForVersion = null;

        public StereoRenderingModes StereoRenderingMode = StereoRenderingModes.SinglePassInstanced;

        public InitializationTypes InitializationType = InitializationTypes.Scene;

        public string EditorAppKey = null;

        public string ActionManifestFileRelativeFilePath;

        public MirrorViewModes MirrorView = MirrorViewModes.Right;

        public const string StreamingAssetsFolderName = "SteamVR";
        public const string ActionManifestFileName = "legacy_manifest.json";
        public static string GetStreamingSteamVRPath(bool create = true)
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, StreamingAssetsFolderName);

            if (create)
            {
                CreateDirectory(new DirectoryInfo(path));
            }

            return path;
        }

        private static void CreateDirectory(DirectoryInfo directory)
        {
            if (directory.Parent.Exists == false)
                CreateDirectory(directory.Parent);

            if (directory.Exists == false)
                directory.Create();
        }

        public bool HasCopiedDefaults = false;

        public ushort GetStereoRenderingMode()
        {
            return (ushort)StereoRenderingMode;
        }

        public ushort GetInitializationType()
        {
            return (ushort)InitializationType;
        }

        public MirrorViewModes GetMirrorViewMode()
        {
            return MirrorView;
        }

        /// <summary>
        /// Sets the mirror view mode (left, right, composite of both + openvr overlays) at runtime.
        /// </summary>
        /// <param name="newMode">left, right, composite of both + openvr overlays</param>
        public void SetMirrorViewMode(MirrorViewModes newMode)
        {
            MirrorView = newMode;
            SetMirrorViewMode((ushort)newMode);
        }

        public string GenerateEditorAppKey()
        {
            return string.Format("application.generated.unity.{0}.{1}.exe", CleanProductName(), ((int)(UnityEngine.Random.value * int.MaxValue)).ToString());
        }

        private static string CleanProductName()
        {
            var productName = Application.productName;
            if (string.IsNullOrEmpty(productName))
                productName = "unnamed_product";
            else
            {
                productName = System.Text.RegularExpressions.Regex.Replace(productName, "[^\\w\\._]", "");
                productName = productName.ToLower();
                productName = string.Concat(productName.Normalize(NormalizationForm.FormD).Where(
                    c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
                var bytes = Encoding.ASCII.GetBytes(productName);
                var chars = Encoding.ASCII.GetChars(bytes);
                productName = new String(chars).Replace("?", "");
                if (productName.Length == 0)
                {
                    productName = Mathf.Abs(Application.productName.GetHashCode()).ToString();
                }
            }

            return productName;
        }

        public static OpenVRSettings GetSettings(bool create = true)
        {
            OpenVRSettings settings = null;
            settings = OpenVRSettings.s_Settings;

            if (settings == null && create)
                settings = OpenVRSettings.CreateInstance<OpenVRSettings>();

            return settings;
        }

        [DllImport("XRSDKOpenVR.dll", CharSet = CharSet.Auto)]
        public static extern void SetMirrorViewMode(ushort mirrorViewMode);


        public bool InitializeActionManifestFileRelativeFilePath()
        {
            var oldPath = ActionManifestFileRelativeFilePath;
            string newPath;

            if (OpenVRHelpers.IsUsingSteamVRInput())
            {
                newPath = System.IO.Path.Combine(OpenVRSettings.GetStreamingSteamVRPath(false), OpenVRHelpers.GetActionManifestNameFromPlugin());
                
                var fullpath = System.IO.Path.GetFullPath(".");
                newPath = newPath.Remove(0, fullpath.Length + 1);

                if (newPath.StartsWith("Assets"))
                    newPath = newPath.Remove(0, "Assets".Length + 1);
            }
            else
            {
                newPath = null;
            }
            
            return false;
        }
        
        public static OpenVRSettings s_Settings;

		public void Awake()
		{
			s_Settings = this;
		}
    }
}
