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
        public int modId = 0;
        public EditableModFields modData = null;

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
            if(modData.logoIdentifier.value.Equals(identifier)
               && version == ImageVersion.Thumb_320x180)
            {
                modLogoTexture = imageTexture;
                modLogoLastWrite = new DateTime();
            }
        }

        // TODO(@jackson): Move to PropertyDrawer
        private void ReacquireModLogo()
        {
            if(File.Exists(modData.logoIdentifier.value))
            {
                modLogoTexture = new Texture2D(0, 0);
                modLogoTexture.LoadImage(File.ReadAllBytes(modData.logoIdentifier.value));

                modLogoLastWrite = File.GetLastWriteTime(modData.logoIdentifier.value);
            }
            else if(modId > 0)
            {
                modData.logoIdentifier.value = ModManager.GetModProfile(modId).logoIdentifier;
                modData.logoIdentifier.isDirty = false;
                modLogoTexture = ModManager.LoadOrDownloadModImage(modData.logoIdentifier.value, ImageVersion.Thumb_320x180);
                modLogoLastWrite = new DateTime();
            }
            else
            {
                modData.logoIdentifier.value = "";
                modData.logoIdentifier.isDirty = false;
            }
        }

        // TODO(@jackson): Move to PropertyDrawer
        private void Update()
        {
            // string newLogoLocal = modData.logoIdentifier.value;
            // string newLogoServer = modInfo.logo.thumb320x180;
            // string newLogoSource = (modId > 0 && newLogoLocal == "" ? newLogoServer : newLogoLocal);

            // // TODO(@jackson): Handle file missing
            // // - If file has changed or unsubmitted file is updated -
            // if((modLogoSource != newLogoSource)
            //    || (File.Exists(modData.logoIdentifier.value) && File.GetLastWriteTime(modData.logoIdentifier.value) > modLogoLastWrite))
            // {
            //     ReacquireModLogo();
            // }
        }
    }
}
#endif
