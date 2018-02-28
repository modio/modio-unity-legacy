#if UNITY_EDITOR

#define ENABLE_STOCK

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public static class EditorModLayout
    {
        private const int SUMMARY_CHAR_LIMIT = 250;
        private const int DESCRIPTION_CHAR_MIN = 100;

        private static GUILayoutOption[] buttonLayout = new GUILayoutOption[]{ GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight) };

        public static void ModProfilePanel(SerializedProperty modInfoProp,
                                           Texture2D logoTexture,
                                           string logoSource,
                                           List<string> selectedTags,
                                           ref bool isTagsExpanded)
        {
            bool isNewMod = modInfoProp.FindPropertyRelative("_data.id").intValue <= 0;
            SerializedProperty modObjectProp = modInfoProp.FindPropertyRelative("_data");
            bool isUndoRequested = false;
            
            // ------[ LOGO ]------
            bool doBrowseLogo = false;

            if(logoTexture != null)
            {
                //TODO(@jackson): Make full-width

                Rect logoRect = EditorGUILayout.GetControlRect(false, 180.0f, null);
                EditorGUI.DrawPreviewTexture(new Rect((logoRect.width - 320.0f) * 0.5f, logoRect.y, 320.0f, logoRect.height),
                                             logoTexture, null, ScaleMode.ScaleAndCrop);
                doBrowseLogo |= GUI.Button(logoRect, "", GUI.skin.label);
            }

            EditorGUILayout.BeginHorizontal();
                doBrowseLogo = EditorGUILayoutExtensions.BrowseButton(logoSource, new GUIContent("Logo"));
                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
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

            // ------[ NAME ]------
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("name"),
                                              new GUIContent("Name"));
                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "name");
            }

            // ------[ NAME-ID/PROFILE URL ]------
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Profile URL");
                EditorGUILayout.LabelField("@", GUILayout.Width(13));
                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("name_id"),
                                              GUIContent.none);
                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "name_id");
            }
           
            // ------[ HOMEPAGE ]------
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("homepage"),
                                              new GUIContent("Homepage"));
                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "homepage");
            }

            // ------[ TAGS ]------
            EditorGUILayout.BeginHorizontal();
                isTagsExpanded = EditorGUILayout.Foldout(isTagsExpanded, "Tags", true);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            if(isTagsExpanded)
            {
                DisplayTagOptions(modInfoProp, selectedTags);
            }

            if(isUndoRequested)
            {
                ResetTags(modInfoProp);
            }

            // ------[ VISIBILITY ]------
            ModInfo.Visibility modVisibility = (ModInfo.Visibility)modObjectProp.FindPropertyRelative("visible").intValue;

            EditorGUILayout.BeginHorizontal();
                modVisibility = (ModInfo.Visibility)EditorGUILayout.EnumPopup("Visibility", modVisibility);
                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetIntField(modInfoProp, "visible");
            }
            else
            {
                modObjectProp.FindPropertyRelative("visible").intValue = (int)modVisibility;
            }

            // ------[ STOCK ]------
            #if ENABLE_STOCK
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Stock");

                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("stock"),
                                              GUIContent.none);//, GUILayout.Width(40));

                // TODO(@jackson): Change to checkbox
                EditorGUILayout.LabelField("0 = Unlimited", GUILayout.Width(80));

                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetIntField(modInfoProp, "stock");
            }
            #endif


            // --- Paragraph Text Inspection Settings ---
            Rect controlRect;
            bool wasWordWrapEnabled = GUI.skin.textField.wordWrap;
            GUI.skin.textField.wordWrap = true;


            // ------[ SUMMARY ]------
            SerializedProperty summaryProp = modObjectProp.FindPropertyRelative("summary");
            EditorGUILayout.BeginHorizontal();
                int charCount = summaryProp.stringValue.Length;

                EditorGUILayout.PrefixLabel("Summary");
                // GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("[" + (SUMMARY_CHAR_LIMIT - charCount).ToString()
                                           + " characters remaining]");

                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            controlRect = EditorGUILayout.GetControlRect(false, 130.0f, null);
            summaryProp.stringValue = EditorGUI.TextField(controlRect, summaryProp.stringValue);
            if(summaryProp.stringValue.Length > SUMMARY_CHAR_LIMIT)
            {
                summaryProp.stringValue = summaryProp.stringValue.Substring(0, SUMMARY_CHAR_LIMIT);
            }

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "summary");
            }

            // ------[ DESCRIPTION ]------
            SerializedProperty descriptionProp = modObjectProp.FindPropertyRelative("description");
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Description");
                // GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("[HTML Tags accepted]");

                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            controlRect = EditorGUILayout.GetControlRect(false, 127.0f, null);
            descriptionProp.stringValue = EditorGUI.TextField(controlRect, descriptionProp.stringValue);

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "description");
            }

            // TODO(@jackson): Dependencies
            
            // ------[ METADATA ]------
            SerializedProperty metadataProp = modObjectProp.FindPropertyRelative("metadata_blob");
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Metadata");
                
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            controlRect = EditorGUILayout.GetControlRect(false, 120.0f, null);
            metadataProp.stringValue = EditorGUI.TextField(controlRect, metadataProp.stringValue);

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "description");
            }

            // TODO(@jackson): MetadataKVP
        }

        public static void ModMediaPanel(SerializedProperty modInfoProp)
        {
            bool isNewMod = modInfoProp.FindPropertyRelative("_data.id").intValue <= 0;

            // --- Mod Media ---
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Media");
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    ResetModMedia(modInfoProp);
                }
            EditorGUILayout.EndHorizontal();

            DisplayModMedia(modInfoProp);

            // Images
            // Youtube
            // Sketchfab
        }

        // TODO(@jackson): Show all modfiles
        public static void ModfileManagementPanel(SerializedProperty buildLocationProp,
                                                   SerializedProperty modfileProfileProp,
                                                   SerializedProperty setPrimaryProp)
        {
            EditorGUILayout.LabelField("Build Info");

            if(EditorGUILayoutExtensions.BrowseButton(buildLocationProp.stringValue, new GUIContent("Build Location")))
            {
                EditorApplication.delayCall += () =>
                {
                    // TODO(@jackson): Allow folders?
                    string path = EditorUtility.OpenFilePanel("Set Build Location", "", "unity3d");
                    if (path.Length != 0)
                    {
                        buildLocationProp.stringValue = path;
                        buildLocationProp.serializedObject.ApplyModifiedProperties();
                    }
                };
            }

            EditorGUILayout.PropertyField(modfileProfileProp, GUIContent.none);

            EditorGUILayout.PropertyField(setPrimaryProp, new GUIContent("Set Primary"));
        }

        // TODO(@jackson): public static void ModTeamPanel()

        public static void ModServerDataPanel(SerializedProperty modInfoProp)
        {
            SerializedProperty modObjectProp = modInfoProp.FindPropertyRelative("_data");

            // --- Read-only Data ---
            using (new EditorGUI.DisabledScope(true))
            {
                int modId = modObjectProp.FindPropertyRelative("id").intValue;
                if(modId <= 0)
                {
                    EditorGUILayout.LabelField("ModIO ID",
                                               "Not yet uploaded");
                }
                else
                {
                    EditorGUILayout.LabelField("ModIO ID",
                                               modId.ToString());
                    
                    EditorGUILayout.LabelField("Submitted By",
                                               modObjectProp.FindPropertyRelative("submitted_by.username").stringValue);

                    ModInfo.Status modStatus = (ModInfo.Status)modObjectProp.FindPropertyRelative("status").intValue;
                    EditorGUILayout.LabelField("Status",
                                               modStatus.ToString());

                    string ratingSummaryDisplay
                        = modObjectProp.FindPropertyRelative("rating_summary.weighted_aggregate").floatValue.ToString("0.00")
                        + " aggregate score. (From "
                        + modObjectProp.FindPropertyRelative("rating_summary.total_ratings").intValue.ToString()
                        + " ratings)";

                    EditorGUILayout.LabelField("Rating Summary",
                                                ratingSummaryDisplay);

                    EditorGUILayout.LabelField("Date Uploaded",
                                               modObjectProp.FindPropertyRelative("date_added").intValue.ToString());
                    EditorGUILayout.LabelField("Date Updated",
                                               modObjectProp.FindPropertyRelative("date_updated").intValue.ToString());
                    EditorGUILayout.LabelField("Date Live",
                                               modObjectProp.FindPropertyRelative("date_live").intValue.ToString());
                }
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
        private static void DisplayTagOptions(SerializedProperty modInfoProp, List<string> selectedTags)
        {
            int tagsRemovedCount = 0;

            ++EditorGUI.indentLevel;
                foreach(GameTagOption tagOption in ModManager.gameInfo.taggingOptions)
                {
                    if(!tagOption.isHidden)
                    {
                        DisplayTagOption(modInfoProp, tagOption, selectedTags, ref tagsRemovedCount);
                    }
                }
            --EditorGUI.indentLevel;
        }
        private static void DisplayTagOption(SerializedProperty modInfoProp,
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

        // ---------[ MOD MEDIA ]---------
        private static bool isYouTubeExpanded = false;
        private static bool isSketchFabExpanded = false;
        private static bool isImagesExpanded = false;

        private static void DisplayModMedia(SerializedProperty modInfoProp)
        {
            ++EditorGUI.indentLevel;

                EditorGUILayoutExtensions.ArrayPropertyField(modInfoProp.FindPropertyRelative("_data.media.youtube"),
                                                             "YouTube Links", ref isYouTubeExpanded);
                EditorGUILayoutExtensions.ArrayPropertyField(modInfoProp.FindPropertyRelative("_data.media.sketchfab"),
                                                             "SketchFab Links", ref isSketchFabExpanded);
                EditorGUILayoutExtensions.ArrayPropertyField(modInfoProp.FindPropertyRelative("_data.media.images"),
                                                             "Gallery Images", ref isImagesExpanded);

            --EditorGUI.indentLevel;
        }

        private static void ResetModMedia(SerializedProperty modInfoProp)
        {
            SerializedProperty initialDataProp;
            SerializedProperty currentDataProp;

            // - YouTube -
            initialDataProp = modInfoProp.FindPropertyRelative("_initialData.media.youtube");
            currentDataProp = modInfoProp.FindPropertyRelative("_data.media.youtube");

            currentDataProp.arraySize = initialDataProp.arraySize;

            for(int i = 0; i < initialDataProp.arraySize; ++i)
            {
                currentDataProp.GetArrayElementAtIndex(i).stringValue
                    = initialDataProp.GetArrayElementAtIndex(i).stringValue;
            }

            // - SketchFab -
            initialDataProp = modInfoProp.FindPropertyRelative("_initialData.media.sketchfab");
            currentDataProp = modInfoProp.FindPropertyRelative("_data.media.sketchfab");

            currentDataProp.arraySize = initialDataProp.arraySize;

            for(int i = 0; i < initialDataProp.arraySize; ++i)
            {
                currentDataProp.GetArrayElementAtIndex(i).stringValue
                    = initialDataProp.GetArrayElementAtIndex(i).stringValue;
            }

            // - Image Gallery -
            initialDataProp = modInfoProp.FindPropertyRelative("_initialData.media.images");
            currentDataProp = modInfoProp.FindPropertyRelative("_data.media.images");

            currentDataProp.arraySize = initialDataProp.arraySize;

            for(int i = 0; i < initialDataProp.arraySize; ++i)
            {
                currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("filename").stringValue
                    = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("filename").stringValue;
                currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("original").stringValue
                    = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("original").stringValue;
                currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("thumb_320x180").stringValue
                    = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("thumb_320x180").stringValue;
            }
        }
    }
}

#endif