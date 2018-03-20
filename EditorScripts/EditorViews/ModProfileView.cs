#if UNITY_EDITOR
// #define ENABLE_MOD_STOCK

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ModIO
{
    public class ModProfileView : ISceneEditorView
    {
        private const int SUMMARY_CHAR_LIMIT = 250;
        private const int DESCRIPTION_CHAR_MIN = 100;

        // ---------[ FIELDS ]---------
        private bool isTagsExpanded = false;
        private bool isUploading = false;
        private bool isUndoEnabled = false;

        // - ISceneEditorView Interface -
        public string GetViewTitle() { return "Profile"; }
        public void OnEnable()
        {
            isTagsExpanded = false;
        }
        public void OnDisable() {}
        public virtual bool IsViewDisabled()
        {
            return isUploading;
        }

        public void OnGUI(EditorSceneData sceneData)
        {
            using(new EditorGUI.DisabledScope(this.IsViewDisabled()))
            {
                this.OnGUIInner(sceneData);
            }
        }

        protected virtual void OnGUIInner(EditorSceneData sceneData)
        {
            // TODO(@jackson): Move textures to tempcache
            Texture2D logoTexture = sceneData.GetModLogoTexture();
            string logoSource = sceneData.GetModLogoSource();
            List<string> selectedTags = new List<string>(sceneData.modInfo.GetTagNames());
            isUndoEnabled = sceneData.modInfo.id > 0;

            SerializedObject serializedSceneData = new SerializedObject(sceneData);
            SerializedProperty modInfoProp = serializedSceneData.FindProperty("modInfo");
 
            LayoutLogoTexture(modInfoProp, logoTexture, logoSource);
            LayoutNameField(modInfoProp);
            LayoutNameIDField(modInfoProp);
            LayoutHomepageField(modInfoProp);
            LayoutTagsField(modInfoProp, selectedTags, ref isTagsExpanded);
            LayoutVisibilityField(modInfoProp);
            LayoutStockField(modInfoProp);
            LayoutSummaryField(modInfoProp);
            LayoutDescriptionField(modInfoProp);
            // LayoutModDependenciesField(modInfoProp);
            LayoutMetadataField(modInfoProp);
            // LayoutMetadataKVPField(modInfoProp);

            serializedSceneData.ApplyModifiedProperties();

            LayoutUploadButton(sceneData);
        }

        // ---------[ LAYOUT FUNCTIONS ]---------
        protected virtual void LayoutLogoTexture(SerializedProperty modInfoProp,
                                                 Texture2D cachedTexture,
                                                 string browseButtonContent)
        {
            bool doBrowseLogo = false;

            // - Draw Texture -
            if(cachedTexture != null)
            {
                //TODO(@jackson): Make full-width
                Rect logoRect = EditorGUILayout.GetControlRect(false, 180.0f, null);
                EditorGUI.DrawPreviewTexture(new Rect((logoRect.width - 320.0f) * 0.5f, logoRect.y, 320.0f, logoRect.height),
                                             cachedTexture, null, ScaleMode.ScaleAndCrop);
                doBrowseLogo |= GUI.Button(logoRect, "", GUI.skin.label);
            }

            // - Browse Field -
            EditorGUILayout.BeginHorizontal();
                doBrowseLogo |= EditorGUILayoutExtensions.BrowseButton(browseButtonContent, new GUIContent("Logo"));
                bool isUndoRequested = LayoutUndoButton();
            EditorGUILayout.EndHorizontal();

            if(doBrowseLogo)
            {
                EditorApplication.delayCall += () =>
                {
                    // TODO(@jackson): Add other file-types
                    string path = EditorUtility.OpenFilePanel("Select Mod Logo", "", "png");
                    if (path.Length != 0)
                    {
                        modInfoProp.FindPropertyRelative("logoFilepath").stringValue = path;
                        modInfoProp.serializedObject.ApplyModifiedProperties();
                    }
                };
            }

            if(isUndoRequested)
            {
                modInfoProp.FindPropertyRelative("logoFilepath").stringValue = "";
            }
        }

        protected virtual void LayoutNameField(SerializedProperty modInfoProp)
        {
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(modInfoProp.FindPropertyRelative("_data.name"),
                                              new GUIContent("Name"));
                bool isUndoRequested = LayoutUndoButton();
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "name");
            }
        }

        protected virtual void LayoutNameIDField(SerializedProperty modInfoProp)
        {
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Profile URL");
                EditorGUILayout.LabelField("@", GUILayout.Width(13));
                EditorGUILayout.PropertyField(modInfoProp.FindPropertyRelative("_data.name_id"),
                                              GUIContent.none);
                bool isUndoRequested = LayoutUndoButton();
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "name_id");
            }
        }

        protected virtual void LayoutHomepageField(SerializedProperty modInfoProp)
        {
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(modInfoProp.FindPropertyRelative("_data.homepage"),
                                              new GUIContent("Homepage"));
                bool isUndoRequested = LayoutUndoButton();
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "homepage");
            }
        }

        protected virtual void LayoutTagsField(SerializedProperty modInfoProp,
                                               List<string> selectedTags,
                                               ref bool isExpanded)
        {
            EditorGUILayout.BeginHorizontal();
                isExpanded = EditorGUILayout.Foldout(isExpanded, "Tags", true);
                GUILayout.FlexibleSpace();
                bool isUndoRequested = LayoutUndoButton();
            EditorGUILayout.EndHorizontal();

            if(isExpanded)
            {
                int tagsRemovedCount = 0;

                ++EditorGUI.indentLevel;
                    foreach(GameTagOption tagOption in ModManager.gameInfo.taggingOptions)
                    {
                        if(!tagOption.isHidden)
                        {
                            LayoutTagOption(modInfoProp, tagOption, selectedTags, ref tagsRemovedCount);
                        }
                    }
                --EditorGUI.indentLevel;
            }

            if(isUndoRequested)
            {
                ResetTags(modInfoProp);
            }
        }

        protected virtual void LayoutTagOption(SerializedProperty modInfoProp,
                                               GameTagOption tagOption,
                                               List<string> selectedTags,
                                               ref int tagsRemovedCount)
        {
            // EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(tagOption.name);

            EditorGUILayout.BeginVertical();
                if(tagOption.tagType == GameTagOption.TagType.SingleValue)
                {
                    string selectedTag = "";
                    foreach(string tag in tagOption.tags)
                    {
                        if(selectedTags.Contains(tag))
                        {
                            selectedTag = tag;
                        }
                    }

                    foreach(string tag in tagOption.tags)
                    {
                        bool isSelected = (tag == selectedTag);
                        isSelected = EditorGUILayout.Toggle(tag, isSelected, EditorStyles.radioButton);

                        if(isSelected && tag != selectedTag)
                        {
                            if(selectedTag != "")
                            {
                                RemoveTagFromMod(modInfoProp, selectedTags.IndexOf(selectedTag) - tagsRemovedCount);
                                ++tagsRemovedCount;
                            }

                            AddTagToMod(modInfoProp, tag);
                        }
                    }
                }
                else
                {
                    foreach(string tag in tagOption.tags)
                    {
                        bool wasSelected = selectedTags.Contains(tag);
                        bool isSelected = EditorGUILayout.Toggle(tag, wasSelected);

                        if(wasSelected != isSelected)
                        {
                            if(isSelected)
                            {
                                AddTagToMod(modInfoProp, tag);
                            }
                            else
                            {
                                RemoveTagFromMod(modInfoProp, selectedTags.IndexOf(tag) - tagsRemovedCount);
                                ++tagsRemovedCount;
                            }
                        }
                    }
                }
            EditorGUILayout.EndVertical();
            // EditorGUILayout.EndHorizontal();
        }

        protected virtual void LayoutVisibilityField(SerializedProperty modInfoProp)
        {
            ModInfo.Visibility modVisibility = (ModInfo.Visibility)modInfoProp.FindPropertyRelative("_data.visible").intValue;

            EditorGUILayout.BeginHorizontal();
                    modVisibility = (ModInfo.Visibility)EditorGUILayout.EnumPopup("Visibility", modVisibility);             bool isUndoRequested = LayoutUndoButton();
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetIntField(modInfoProp, "visible");
            }
            else
            {
                modInfoProp.FindPropertyRelative("_data.visible").intValue = (int)modVisibility;
            }
        }

        protected virtual void LayoutStockField(SerializedProperty modInfoProp)
        {
            #if ENABLE_MOD_STOCK
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Stock");

                EditorGUILayout.PropertyField(modInfoProp.FindPropertyRelative("_data.stock"),
                                              GUIContent.none);//, GUILayout.Width(40));

                // TODO(@jackson): Change to checkbox
                EditorGUILayout.LabelField("0 = Unlimited", GUILayout.Width(80));
                bool isUndoRequested = LayoutUndoButton();
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetIntField(modInfoProp, "stock");
            }
            #endif
        }

        protected virtual void LayoutSummaryField(SerializedProperty modInfoProp)
        {
            SerializedProperty summaryProp = modInfoProp.FindPropertyRelative("_data.summary");
            EditorGUILayout.BeginHorizontal();
                int charCount = summaryProp.stringValue.Length;

                EditorGUILayout.PrefixLabel("Summary");
                EditorGUILayout.LabelField("[" + (SUMMARY_CHAR_LIMIT - charCount).ToString()
                                           + " characters remaining]");
                bool isUndoRequested = LayoutUndoButton();
            EditorGUILayout.EndHorizontal();

            summaryProp.stringValue = EditorGUILayoutExtensions.MultilineTextField(summaryProp.stringValue);

            if(summaryProp.stringValue.Length > SUMMARY_CHAR_LIMIT)
            {
                summaryProp.stringValue = summaryProp.stringValue.Substring(0, SUMMARY_CHAR_LIMIT);
            }

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "summary");
            }
        }

        protected virtual void LayoutDescriptionField(SerializedProperty modInfoProp)
        {
            SerializedProperty descriptionProp = modInfoProp.FindPropertyRelative("_data.description");
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Description");
                EditorGUILayout.LabelField("[HTML Tags accepted]");
                bool isUndoRequested = LayoutUndoButton();
            EditorGUILayout.EndHorizontal();

            descriptionProp.stringValue = EditorGUILayoutExtensions.MultilineTextField(descriptionProp.stringValue);

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "description");
            }
        }

        protected virtual void LayoutModDependenciesField(SerializedProperty modInfoProp)
        {
            // TODO(@jackson): Dependencies
        }

        protected virtual void LayoutMetadataField(SerializedProperty modInfoProp)
        {
            SerializedProperty metadataProp = modInfoProp.FindPropertyRelative("_data.metadata_blob");
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Metadata");
                GUILayout.FlexibleSpace();
                bool isUndoRequested = LayoutUndoButton();
            EditorGUILayout.EndHorizontal();

            metadataProp.stringValue = EditorGUILayoutExtensions.MultilineTextField(metadataProp.stringValue);

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "description");
            }
        }

        protected virtual void LayoutUploadButton(EditorSceneData sceneData)
        {
            if(GUILayout.Button("Save To Server"))
            {
                EditorApplication.delayCall += () => UploadModInfo(sceneData);
            }
        }

        protected virtual bool LayoutUndoButton()
        {
            using (new EditorGUI.DisabledScope(isUndoEnabled))
            {
                return EditorGUILayoutExtensions.UndoButton();
            }
        }

        // ---------[ RESET FUNCTIONS ]---------
        private static void ResetStringField(SerializedProperty modInfoProp, string fieldName)
        {
            modInfoProp.FindPropertyRelative("_data").FindPropertyRelative(fieldName).stringValue
            = modInfoProp.FindPropertyRelative("_initialData").FindPropertyRelative(fieldName).stringValue;
        }

        private static void ResetIntField(SerializedProperty modInfoProp, string fieldName)
        {
            modInfoProp.FindPropertyRelative("_data").FindPropertyRelative(fieldName).intValue
            = modInfoProp.FindPropertyRelative("_initialData").FindPropertyRelative(fieldName).intValue;
        }


        // ---------[ TAG OPTIONS ]---------
        //  TODO(@jackson): Work out a better way of displaying this
        private static void AddTagToMod(SerializedProperty modInfoProp, string tag)
        {
            SerializedProperty tagsArrayProp = modInfoProp.FindPropertyRelative("_data.tags");
            int newIndex = tagsArrayProp.arraySize;
            ++tagsArrayProp.arraySize;

            tagsArrayProp.GetArrayElementAtIndex(newIndex).FindPropertyRelative("name").stringValue = tag;
            tagsArrayProp.GetArrayElementAtIndex(newIndex).FindPropertyRelative("date_added").intValue = TimeStamp.Now().AsServerTimeStamp();
        }

        private static void RemoveTagFromMod(SerializedProperty modInfoProp, int tagIndex)
        {
            SerializedProperty tagsArrayProp = modInfoProp.FindPropertyRelative("_data.tags");

            tagsArrayProp.DeleteArrayElementAtIndex(tagIndex);
        }

        private static void ResetTags(SerializedProperty modInfoProp)
        {
            SerializedProperty initTagsProp = modInfoProp.FindPropertyRelative("_initialData.tags");
            SerializedProperty currentTagsProp = modInfoProp.FindPropertyRelative("_data.tags");

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
        private void UploadModInfo(EditorSceneData sceneData)
        {
            if(EditorSceneManager.EnsureUntitledSceneHasBeenSaved("The scene needs to be saved before uploading mod data"))
            {
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

                isUploading = true;

                if(sceneData.modId == 0)
                {
                    ModManager.SubmitNewMod(sceneData.modData,
                                             sceneData.modInfo.unsubmittedLogoFilepath,
                                             sceneData.modInfo.GetTagNames(),
                                             (mod) =>
                                             {
                                                // TODO(@jackson): Mark Dirty -> Save
                                                sceneData.modInfo = EditableModInfo.FromModInfo(mod);
                                                isUploading = false;
                                             },
                                             (e) =>
                                             {
                                                isUploading = false;
                                                EditorUtility.DisplayDialog("Mod Profile submission failed",
                                                                            e.message,
                                                                            "Ok");
                                             });
                }
                else
                {
                    ModManager.SubmitModChanges(sceneData.modId,
                                             sceneData.modData,
                                             sceneData.modInfo,
                                             (mod) =>
                                             {
                                                // TODO(@jackson): Mark Dirty -> Save
                                                sceneData.modInfo = EditableModInfo.FromModInfo(mod);
                                                isUploading = false;
                                             },
                                             (e) =>
                                             {
                                                isUploading = false;
                                                EditorUtility.DisplayDialog("Mod Profile submission failed",
                                                                            e.message,
                                                                            "Ok");
                                             });
                }
            }
        }
    }
}

#endif
