#if UNITY_EDITOR

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ModIO
{
    public class ModMediaViewPart : IModProfileViewPart
    {
        // ------[ CONSTANTS ]------
        private const ModGalleryImageVersion IMAGE_PREVIEW_VERSION = ModGalleryImageVersion.Thumbnail_320x180;

        // ------[ EDITOR CACHING ]------
        // - Serialized Property -
        private ModProfile profile;
        private SerializedProperty youtubeURLsProp;
        private SerializedProperty sketchfabURLsProp;
        private SerializedProperty galleryImagesProp;

        private string GetGalleryImageFileName(int index)
        {
            return (galleryImagesProp
                    .FindPropertyRelative("value")
                    .GetArrayElementAtIndex(index)
                    .FindPropertyRelative("fileName")
                    .stringValue);
        }

        private string GetGalleryImageSource(int index)
        {
            return (galleryImagesProp
                    .FindPropertyRelative("value")
                    .GetArrayElementAtIndex(index)
                    .FindPropertyRelative("source")
                    .stringValue);
        }

        private string GenerateUniqueFileName(string path)
        {
            string fileNameNoExtension = System.IO.Path.GetFileNameWithoutExtension(path);
            string fileExtension = System.IO.Path.GetExtension(path);
            int numberToAppend = 0;
            string regexPattern = fileNameNoExtension + "\\d*\\" + fileExtension;

            foreach(SerializedProperty elementProperty in galleryImagesProp.FindPropertyRelative("value"))
            {
                var elementFileName = elementProperty.FindPropertyRelative("fileName").stringValue;
                if(System.Text.RegularExpressions.Regex.IsMatch(elementFileName, regexPattern))
                {
                    string numberString = elementFileName.Substring(fileNameNoExtension.Length);
                    numberString = numberString.Substring(0, numberString.Length - fileExtension.Length);
                    int number;
                    if(!Int32.TryParse(numberString, out number))
                    {
                        number = 0;
                    }

                    if(numberToAppend <= number)
                    {
                        numberToAppend = number + 1;
                    }
                }
            }

            if(numberToAppend > 0)
            {
                fileNameNoExtension += numberToAppend.ToString();
            }

            return fileNameNoExtension + fileExtension;
        }

        // - Foldouts -
        private bool isYouTubeExpanded;
        private bool isSketchFabExpanded;
        private bool isImagesExpanded;

        private Dictionary<string, Texture2D> textureCache;


        // ------[ INITIALIZATION ]------
        public void OnEnable(SerializedProperty serializedEditableModProfile, ModProfile baseProfile)
        {
            this.profile = baseProfile;
            this.youtubeURLsProp = serializedEditableModProfile.FindPropertyRelative("youtubeURLs");
            this.sketchfabURLsProp = serializedEditableModProfile.FindPropertyRelative("sketchfabURLs");
            this.galleryImagesProp = serializedEditableModProfile.FindPropertyRelative("galleryImageLocators");

            this.isYouTubeExpanded = false;
            this.isSketchFabExpanded = false;
            this.isImagesExpanded = false;

            // Initialize textureCache
            this.textureCache = new Dictionary<string, Texture2D>(galleryImagesProp.FindPropertyRelative("value").arraySize);
            for (int i = 0;
                 i < galleryImagesProp.FindPropertyRelative("value").arraySize;
                 ++i)
            {
                string imageFileName = GetGalleryImageFileName(i);
                string imageURL = GetGalleryImageSource(i);
                Texture2D imageTexture = null;

                if(!String.IsNullOrEmpty(imageFileName)
                   && !String.IsNullOrEmpty(imageURL))
                {
                    if(!Utility.TryLoadTextureFromFile(imageURL, out imageTexture))
                    {
                        imageTexture = ModManager.LoadOrDownloadModGalleryImage(baseProfile.id,
                                                                                imageFileName,
                                                                                IMAGE_PREVIEW_VERSION);
                    }
                    
                    this.textureCache[imageFileName] = imageTexture;
                }
            }
        }

        public void OnDisable() {}

        // ------[ ONUPDATE ]------
        public void OnUpdate() {}

        // ------[ ONGUI ]------
        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Media");
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(profile == null))
                {
                    if(EditorGUILayoutExtensions.UndoButton())
                    {
                        ResetModMedia();
                    }
                }
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.IndentLevelScope())
            {
                // - YouTube -
                EditorGUI.BeginChangeCheck();
                EditorGUILayoutExtensions.ArrayPropertyField(youtubeURLsProp.FindPropertyRelative("value"),
                                                             "YouTube Links", ref isYouTubeExpanded);
                youtubeURLsProp.FindPropertyRelative("isDirty").boolValue |= EditorGUI.EndChangeCheck();
                // - SketchFab -
                EditorGUI.BeginChangeCheck();
                EditorGUILayoutExtensions.ArrayPropertyField(sketchfabURLsProp.FindPropertyRelative("value"),
                                                             "SketchFab Links", ref isSketchFabExpanded);
                sketchfabURLsProp.FindPropertyRelative("isDirty").boolValue |= EditorGUI.EndChangeCheck();
                // - Gallery Images -
                EditorGUI.BeginChangeCheck();
                EditorGUILayoutExtensions.CustomLayoutArrayPropertyField(galleryImagesProp.FindPropertyRelative("value"),
                                                                         "Gallery Images Links",
                                                                         ref isImagesExpanded,
                                                                         LayoutGalleryImageProperty);
                galleryImagesProp.FindPropertyRelative("isDirty").boolValue |= EditorGUI.EndChangeCheck();
            }
        }

        // - Image Locator Layouting -
        private void LayoutGalleryImageProperty(SerializedProperty elementProperty)
        {
            bool doBrowse = false;

            // - Browse Field -
            EditorGUILayout.BeginHorizontal();
                doBrowse |= EditorGUILayoutExtensions.BrowseButton(elementProperty.FindPropertyRelative("source").stringValue,
                                                                   new GUIContent("Location"));
            EditorGUILayout.EndHorizontal();

            // - Draw Texture -
            Texture2D texture = null;
            if(this.textureCache.TryGetValue(elementProperty.FindPropertyRelative("fileName").stringValue,
                                             out texture))
            {
                // TODO(@jackson): Make full-width
                Rect imageRect = EditorGUILayout.GetControlRect(false, 180.0f, null);
                EditorGUI.DrawPreviewTexture(new Rect((imageRect.width - 320.0f) * 0.5f,
                                                      imageRect.y,
                                                      320.0f,
                                                      imageRect.height),
                                             texture, null, ScaleMode.ScaleAndCrop);
                doBrowse |= GUI.Button(imageRect, "", GUI.skin.label);
            }

            if(doBrowse)
            {
                EditorApplication.delayCall += () =>
                {
                    Texture2D newTexture;

                    // TODO(@jackson): Add other file-types
                    string path = EditorUtility.OpenFilePanel("Select Gallery Image", "", "png");
                    if (path.Length != 0
                        && Utility.TryLoadTextureFromFile(path, out newTexture))
                    {
                        string fileName = GenerateUniqueFileName(path);

                        elementProperty.FindPropertyRelative("source").stringValue = path;
                        elementProperty.FindPropertyRelative("fileName").stringValue = fileName;

                        galleryImagesProp.FindPropertyRelative("isDirty").boolValue = true;
                        galleryImagesProp.serializedObject.ApplyModifiedProperties();

                        textureCache.Add(fileName, newTexture);
                    }
                };
            }
        }

        // - Misc Functionality -
        private void ResetModMedia() {}
        // {
        //     // - YouTube -
        //     initialDataProp = editableProfileProperty.FindPropertyRelative("_initialData.media.youtube");
        //     currentDataProp = editableProfileProperty.FindPropertyRelative("_data.media.youtube");

        //     currentDataProp.arraySize = initialDataProp.arraySize;

        //     for(int i = 0; i < initialDataProp.arraySize; ++i)
        //     {
        //         currentDataProp.GetArrayElementAtIndex(i).stringValue
        //             = initialDataProp.GetArrayElementAtIndex(i).stringValue;
        //     }

        //     // - SketchFab -
        //     initialDataProp = editableProfileProperty.FindPropertyRelative("_initialData.media.sketchfab");
        //     currentDataProp = editableProfileProperty.FindPropertyRelative("_data.media.sketchfab");

        //     currentDataProp.arraySize = initialDataProp.arraySize;

        //     for(int i = 0; i < initialDataProp.arraySize; ++i)
        //     {
        //         currentDataProp.GetArrayElementAtIndex(i).stringValue
        //             = initialDataProp.GetArrayElementAtIndex(i).stringValue;
        //     }

        //     // - Image Gallery -
        //     initialDataProp = editableProfileProperty.FindPropertyRelative("_initialData.media.images");
        //     currentDataProp = editableProfileProperty.FindPropertyRelative("_data.media.images");

        //     currentDataProp.arraySize = initialDataProp.arraySize;

        //     for(int i = 0; i < initialDataProp.arraySize; ++i)
        //     {
        //         currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("filename").stringValue
        //             = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("filename").stringValue;
        //         currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("original").stringValue
        //             = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("original").stringValue;
        //         currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("thumb_320x180").stringValue
        //             = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("thumb_320x180").stringValue;
        //     }
        // }
    }
}

#endif
