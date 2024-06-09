using System;
using System.Collections;

using UnityEngine;

namespace UnityEngine.XR.Management
{
    /// <summary>General settings container used to house the instance of the active settings as well as the manager
    /// instance used to load the loaders with.
    /// </summary>
    public class XRGeneralSettings : ScriptableObject
    {
        /// <summary>The key used to query to get the current loader settings.</summary>
        public static string k_SettingsKey = "com.unity.xr.management.loader_settings";
        internal static XRGeneralSettings s_RuntimeSettingsInstance = null;

        [SerializeField]
        internal XRManagerSettings m_LoaderManagerInstance = null;

        [SerializeField]
        [Tooltip("Toggling this on/off will enable/disable the automatic startup of XR at run time.")]
        internal bool m_InitManagerOnStart = true;

        /// <summary>The current active manager used to manage XR lifetime.</summary>
        public XRManagerSettings Manager
        {
            get { return m_LoaderManagerInstance; }
            set { m_LoaderManagerInstance = value; }
        }

        private XRManagerSettings m_XRManager = null;

#pragma warning disable 414 // Suppress warning for needed variables.
        private bool m_ProviderIntialized = false;
        private bool m_ProviderStarted = false;
#pragma warning restore 414

        /// <summary>The current settings instance.</summary>
        public static XRGeneralSettings Instance
        {
            get
            {
                return s_RuntimeSettingsInstance;
            }
        }

        /// <summary>The current active manager used to manage XR lifetime.</summary>
        public XRManagerSettings AssignedSettings
        {
            get
            {
                return m_LoaderManagerInstance;
            }
        }

        /// <summary>Used to set if the manager is activated and initialized on startup.</summary>
        public bool InitManagerOnStart
        {
            get
            {
                return m_InitManagerOnStart;
            }
        }
        
        void Awake()
        {
            Debug.Log("XRGeneral Settings awakening...");
            s_RuntimeSettingsInstance = this;
            Application.quitting += Quit;
            DontDestroyOnLoad(s_RuntimeSettingsInstance);
        }

        static void Quit()
        {
            var instance = XRGeneralSettings.Instance;
            if (instance == null)
                return;

            instance.DeInitXRSDK();
        }

        void Start()
        {
            StartXRSDK();
        }

        void OnDestroy()
        {
            DeInitXRSDK();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void AttemptInitializeXRSDKOnLoad()
        {
            var instance = XRGeneralSettings.Instance;
            if (instance == null || !instance.InitManagerOnStart)
                return;

            instance.InitXRSDK();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        internal static void AttemptStartXRSDKOnBeforeSplashScreen()
        {
            var instance = XRGeneralSettings.Instance;
            if (instance == null || !instance.InitManagerOnStart)
                return;

            instance.StartXRSDK();
        }

        private void InitXRSDK()
        {
            if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.m_LoaderManagerInstance == null || XRGeneralSettings.Instance.m_InitManagerOnStart == false)
                return;

            m_XRManager = XRGeneralSettings.Instance.m_LoaderManagerInstance;
            if (m_XRManager == null)
            {
                Debug.LogError("Assigned GameObject for XR Management loading is invalid. No XR Providers will be automatically loaded.");
                return;
            }

            m_XRManager.automaticLoading = false;
            m_XRManager.automaticRunning = false;
            m_XRManager.InitializeLoaderSync();
            m_ProviderIntialized = true;
        }

        private void StartXRSDK()
        {
            if (m_XRManager != null && m_XRManager.activeLoader != null)
            {
                m_XRManager.StartSubsystems();
                m_ProviderStarted = true;
            }
        }

        private void StopXRSDK()
        {
            if (m_XRManager != null && m_XRManager.activeLoader != null)
            {
                m_XRManager.StopSubsystems();
                m_ProviderStarted = false;
            }
        }

        private void DeInitXRSDK()
        {
            if (m_XRManager != null && m_XRManager.activeLoader != null)
            {
                m_XRManager.DeinitializeLoader();
                m_XRManager = null;
                m_ProviderIntialized = false;
            }
        }

    }
}
