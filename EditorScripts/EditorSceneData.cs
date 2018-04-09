#if UNITY_EDITOR

using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace ModIO
{
    [ExecuteInEditMode]
    public class EditorSceneData : MonoBehaviour
    {
        public const ModLogoVersion LOGO_VERSION = ModLogoVersion.Thumbnail_320x180;

        public int modId = 0;
        public EditableModFields modProfileEdits = null;

        public string buildLocation = string.Empty;
        public bool setBuildAsPrimary = true;
        public ModfileEditableFields modfileValues = null;

        public Texture2D modLogoTexture = null;
        private DateTime modLogoLastWrite = new DateTime();

        private void OnEnable()
        {
            ModManager.OnModImageUpdated += OnModImageUpdated;

            ReacquireModLogo();
        }

        private void OnDisable()
        {
            ModManager.OnModImageUpdated -= OnModImageUpdated;
        }

        // TODO(@jackson): Move to PropertyDrawer
        private void OnModImageUpdated(string identifier, ImageVersion version, Texture2D imageTexture)
        {
            // TODO(@jackson): Serialize this
            // TODO(@jackson): Other thumb sizes?
            if(modProfileEdits.logoLocator.value.Equals(identifier)
               && version == ImageVersion.Thumb_320x180)
            {
                modLogoTexture = imageTexture;
                modLogoLastWrite = new DateTime();
            }
        }

        // TODO(@jackson): Move to PropertyDrawer
        private void ReacquireModLogo()
        {
            if(File.Exists(modProfileEdits.logoLocator.value.source))
            {
                modLogoTexture = new Texture2D(0, 0);
                modLogoTexture.LoadImage(File.ReadAllBytes(modProfileEdits.logoLocator.value.source));

                modLogoLastWrite = File.GetLastWriteTime(modProfileEdits.logoLocator.value.source);
            }
            else if(modId > 0)
            {
                ModProfile profile = ModManager.GetModProfile(this.modId);

                modProfileEdits.logoLocator.fileName = profile.logoLocator.fileName;
                modProfileEdits.logoLocator.source = profile.logoLocator.source;
                
                modLogoTexture = ModManager.LoadOrDownloadModLogo(this.modId, LOGO_VERSION);
                modLogoLastWrite = new DateTime();
            }
            else
            {
                // TODO(@jackson): Does not exist
                modProfileEdits.logoLocator.value.fileName = string.Empty;
                modProfileEdits.logoLocator.value.source = string.Empty;
                modProfileEdits.logoLocator.isDirty = false;
            }
        }

        // TODO(@jackson): Move to PropertyDrawer
        private void Update()
        {
            // string newLogoLocal = modProfileEdits.logoLocator.value;
            // string newLogoServer = modInfo.logo.thumb320x180;
            // string newLogoSource = (modId > 0 && newLogoLocal == "" ? newLogoServer : newLogoLocal);

            // // TODO(@jackson): Handle file missing
            // // - If file has changed or unsubmitted file is updated -
            // if((modLogoSource != newLogoSource)
            //    || (File.Exists(modProfileEdits.logoLocator.value) && File.GetLastWriteTime(modProfileEdits.logoLocator.value) > modLogoLastWrite))
            // {
            //     ReacquireModLogo();
            // }
        }
    }
}
#endif
