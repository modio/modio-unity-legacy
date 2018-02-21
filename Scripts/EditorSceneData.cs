#if UNITY_EDITOR

using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace ModIO
{
    // TODO(@jackson): Handle ModUpdated (esp. Deleted)
    [ExecuteInEditMode]
    public class EditorSceneData : MonoBehaviour
    {
        public EditableModInfo modInfo = new EditableModInfo();

        private Texture2D modLogoTexture = null;
        private string modLogoSource = "";
        private DateTime modLogoLastWrite = new DateTime();

        private void OnEnable()
        {
            ModManager.OnModLogoUpdated += OnModLogoUpdated;

            ReacquireModLogo();
        }

        private void OnDisable()
        {
            ModManager.OnModLogoUpdated -= OnModLogoUpdated;
        }

        private void OnModLogoUpdated(int modId, Texture2D modLogo, LogoVersion logoVersion)
        {
            if(modId == modInfo.id
               && modInfo.unsubmittedLogoFilepath == ""
               && logoVersion == LogoVersion.Thumb_320x180)
            {
                modLogoTexture = modLogo;
                modLogoSource = modInfo.logo.thumb320x180;
                modLogoLastWrite = new DateTime();
            }
        }

        private void ReacquireModLogo()
        {
            if(modInfo.id > 0
               && modInfo.unsubmittedLogoFilepath == "")
            {
                modLogoSource = modInfo.logo.thumb320x180;

                modLogoTexture = ModManager.LoadCachedModLogo(modInfo.id, LogoVersion.Thumb_320x180);
                modLogoLastWrite = new DateTime();

                if(modLogoTexture == null)
                {
                    modLogoTexture = UISettings.Instance.LoadingLogo320x180;

                    ModManager.DownloadModLogo(modInfo.id, LogoVersion.Thumb_320x180);
                }
            }
            else
            {
                modLogoSource = modInfo.unsubmittedLogoFilepath;

                // TODO(@jackson): Complete this
                if(File.Exists(modLogoSource))
                {
                    modLogoTexture = new Texture2D(0, 0);
                    modLogoTexture.LoadImage(File.ReadAllBytes(modLogoSource));

                    modLogoLastWrite = File.GetLastWriteTime(modLogoSource);
                }
            }
        }

        private void Update()
        {
            string newLogoLocal = modInfo.unsubmittedLogoFilepath;
            string newLogoServer = modInfo.logo.thumb320x180;
            string newLogoSource = (modInfo.id > 0 && newLogoLocal == "" ? newLogoServer : newLogoLocal);

            // TODO(@jackson): Handle file missing
            // - If file has changed or unsubmitted file is updated -
            if((modLogoSource != newLogoSource)
               || (File.Exists(modInfo.unsubmittedLogoFilepath) && File.GetLastWriteTime(modInfo.unsubmittedLogoFilepath) > modLogoLastWrite))
            {
                ReacquireModLogo();
            }
        }

        public Texture2D GetModLogoTexture()
        {
            return modLogoTexture;
        }
        public string GetModLogoSource()
        {
            return modLogoSource;
        }
    }
}
#endif
