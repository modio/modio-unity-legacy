#if UNITY_EDITOR
using System;
using System.IO;
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

            DisplayInner(modInfoProp, logoTexture, logoSource);
        }

        private static void DisplayInner(SerializedProperty modInfoProp, Texture2D logoTexture, string logoSource)
        {
            string logoSourceDisplay = (logoSource == "" ? "Browse..." : logoSource);
            SerializedProperty modObjectProp = modInfoProp.FindPropertyRelative("_data");
            bool isUndoRequested = false;
            Rect buttonRect = new Rect();
            GUILayoutOption[] buttonLayout = new GUILayoutOption[]{ GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight) };

            // - Name -
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("name"),
                                              new GUIContent("Name"));
                isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
            }
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "name");
            }


            // - Name ID -
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("name_id"),
                                              new GUIContent("Name-ID"));
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
            {
                modVisibility = (ModInfo.Visibility)EditorGUILayout.EnumPopup("Visibility", modVisibility);
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
            {
                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("homepage"),
                                              new GUIContent("Homepage"));
                isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
            }
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "homepage");
            }

            // - Stock -
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Stock");

                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("stock"),
                                              new GUIContent(""));//, GUILayout.Width(40));

                // TODO(@jackson): Change to checkbox
                EditorGUILayout.LabelField("0 = Unlimited", GUILayout.Width(80));

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
            {
                EditorGUILayout.PrefixLabel("Logo");

                if(Event.current.type == EventType.Layout)
                {
                    EditorGUILayout.TextField(logoSourceDisplay);
                }
                else
                {
                    doBrowseLogo = GUILayout.Button(logoSourceDisplay, GUI.skin.textField);
                }

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
            {
                EditorGUILayout.PrefixLabel("Summary");

                int charCount = summaryProp.stringValue.Length;

                EditorGUILayout.LabelField("[" + (SUMMARY_CHAR_LIMIT - charCount).ToString()
                                           + " characters remaining]");

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
            {
                EditorGUILayout.PrefixLabel("Description");

                EditorGUILayout.LabelField("[HTML Tags accepted]");
                
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
            {
                EditorGUILayout.PrefixLabel("Metadata");
                
                GUILayout.FlexibleSpace();

                isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label, buttonLayout);
            }
            EditorGUILayout.EndHorizontal();

            controlRect = EditorGUILayout.GetControlRect(false, 120.0f, null);
            metadataProp.stringValue = EditorGUI.TextField(controlRect, metadataProp.stringValue);

            if(isUndoRequested)
            {
                ResetStringField(modInfoProp, "description");
            }


            // EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("modfile"),
            //                               new GUIContent("Modfile"));

            // EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("tags"),
            //                               new GUIContent("Tags"));

            // EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("media"),
            //                               new GUIContent("Media"));

            // TODO(@jackson): Do section header or foldout
            EditorGUI.BeginDisabledGroup(true);
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
                }
            }
            EditorGUI.EndDisabledGroup();
            
            // ---[ FINALIZATION ]---
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

        private static void AffixUndoButton(Rect buttonRect, out bool isUndoRequested)
        {
            isUndoRequested = GUI.Button(buttonRect, UISettings.Instance.EditorTexture_UndoButton, GUI.skin.label);
        }

        private static void CreatePrefixWithUndoButton(string prefix, out bool isUndoRequested)
        {
            float buttonSize = EditorGUIUtility.singleLineHeight;
            EditorGUIUtility.labelWidth -= buttonSize;
            
            EditorGUILayout.BeginHorizontal();
            {
                isUndoRequested = GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton,
                                                   GUI.skin.label, GUILayout.Width(buttonSize));
                EditorGUILayout.PrefixLabel(prefix);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth += buttonSize;
        }

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
    }
}

#endif