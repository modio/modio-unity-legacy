#if UNITY_EDITOR

using System;
using System.Collections.Generic;

using File = System.IO.File;
using Path = System.IO.Path;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ModIO.EditorCode
{
    public class ModMediaViewPart : IModProfileViewPart
    {
        // ------[ CONSTANTS ]------
        private const ModGalleryImageSize IMAGE_PREVIEW_SIZE =
            ModGalleryImageSize.Thumbnail_320x180;
        private static readonly string[] IMAGE_FILE_FILTER =
            new string[] { "JPEG Image Format", "jpeg,jpg", "PNG Image Format", "png",
                           "GIF Image Format",  "gif" };

        // ------[ EDITOR CACHING ]------
        private bool isRepaintRequired = false;

        // - Serialized Property -
        private ModProfile profile;
        private SerializedProperty youTubeURLsProp;
        private SerializedProperty sketchfabURLsProp;
        private SerializedProperty galleryImagesProp;

        private string GetGalleryImageFileName(int index)
        {
            return (galleryImagesProp.FindPropertyRelative("value")
                        .GetArrayElementAtIndex(index)
                        .FindPropertyRelative("fileName")
                        .stringValue);
        }

        private string GetGalleryImageSource(int index)
        {
            return (galleryImagesProp.FindPropertyRelative("value")
                        .GetArrayElementAtIndex(index)
                        .FindPropertyRelative("url")
                        .stringValue);
        }

        private string GenerateUniqueFileName(string path)
        {
            string fileNameNoExtension = Path.GetFileNameWithoutExtension(path);
            string fileExtension = Path.GetExtension(path);
            int numberToAppend = 0;
            string regexPattern = fileNameNoExtension + "\\d*\\" + fileExtension;

            foreach(SerializedProperty elementProperty in galleryImagesProp.FindPropertyRelative(
                        "value"))
            {
                var elementFileName = elementProperty.FindPropertyRelative("fileName").stringValue;
                if(System.Text.RegularExpressions.Regex.IsMatch(elementFileName, regexPattern))
                {
                    string numberString = elementFileName.Substring(fileNameNoExtension.Length);
                    numberString =
                        numberString.Substring(0, numberString.Length - fileExtension.Length);
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
        public void OnEnable(SerializedProperty serializedEditableModProfile,
                             ModProfile baseProfile, UserProfile user)
        {
            this.profile = baseProfile;
            this.youTubeURLsProp = serializedEditableModProfile.FindPropertyRelative("youTubeURLs");
            this.sketchfabURLsProp =
                serializedEditableModProfile.FindPropertyRelative("sketchfabURLs");
            this.galleryImagesProp =
                serializedEditableModProfile.FindPropertyRelative("galleryImageLocators");

            this.isYouTubeExpanded = false;
            this.isSketchFabExpanded = false;
            this.isImagesExpanded = false;

            // Initialize textureCache
            int arraySize = galleryImagesProp.FindPropertyRelative("value").arraySize;

            this.textureCache = new Dictionary<string, Texture2D>(arraySize);
            for(int i = 0; i < arraySize; ++i)
            {
                string imageFileName = GetGalleryImageFileName(i);
                string imageURL = GetGalleryImageSource(i);

                if(!String.IsNullOrEmpty(imageFileName) && !String.IsNullOrEmpty(imageURL))
                {
                    GalleryImageLocator imageLocator =
                        baseProfile.media.GetGalleryImageWithFileName(imageFileName);
                    this.textureCache[imageFileName] = EditorImages.LoadingPlaceholder;

                    if(imageLocator != null)
                    {
                        ModManager.GetModGalleryImage(baseProfile.id, imageLocator,
                                                      IMAGE_PREVIEW_SIZE, (t) => {
                                                          this.textureCache[imageFileName] = t;
                                                          isRepaintRequired = true;
                                                      }, null);
                    }
                    else
                    {
                        byte[] data = null;
                        data = File.ReadAllBytes(imageURL);

                        if(data != null)
                        {
                            this.textureCache[imageFileName] = IOUtilities.ParseImageData(data);
                        }
                    }
                }
            }
        }

        public void OnDisable() {}

        // ------[ UPDATES ]------
        public void OnUpdate() {}

        protected virtual void OnModGalleryImageUpdated(int modId, string imageFileName,
                                                        ModGalleryImageSize size, Texture2D texture)
        {
            if(profile != null && profile.id == modId && size == IMAGE_PREVIEW_SIZE
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
            using(new EditorGUI.DisabledScope(profile == null))
            {
                if(EditorGUILayoutExtensions.UndoButton())
                {
                    ResetModMedia();
                }
            }
            EditorGUILayout.EndHorizontal();

            using(new EditorGUI.IndentLevelScope())
            {
                // - YouTube -
                EditorGUI.BeginChangeCheck();
                EditorGUILayoutExtensions.ArrayPropertyField(
                    youTubeURLsProp.FindPropertyRelative("value"), "YouTube Links",
                    ref isYouTubeExpanded);
                youTubeURLsProp.FindPropertyRelative("isDirty").boolValue |=
                    EditorGUI.EndChangeCheck();
                // - SketchFab -
                EditorGUI.BeginChangeCheck();
                EditorGUILayoutExtensions.ArrayPropertyField(
                    sketchfabURLsProp.FindPropertyRelative("value"), "SketchFab Links",
                    ref isSketchFabExpanded);
                sketchfabURLsProp.FindPropertyRelative("isDirty").boolValue |=
                    EditorGUI.EndChangeCheck();
                // - Gallery Images -
                EditorGUI.BeginChangeCheck();
                EditorGUILayoutExtensions.CustomLayoutArrayPropertyField(
                    galleryImagesProp.FindPropertyRelative("value"), "Gallery Images Links",
                    ref isImagesExpanded, LayoutGalleryImageProperty);
                galleryImagesProp.FindPropertyRelative("isDirty").boolValue |=
                    EditorGUI.EndChangeCheck();
            }
        }

        public bool IsRepaintRequired()
        {
            return this.isRepaintRequired;
        }

        // - Image Locator Layouting -
        private void LayoutGalleryImageProperty(int elementIndex,
                                                SerializedProperty elementProperty)
        {
            bool doBrowse = false;
            bool doClear = false;
            string imageFileName = elementProperty.FindPropertyRelative("fileName").stringValue;
            string imageSource = elementProperty.FindPropertyRelative("url").stringValue;

            // - Browse Field -
            EditorGUILayout.BeginHorizontal();
            doBrowse |= EditorGUILayoutExtensions.BrowseButton(
                imageSource, new GUIContent("Image " + elementIndex));
            doClear = EditorGUILayoutExtensions.ClearButton();
            EditorGUILayout.EndHorizontal();

            // - Draw Texture -
            Texture2D imageTexture = GetGalleryImageByIndex(elementIndex);

            if(imageTexture != null)
            {
                EditorGUI.indentLevel += 2;
                EditorGUILayout.LabelField("File Name", imageFileName);
                Rect imageRect = EditorGUILayout.GetControlRect(false, 180.0f);
                imageRect = EditorGUI.IndentedRect(imageRect);
                EditorGUI.DrawPreviewTexture(
                    new Rect(imageRect.x, imageRect.y, 320.0f, imageRect.height), imageTexture,
                    null, ScaleMode.ScaleAndCrop);
                doBrowse |= GUI.Button(imageRect, "", GUI.skin.label);
                EditorGUI.indentLevel -= 2;
            }

            if(doBrowse)
            {
                EditorApplication.delayCall += () =>
                {
                    string path = EditorUtility.OpenFilePanelWithFilters(
                        "Select Gallery Image", "", ModMediaViewPart.IMAGE_FILE_FILTER);

                    byte[] data = null;
                    data = File.ReadAllBytes(path);

                    Texture2D newTexture = null;
                    if(data != null)
                    {
                        newTexture = IOUtilities.ParseImageData(data);
                    }

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
            if(doClear)
            {
                elementProperty.FindPropertyRelative("url").stringValue = string.Empty;
                elementProperty.FindPropertyRelative("fileName").stringValue = string.Empty;

                galleryImagesProp.FindPropertyRelative("isDirty").boolValue = true;
                galleryImagesProp.serializedObject.ApplyModifiedProperties();
            }
        }

        private Texture2D GetGalleryImageByIndex(int index)
        {
            string imageFileName = GetGalleryImageFileName(index);
            GalleryImageLocator imageLocator = null;

            if(this.profile != null && this.profile.media != null)
            {
                imageLocator = this.profile.media.GetGalleryImageWithFileName(imageFileName);
            }

            Texture2D texture;
            if(String.IsNullOrEmpty(imageFileName))
            {
                return null;
            }
            // - Get -
            else if(this.textureCache.TryGetValue(imageFileName, out texture))
            {
                return texture;
            }
            // - LoadOrDownload -
            else if(imageLocator != null)
            {
                this.textureCache.Add(imageFileName, EditorImages.LoadingPlaceholder);

                ModManager.GetModGalleryImage(this.profile.id, imageLocator,
                                              IMAGE_PREVIEW_SIZE, (t) => {
                                                  this.textureCache[imageFileName] = t;
                                                  isRepaintRequired = true;
                                              }, null);

                return this.textureCache[imageFileName];
            }
            // - Load -
            else
            {
                string imageSource = GetGalleryImageSource(index);

                byte[] data = null;
                Texture2D imageData = null;

                data = File.ReadAllBytes(imageSource);

                if(data != null)
                {
                    imageData = IOUtilities.ParseImageData(data);
                }

                this.textureCache.Add(imageFileName, imageData);
                return this.textureCache[imageFileName];
            }
        }

        // - Misc Functionality -
        private void ResetModMedia()
        {
            EditorUtilityExtensions.SetSerializedPropertyStringArray(
                youTubeURLsProp.FindPropertyRelative("value"), profile.media.youTubeURLs);
            youTubeURLsProp.FindPropertyRelative("isDirty").boolValue = false;

            EditorUtilityExtensions.SetSerializedPropertyStringArray(
                sketchfabURLsProp.FindPropertyRelative("value"), profile.media.sketchfabURLs);
            sketchfabURLsProp.FindPropertyRelative("isDirty").boolValue = false;

            // - Images -
            SerializedProperty galleryImagesArray = galleryImagesProp.FindPropertyRelative("value");

            galleryImagesArray.arraySize = profile.media.galleryImageLocators.Length;
            for(int i = 0; i < profile.media.galleryImageLocators.Length; ++i)
            {
                galleryImagesArray.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("fileName")
                    .stringValue = profile.media.galleryImageLocators[i].GetFileName();
                galleryImagesArray.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("url")
                    .stringValue = profile.media.galleryImageLocators[i].GetURL();
            }

            galleryImagesProp.FindPropertyRelative("isDirty").boolValue = false;
        }
    }
}

#endif
