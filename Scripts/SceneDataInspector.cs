#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ModIO
{
    [CustomEditor(typeof(EditorSceneData))]
    public class SceneDataInspector : Editor
    {
        private const int SUMMARY_CHAR_LIMIT = 250;


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SceneDataInspector.DisplayAsObject(serializedObject);

            serializedObject.ApplyModifiedProperties();
        }

        public static void DisplayAsObject(SerializedObject serializedSceneData)
        {
            EditorSceneData sceneData = serializedSceneData.targetObject as EditorSceneData;
            Debug.Assert(sceneData != null);

            SerializedProperty modInfoProp = serializedSceneData.FindProperty("modInfo");

            Texture2D logoTexture = sceneData.GetModLogoTexture();
            string logoSource = sceneData.GetModLogoSource();

            List<string> selectedTags = new List<string>(sceneData.modInfo.GetTagNames());

            DisplayInner(modInfoProp, logoTexture, logoSource, selectedTags);
        }

        private static void DisplayInner(SerializedProperty modInfoProp,
                                         Texture2D logoTexture,
                                         string logoSource,
                                         List<string> selectedTags)
        {
            bool isNewMod = modInfoProp.FindPropertyRelative("_data").FindPropertyRelative("id").intValue <= 0;

            string logoSourceDisplay = (logoSource == "" ? "Browse..." : logoSource);
            SerializedProperty modObjectProp = modInfoProp.FindPropertyRelative("_data");
            bool isUndoRequested = false;
            GUILayoutOption[] buttonLayout = new GUILayoutOption[]{ GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight) };

            // - Name -
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


            // - Name ID -
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("name_id"),
                                              new GUIContent("Name-ID"));
                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "name_id");
            }


            // - Visibility -
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

            // - Homepage -
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

            // - Stock -
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Stock");

                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("stock"),
                                              new GUIContent(""));//, GUILayout.Width(40));

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

            // - Logo -
            bool doBrowseLogo = false;

            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Logo");

                if(Event.current.type == EventType.Layout)
                {
                    EditorGUILayout.TextField(logoSourceDisplay);
                }
                else
                {
                    doBrowseLogo = GUILayout.Button(logoSourceDisplay, GUI.skin.textField);
                }

                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            if(logoTexture != null)
            {
                Rect logoRect = EditorGUILayout.GetControlRect(false, 180.0f, null);
                EditorGUI.DrawPreviewTexture(new Rect((logoRect.width - 320.0f) * 0.5f, logoRect.y, 320.0f, logoRect.height),
                                             logoTexture, null, ScaleMode.ScaleAndCrop);
                doBrowseLogo |= GUI.Button(logoRect, "", GUI.skin.label);
            }

            if(isUndoRequested)
            {
                modInfoProp.FindPropertyRelative("logoFilepath").stringValue = "";
            }


            // --- Paragraph Text Inspection Settings ---
            Rect controlRect;
            bool wasWordWrapEnabled = GUI.skin.textField.wordWrap;
            GUI.skin.textField.wordWrap = true;

            // - Summary -
            SerializedProperty summaryProp = modObjectProp.FindPropertyRelative("summary");
            EditorGUILayout.BeginHorizontal();
                int charCount = summaryProp.stringValue.Length;

                EditorGUILayout.PrefixLabel("Summary");
                GUILayout.FlexibleSpace();
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

            // - Description -
            SerializedProperty descriptionProp = modObjectProp.FindPropertyRelative("description");
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Description");
                GUILayout.FlexibleSpace();
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

            // - Metadata -
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


            // --- Tags ---
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Tags");
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
                }
            EditorGUILayout.EndHorizontal();

            DisplayTagOptions(modInfoProp, selectedTags);

            if(isUndoRequested)
            {
                ResetTags(modInfoProp);
            }

            if(isUndoRequested)
            {
                SerializedProperty initTagsProp = modInfoProp.FindPropertyRelative("_initialData").FindPropertyRelative("tags");
                SerializedProperty currentTagsProp = modInfoProp.FindPropertyRelative("_data").FindPropertyRelative("tags");

                currentTagsProp.arraySize = initTagsProp.arraySize;

                for(int i = 0; i < initTagsProp.arraySize; ++i)
                {
                    currentTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue
                        = initTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                    currentTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("date_added").intValue
                        = initTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("date_added").intValue;
                }
            }

            // EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("media"),
            //                               new GUIContent("Media"));

            
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
                    EditorGUILayout.LabelField("ModIO URL",
                                               modObjectProp.FindPropertyRelative("profile_url").stringValue);
                    
                    EditorGUILayout.LabelField("Submitted By",
                                               modObjectProp.FindPropertyRelative("submitted_by").FindPropertyRelative("username").stringValue);

                    ModInfo.Status modStatus = (ModInfo.Status)modObjectProp.FindPropertyRelative("status").intValue;
                    EditorGUILayout.LabelField("Status",
                                               modStatus.ToString());

                    string ratingSummaryDisplay
                        = modObjectProp.FindPropertyRelative("rating_summary").FindPropertyRelative("weighted_aggregate").floatValue.ToString("0.00")
                        + " aggregate score. (From "
                        + modObjectProp.FindPropertyRelative("rating_summary").FindPropertyRelative("total_ratings").intValue.ToString()
                        + " ratings)";

                    EditorGUILayout.LabelField("Rating Summary",
                                                ratingSummaryDisplay);

                    EditorGUILayout.LabelField("Date Uploaded",
                                               modObjectProp.FindPropertyRelative("date_added").intValue.ToString());
                    EditorGUILayout.LabelField("Date Updated",
                                               modObjectProp.FindPropertyRelative("date_updated").intValue.ToString());
                    EditorGUILayout.LabelField("Date Live",
                                               modObjectProp.FindPropertyRelative("date_live").intValue.ToString());

                    EditorGUILayout.LabelField("Description",
                                               modInfoProp.FindPropertyRelative("_initialData").FindPropertyRelative("description").stringValue);

                    // EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("modfile"),
                    //                               new GUIContent("Modfile"));
                }
            }
            
            // --- Finalization ---
            if(doBrowseLogo)
            {
                // TODO(@jackson): Add other file-types
                string path = EditorUtility.OpenFilePanel("Select Mod Logo", "", "png");
                if (path.Length != 0)
                {
                    modInfoProp.FindPropertyRelative("logoFilepath").stringValue = path;
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
        private static List<string> expandedTagOptions = new List<string>();
        private static void DisplayTagOptions(SerializedProperty modInfoProp, List<string> selectedTags)
        {
            ++EditorGUI.indentLevel;

            int tagsRemovedCount = 0;

            foreach(GameTagOption tagOption in ModManager.gameInfo.taggingOptions)
            {
                if(!tagOption.isHidden)
                {
                    bool wasExpanded = expandedTagOptions.Contains(tagOption.name);
                    
                    if(EditorGUILayout.Foldout(wasExpanded, tagOption.name, true))
                    {
                        // Update expanded list
                        if(!wasExpanded)
                        {
                            expandedTagOptions.Add(tagOption.name);
                        }


                        ++EditorGUI.indentLevel;

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
                        

                        --EditorGUI.indentLevel;
                    }
                    else if(wasExpanded)
                    {
                        expandedTagOptions.Remove(tagOption.name);
                    }
                }
            }

            --EditorGUI.indentLevel;
        }

        private static void AddTagToMod(SerializedProperty modInfoProp, string tag)
        {
            SerializedProperty tagsArrayProp = modInfoProp.FindPropertyRelative("_data").FindPropertyRelative("tags");
            int newIndex = tagsArrayProp.arraySize;
            ++tagsArrayProp.arraySize;

            tagsArrayProp.GetArrayElementAtIndex(newIndex).FindPropertyRelative("name").stringValue = tag;
            tagsArrayProp.GetArrayElementAtIndex(newIndex).FindPropertyRelative("date_added").intValue = TimeStamp.Now().AsServerTimeStamp();
        }

        private static void RemoveTagFromMod(SerializedProperty modInfoProp, int tagIndex)
        {
            SerializedProperty tagsArrayProp = modInfoProp.FindPropertyRelative("_data").FindPropertyRelative("tags");

            tagsArrayProp.DeleteArrayElementAtIndex(tagIndex);
        }

        private static void ResetTags(SerializedProperty modInfoProp)
        {
            SerializedProperty initTagsProp = modInfoProp.FindPropertyRelative("_initialData").FindPropertyRelative("tags");
            SerializedProperty currentTagsProp = modInfoProp.FindPropertyRelative("_data").FindPropertyRelative("tags");

            currentTagsProp.arraySize = initTagsProp.arraySize;

            for(int i = 0; i < initTagsProp.arraySize; ++i)
            {
                currentTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue
                    = initTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                currentTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("date_added").intValue
                    = initTagsProp.GetArrayElementAtIndex(i).FindPropertyRelative("date_added").intValue;
            }
        }
    }
}

#endif