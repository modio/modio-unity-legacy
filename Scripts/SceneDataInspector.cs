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

            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("name"),
                                          new GUIContent("Name"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("name_id"),
                                          new GUIContent("Name-ID"));


            ModInfo.Visibility modVisibility = (ModInfo.Visibility)modObjectProp.FindPropertyRelative("visible").intValue;
            modVisibility = (ModInfo.Visibility)EditorGUILayout.EnumPopup("Visibility", modVisibility);
            modObjectProp.FindPropertyRelative("visible").intValue = (int)modVisibility;

            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("homepage"),
                                          new GUIContent("Homepage"));

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Stock");
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("stock"),
                                                  new GUIContent(""));

                    EditorGUILayout.LabelField("0 = Unlimited", GUILayout.Width(80));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();

            // --- EXPANDABLE SECTION ---
            bool doBrowseLogo = false;
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Logo");
                EditorGUILayout.BeginVertical();
                {
                    // TODO(@jackson): Add placeholder for no logo

                    if(logoTexture != null)
                    {
                        Rect logoRect = EditorGUILayout.GetControlRect(false, 110.0f, null);
                        EditorGUI.LabelField(new Rect(logoRect.x, logoRect.y, logoRect.width, logoRect.height - 90.0f),
                                             logoSourceDisplay);
                        EditorGUI.DrawPreviewTexture(new Rect(logoRect.x, logoRect.y + 20.0f, logoRect.width, logoRect.height - 20.0f),
                                                     logoTexture, null, ScaleMode.ScaleAndCrop);
                        doBrowseLogo = GUI.Button(logoRect, "", GUI.skin.label);
                    }

                    // doBrowseLogo = GUILayout.Button(logoSourceDisplay, GUI.skin.textField);
                    // EditorGUILayout.BeginHorizontal();
                    // {
                    // }
                    // EditorGUILayout.EndHorizontal();

                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();

            // SerializedProperty summaryProp = modObjectProp.FindPropertyRelative("summary");
            // EditorGUILayout.PrefixLabel("Summary");
            // summaryProp.stringValue = EditorGUILayout.TextArea(summaryProp.stringValue);

            // SerializedProperty descriptionProp = modObjectProp.FindPropertyRelative("description");
            // EditorGUILayout.PrefixLabel("Description");
            // descriptionProp.stringValue = EditorGUILayout.TextArea(descriptionProp.stringValue);

            // SerializedProperty metadataProp = modObjectProp.FindPropertyRelative("metadata_blob");
            // EditorGUILayout.PrefixLabel("Metadata");
            // metadataProp.stringValue = EditorGUILayout.TextArea(metadataProp.stringValue);

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

            // // TODO(@jackson): Handle file missing
            // if(File.Exists(logoFilepath)
            //    && File.GetLastWriteTime(logoFilepath) > logoLastWrite)
            // {
            //     logoLastWrite = File.GetLastWriteTime(logoFilepath);

            //     logoTexture = new Texture2D(0, 0);
            //     logoTexture.LoadImage(File.ReadAllBytes(logoFilepath));
            // }
        }
    }
}

#endif