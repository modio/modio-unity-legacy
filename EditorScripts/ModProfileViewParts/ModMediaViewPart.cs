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
        private const ModGalleryImageSize IMAGE_PREVIEW_SIZE = ModGalleryImageSize.Thumbnail_320x180;

        // ------[ EDITOR CACHING ]------
        private bool isRepaintRequired = false;

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
                    .FindPropertyRelative("url")
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
        public void OnEnable(SerializedProperty serializedEditableModProfile, ModProfile baseProfile, UserProfile user)
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

                // TODO(@jackson): Fix this up methinks
                if(!String.IsNullOrEmpty(imageFileName)
                   && !String.IsNullOrEmpty(imageURL))
                {
                    Texture2D texture = CacheClient.ReadImageFile(imageURL);

                    if(texture != null)
                    {
                        this.textureCache[imageFileName] = imageTexture;
                    }
                    else
                    {
                        this.textureCache[imageFileName] = UISettings.Instance.DownloadingPlaceholderImages.modLogo;

                        ModManager.GetModGalleryImage(baseProfile,
                                                      imageFileName,
                                                      IMAGE_PREVIEW_SIZE,
                                                      (t) => { this.textureCache[imageFileName] = t; isRepaintRequired = true; },
                                                      null);
                    }

                }
            }
        }

        public void OnDisable()
        {
        }

        // ------[ UPDATES ]------
        public void OnUpdate() {}

        protected virtual void OnModGalleryImageUpdated(int modId,
                                                        string imageFileName,
                                                        ModGalleryImageSize size,
                                                        Texture2D texture)
        {
            if(profile != null
               && profile.id == modId
               && size == IMAGE_PREVIEW_SIZE
               && textureCache.ContainsKey(imageFileName))
            {
                textureCache[imageFileName] = texture;
                isRepaintRequired = true;
            }
        }

        // ------[ GUI ]------
        public void OnGUI()
        {
            isRepaintRequired = false;

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

        public bool IsRepaintRequired()
        {
            return this.isRepaintRequired;
        }

        // - Image Locator Layouting -
        private void LayoutGalleryImageProperty(SerializedProperty elementProperty)
        {
            bool doBrowse = false;
            string imageFileName = elementProperty.FindPropertyRelative("fileName").stringValue;
            string imageSource = elementProperty.FindPropertyRelative("url").stringValue;

            // - Browse Field -
            EditorGUILayout.BeginHorizontal();
                doBrowse |= EditorGUILayoutExtensions.BrowseButton(imageSource,
                                                                   new GUIContent("Location"));
            EditorGUILayout.EndHorizontal();

            // - Draw Texture -
            Texture2D imageTexture = GetOrLoadOrDownloadGalleryImageTexture(imageFileName,
                                                                            imageSource);

            if(imageTexture != null)
            {
                // TODO(@jackson): Make full-width
                Rect imageRect = EditorGUILayout.GetControlRect(false, 180.0f, null);
                EditorGUI.DrawPreviewTexture(new Rect((imageRect.width - 320.0f) * 0.5f,
                                                      imageRect.y,
                                                      320.0f,
                                                      imageRect.height),
                                             imageTexture, null, ScaleMode.ScaleAndCrop);
                doBrowse |= GUI.Button(imageRect, "", GUI.skin.label);
            }

            if(doBrowse)
            {
                EditorApplication.delayCall += () =>
                {

                    // TODO(@jackson): Add other file-types
                    string path = EditorUtility.OpenFilePanel("Select Gallery Image", "", "png");
                    Texture2D newTexture = CacheClient.ReadImageFile(path);

                    if(newTexture != null)
                    {
                        string fileName = GenerateUniqueFileName(path);

                        elementProperty.FindPropertyRelative("url").stringValue = path;
                        elementProperty.FindPropertyRelative("fileName").stringValue = fileName;

                        galleryImagesProp.FindPropertyRelative("isDirty").boolValue = true;
                        galleryImagesProp.serializedObject.ApplyModifiedProperties();

                        textureCache.Add(fileName, newTexture);
                    }
                };
            }
        }

        private Texture2D GetOrLoadOrDownloadGalleryImageTexture(string imageFileName,
                                                                 string imageSource)
        {
            if(String.IsNullOrEmpty(imageFileName))
            {
                return null;
            }

            Texture2D texture;
            // - Get -
            if(this.textureCache.TryGetValue(imageFileName, out texture))
            {
                return texture;
            }
            // - Load -
            else if((texture = CacheClient.ReadImageFile(imageSource)) != null)
            {
                this.textureCache.Add(imageFileName, texture);
                return texture;
            }
            // - LoadOrDownload -
            else if(profile != null)
            {
                this.textureCache.Add(imageFileName, UISettings.Instance.DownloadingPlaceholderImages.modLogo);

                ModManager.GetModGalleryImage(profile,
                                              imageFileName,
                                              IMAGE_PREVIEW_SIZE,
                                              (t) => { this.textureCache[imageFileName] = t; isRepaintRequired = true; },
                                              null);
                return this.textureCache[imageFileName];
            }

            return null;
        }

        // - Misc Functionality -
        // TODO(@jackson): Reimplement
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
