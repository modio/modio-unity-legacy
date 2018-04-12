#if UNITY_EDITOR
// #define ENABLE_MOD_STOCK

using System.Collections.Generic;
using Path = System.IO.Path;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ModIO
{
    // TODO(@jackson): Use cache originalModLogo and use Unity built in image inspector?
    public class ModProfileInfoViewPart : IModProfileViewPart
    {
        private const ModLogoVersion LOGO_PREVIEW_VERSION = ModLogoVersion.Thumbnail_320x180;
        private const float LOGO_PREVIEW_WIDTH = 320;
        private const float LOGO_PREVIEW_HEIGHT = 180;
        private const int SUMMARY_CHAR_LIMIT = 250;
        private const int DESCRIPTION_CHAR_MIN = 100;

        private bool isUndoEnabled = false;

        public bool isDisabled { get { return false; } }

        // ------[ EDITOR CACHING ]------
        private SerializedProperty editableProfileProperty;
        private ModProfile profile;

        // - Logo -
        private SerializedProperty logoProperty;
        private Texture2D logoTexture;
        private string logoLocation;

        // - Tags -
        private bool isTagsExpanded;

        // ------[ INITIALIZATION ]------
        public void OnEnable(SerializedProperty serializedEditableModProfile, ModProfile baseProfile)
        {
            this.editableProfileProperty = serializedEditableModProfile;
            this.profile = baseProfile;

            isTagsExpanded = false;

            // - Configure Properties -
            logoProperty = editableProfileProperty.FindPropertyRelative("logoLocator");

            // - Load Textures -
            if(logoProperty.FindPropertyRelative("isDirty").boolValue == true)
            {
                logoLocation = logoProperty.FindPropertyRelative("value.source").stringValue;
                Utility.TryLoadTextureFromFile(logoLocation, out logoTexture);
            }
            else if(profile != null)
            {
                logoLocation = profile.logoLocator.GetVersionSource(LOGO_PREVIEW_VERSION);
                logoTexture = ModManager.LoadOrDownloadModLogo(profile.id, LOGO_PREVIEW_VERSION);
            }
            else
            {
                logoLocation = string.Empty;
                logoTexture = null;
            }

            // - Handle Updates -
            ModManager.OnModLogoUpdated += OnModLogoUpdated;
        }

        public void OnDisable()
        {
            ModManager.OnModLogoUpdated -= OnModLogoUpdated;
        }

        // ------[ ONUPDATE ]------
        public void OnUpdate() {}

        private void OnModLogoUpdated(int modId, ModLogoVersion version, Texture2D texture)
        {
            if(profile != null
               && profile.id == modId
               && version == LOGO_PREVIEW_VERSION
               && logoProperty.FindPropertyRelative("isDirty").boolValue == false)
            {
                logoTexture = texture;
            }
        }

        // ------[ ONGUI ]------
        public void OnGUI()
        {
            LayoutLogoField();
            LayoutTagsField();

            // // TODO(@jackson): Move textures to tempcache
            // Texture2D logoTexture = sceneData.logoTexture;
            // string logoSource = sceneData.modProfileEdits.logoLocator.value.source;
            // List<string> selectedTags = new List<string>(sceneData.modProfileEdits.tags.value);
            // isUndoEnabled = sceneData.modId > 0;

            // SerializedObject serializedSceneData = new SerializedObject(sceneData);
            // SerializedProperty modProfileProp = serializedSceneData.FindProperty("modData");
 
            // LayoutNameField(modProfileProp);
            // LayoutNameIDField(modProfileProp);
            // LayoutHomepageField(modProfileProp);
            // LayoutVisibilityField(modProfileProp);
            // LayoutStockField(modProfileProp);
            // LayoutSummaryField(modProfileProp);
            // LayoutDescriptionField(modProfileProp);
            // // LayoutModDependenciesField(modProfileProp);
            // LayoutMetadataField(modProfileProp);
            // // LayoutMetadataKVPField(modProfileProp);
        }

        // ---------[ LAYOUT FUNCTIONS ]---------
        protected virtual void LayoutLogoField()
        {
            bool doBrowse = false;

            // - Browse Field -
            EditorGUILayout.BeginHorizontal();
                doBrowse |= EditorGUILayoutExtensions.BrowseButton(logoLocation,
                                                                   new GUIContent("Logo"));
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton(isUndoEnabled);
            EditorGUILayout.EndHorizontal();

            // - Draw Texture -
            if(logoTexture != null)
            {
                //TODO(@jackson): Make full-width
                Rect logoRect = EditorGUILayout.GetControlRect(false,
                                                               LOGO_PREVIEW_HEIGHT,
                                                               null);
                EditorGUI.DrawPreviewTexture(new Rect((logoRect.width - LOGO_PREVIEW_WIDTH) * 0.5f,
                                                      logoRect.y,
                                                      LOGO_PREVIEW_WIDTH,
                                                      logoRect.height),
                                             logoTexture,
                                             null,
                                             ScaleMode.ScaleAndCrop);
                doBrowse |= GUI.Button(logoRect, "", GUI.skin.label);
            }

            if(doBrowse)
            {
                EditorApplication.delayCall += () =>
                {
                    Texture2D newLogoTexture;

                    // TODO(@jackson): Add other file-types
                    string path = EditorUtility.OpenFilePanel("Select Mod Logo", "", "png");
                    if (path.Length != 0
                        && Utility.TryLoadTextureFromFile(path, out newLogoTexture))
                    {
                        logoProperty.FindPropertyRelative("value.source").stringValue = path;
                        logoProperty.FindPropertyRelative("value.fileName").stringValue = Path.GetFileName(path);
                        logoProperty.FindPropertyRelative("isDirty").boolValue = true;
                        logoProperty.serializedObject.ApplyModifiedProperties();

                        logoTexture = newLogoTexture;
                        logoLocation = path;
                    }
                };
            }

            if(isUndoRequested)
            {
                logoProperty.FindPropertyRelative("value/source").stringValue = "";
            }
        }

        protected virtual void LayoutTagsField()
        {
            using(new EditorGUI.DisabledScope(ModManager.gameProfile == null))
            {
                EditorGUILayout.BeginHorizontal();
                    this.isTagsExpanded = EditorGUILayout.Foldout(this.isTagsExpanded, "Tags", true);
                    GUILayout.FlexibleSpace();
                    bool isUndoRequested = EditorGUILayoutExtensions.UndoButton(isUndoEnabled);
                EditorGUILayout.EndHorizontal();

                if(this.isTagsExpanded)
                {
                    if(ModManager.gameProfile == null)
                    {
                        EditorGUILayout.HelpBox("The Game's Profile is not yet loaded, and thus tags cannot be displayed. Please wait...", MessageType.Warning);
                    }
                    else if(ModManager.gameProfile.taggingOptions.Count == 0)
                    {
                        EditorGUILayout.HelpBox("The developers of "
                                                + ModManager.gameProfile.name
                                                + " have not designated any tagging options",
                                                MessageType.Info);
                    }
                    else
                    {
                        string[] selectedTags = Utility.SerializedPropertyToStringArray(editableProfileProperty.FindPropertyRelative("tags.value"));

                        int tagsRemovedCount = 0;

                        ++EditorGUI.indentLevel;
                            foreach(ModTagCategory tagCategory in ModManager.gameProfile.taggingOptions)
                            {
                                if(!tagCategory.isHidden)
                                {
                                    EditorGUILayout.LabelField(tagCategory.name);
                                    // LayoutTagCategory(tagCategory, selectedTags, ref tagsRemovedCount);
                                }
                            }
                        --EditorGUI.indentLevel;
                    }

                    if(isUndoRequested)
                    {
                        // ResetTags(modProfileProp);
                    }
                }
            }
        }

        protected virtual void LayoutNameField(SerializedProperty modProfileProp)
        {
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(modProfileProp.FindPropertyRelative("_data.name"),
                                              new GUIContent("Name"));
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton();
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modProfileProp, "name");
            }
        }

        protected virtual void LayoutNameIDField(SerializedProperty modProfileProp)
        {
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Profile URL");
                EditorGUILayout.LabelField("@", GUILayout.Width(13));
                EditorGUILayout.PropertyField(modProfileProp.FindPropertyRelative("_data.name_id"),
                                              GUIContent.none);
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton();
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modProfileProp, "name_id");
            }
        }

        protected virtual void LayoutHomepageField(SerializedProperty modProfileProp)
        {
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(modProfileProp.FindPropertyRelative("_data.homepage"),
                                              new GUIContent("Homepage"));
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton();
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modProfileProp, "homepage");
            }
        }

        protected virtual void LayoutTagCategory(SerializedProperty modProfileProp,
                                                 ModTagCategory tagCategory,
                                                 List<string> selectedTags,
                                                 ref int tagsRemovedCount)
        {
            // EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(tagCategory.name);

            EditorGUILayout.BeginVertical();
                if(!tagCategory.isFlag)
                {
                    string selectedTag = "";
                    foreach(string tag in tagCategory.tags)
                    {
                        if(selectedTags.Contains(tag))
                        {
                            selectedTag = tag;
                        }
                    }

                    foreach(string tag in tagCategory.tags)
                    {
                        bool isSelected = (tag == selectedTag);
                        isSelected = EditorGUILayout.Toggle(tag, isSelected, EditorStyles.radioButton);

                        if(isSelected && tag != selectedTag)
                        {
                            if(selectedTag != "")
                            {
                                RemoveTagFromMod(modProfileProp, selectedTags.IndexOf(selectedTag) - tagsRemovedCount);
                                ++tagsRemovedCount;
                            }

                            AddTagToMod(modProfileProp, tag);
                        }
                    }
                }
                else
                {
                    foreach(string tag in tagCategory.tags)
                    {
                        bool wasSelected = selectedTags.Contains(tag);
                        bool isSelected = EditorGUILayout.Toggle(tag, wasSelected);

                        if(wasSelected != isSelected)
                        {
                            if(isSelected)
                            {
                                AddTagToMod(modProfileProp, tag);
                            }
                            else
                            {
                                RemoveTagFromMod(modProfileProp, selectedTags.IndexOf(tag) - tagsRemovedCount);
                                ++tagsRemovedCount;
                            }
                        }
                    }
                }
            EditorGUILayout.EndVertical();
            // EditorGUILayout.EndHorizontal();
        }

        protected virtual void LayoutVisibilityField(SerializedProperty modProfileProp)
        {
            ModVisibility modVisibility = (ModVisibility)modProfileProp.FindPropertyRelative("_data.visible").intValue;

            EditorGUILayout.BeginHorizontal();
                    modVisibility = (ModVisibility)EditorGUILayout.EnumPopup("Visibility", modVisibility);             bool isUndoRequested = EditorGUILayoutExtensions.UndoButton();
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetIntField(modProfileProp, "visible");
            }
            else
            {
                modProfileProp.FindPropertyRelative("_data.visible").intValue = (int)modVisibility;
            }
        }

        protected virtual void LayoutStockField(SerializedProperty modProfileProp)
        {
            #if ENABLE_MOD_STOCK
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Stock");

                EditorGUILayout.PropertyField(modProfileProp.FindPropertyRelative("_data.stock"),
                                              GUIContent.none);//, GUILayout.Width(40));

                // TODO(@jackson): Change to checkbox
                EditorGUILayout.LabelField("0 = Unlimited", GUILayout.Width(80));
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton();
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetIntField(modProfileProp, "stock");
            }
            #endif
        }

        protected virtual void LayoutSummaryField(SerializedProperty modProfileProp)
        {
            SerializedProperty summaryProp = modProfileProp.FindPropertyRelative("_data.summary");
            EditorGUILayout.BeginHorizontal();
                int charCount = summaryProp.stringValue.Length;

                EditorGUILayout.PrefixLabel("Summary");
                EditorGUILayout.LabelField("[" + (SUMMARY_CHAR_LIMIT - charCount).ToString()
                                           + " characters remaining]");
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton();
            EditorGUILayout.EndHorizontal();

            summaryProp.stringValue = EditorGUILayoutExtensions.MultilineTextField(summaryProp.stringValue);

            if(summaryProp.stringValue.Length > SUMMARY_CHAR_LIMIT)
            {
                summaryProp.stringValue = summaryProp.stringValue.Substring(0, SUMMARY_CHAR_LIMIT);
            }

            if(isUndoRequested)
            {
                ResetStringField(modProfileProp, "summary");
            }
        }

        protected virtual void LayoutDescriptionField(SerializedProperty modProfileProp)
        {
            SerializedProperty descriptionProp = modProfileProp.FindPropertyRelative("_data.description");
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Description");
                EditorGUILayout.LabelField("[HTML Tags accepted]");
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton();
            EditorGUILayout.EndHorizontal();

            descriptionProp.stringValue = EditorGUILayoutExtensions.MultilineTextField(descriptionProp.stringValue);

            if(isUndoRequested)
            {
                ResetStringField(modProfileProp, "description");
            }
        }

        protected virtual void LayoutModDependenciesField(SerializedProperty modProfileProp)
        {
            // TODO(@jackson): Dependencies
        }

        protected virtual void LayoutMetadataField(SerializedProperty modProfileProp)
        {
            SerializedProperty metadataProp = modProfileProp.FindPropertyRelative("_data.metadata_blob");
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Metadata");
                GUILayout.FlexibleSpace();
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton();
            EditorGUILayout.EndHorizontal();

            metadataProp.stringValue = EditorGUILayoutExtensions.MultilineTextField(metadataProp.stringValue);

            if(isUndoRequested)
            {
                ResetStringField(modProfileProp, "description");
            }
        }

        protected virtual void LayoutUploadButton(EditorSceneData sceneData)
        {
            if(GUILayout.Button("Save To Server"))
            {
                EditorApplication.delayCall += () => UploadModProfile(sceneData);
            }
        }

        // ---------[ RESET FUNCTIONS ]---------
        private static void ResetStringField(SerializedProperty modProfileProp, string fieldName)
        {
            modProfileProp.FindPropertyRelative("_data").FindPropertyRelative(fieldName).stringValue
            = modProfileProp.FindPropertyRelative("_initialData").FindPropertyRelative(fieldName).stringValue;
        }

        private static void ResetIntField(SerializedProperty modProfileProp, string fieldName)
        {
            modProfileProp.FindPropertyRelative("_data").FindPropertyRelative(fieldName).intValue
            = modProfileProp.FindPropertyRelative("_initialData").FindPropertyRelative(fieldName).intValue;
        }


        // ---------[ TAG OPTIONS ]---------
        //  TODO(@jackson): Work out a better way of displaying this
        private static void AddTagToMod(SerializedProperty modProfileProp, string tag)
        {
            SerializedProperty tagsArrayProp = modProfileProp.FindPropertyRelative("_data.tags");
            int newIndex = tagsArrayProp.arraySize;
            ++tagsArrayProp.arraySize;

            tagsArrayProp.GetArrayElementAtIndex(newIndex).FindPropertyRelative("name").stringValue = tag;
            tagsArrayProp.GetArrayElementAtIndex(newIndex).FindPropertyRelative("date_added").intValue = TimeStamp.Now().AsServerTimeStamp();
        }

        private static void RemoveTagFromMod(SerializedProperty modProfileProp, int tagIndex)
        {
            SerializedProperty tagsArrayProp = modProfileProp.FindPropertyRelative("_data.tags");

            tagsArrayProp.DeleteArrayElementAtIndex(tagIndex);
        }

        private static void ResetTags(SerializedProperty modProfileProp)
        {
            SerializedProperty initTagsProp = modProfileProp.FindPropertyRelative("_initialData.tags");
            SerializedProperty currentTagsProp = modProfileProp.FindPropertyRelative("_data.tags");

            currentTagsProp.arraySize = initTagsProp.arraySize;

            for(int i = 0; i < initTagsProp.arraySize; ++i)
            {
                currentTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue
                    = initTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                currentTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("date_added").intValue
                    = initTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("date_added").intValue;
            }
        }

        // ---------[ UPLOADING ]---------
        private void UploadModProfile(EditorSceneData sceneData)
        {
            throw new System.NotImplementedException();

            // if(EditorSceneManager.EnsureUntitledSceneHasBeenSaved("The scene needs to be saved before uploading mod data"))
            // {
            //     EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            //     isUploading = true;

            //     if(sceneData.modId == 0)
            //     {
            //         ModManager.SubmitNewMod(sceneData.modProfileEdits,
            //                                  (mod) =>
            //                                  {
            //                                     // TODO(@jackson): Mark Dirty -> Save
            //                                     sceneData.modInfo = EditableModProfile.FromModProfile(mod);
            //                                     isUploading = false;
            //                                  },
            //                                  (e) =>
            //                                  {
            //                                     isUploading = false;
            //                                     EditorUtility.DisplayDialog("Mod Profile submission failed",
            //                                                                 e.message,
            //                                                                 "Ok");
            //                                  });
            //     }
            //     else
            //     {
            //         ModManager.SubmitModChanges(sceneData.modId,
            //                                  sceneData.modProfileEdits,
            //                                  (mod) =>
            //                                  {
            //                                     // TODO(@jackson): Mark Dirty -> Save
            //                                     sceneData.modInfo = EditableModProfile.FromModProfile(mod);
            //                                     isUploading = false;
            //                                  },
            //                                  (e) =>
            //                                  {
            //                                     isUploading = false;
            //                                     EditorUtility.DisplayDialog("Mod Profile submission failed",
            //                                                                 e.message,
            //                                                                 "Ok");
            //                                  });
            //     }
            // }
        }
    }
}

#endif
