#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ModIO
{
    [CustomEditor(typeof(EditableModInfo))]
    public class ModInfoEditor : Editor
    {
        private bool isSummaryExpanded = false;
        private bool isMetadataBlobExpanded = false;
        private bool isLogoExpanded = false;
        private bool isDescriptionExpanded = false;

        private Texture2D logoTexture = null;
        private string logoFilepath = "";
        private DateTime logoLastWrite = new DateTime();

        public override void OnInspectorGUI()
        {
            DisplayAsObject(serializedObject);
        }

        public void DisplayAsObject(SerializedObject serializedModInfo)
        {
            serializedModInfo.Update();
            
            SerializedProperty modObjectProp = serializedModInfo.FindProperty("_data");
            DisplayInner(modObjectProp);

            serializedModInfo.ApplyModifiedProperties();
        }

        public void DisplayAsProperty(SerializedProperty serializedModInfo)
        {
            DisplayInner(serializedModInfo.FindPropertyRelative("_data"));
        }

        private void DisplayInner(SerializedProperty modObjectProp)
        {
            // TODO(@jackson): Load cached Logo

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

                    EditorGUILayout.LabelField("[ 0 = Unlimited ]");
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();

            // --- EXPANDABLE SECTION ---
            bool doBrowseLogo = false;
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Logo");
                EditorGUILayout.BeginHorizontal();
                {
                    string browseButtonText = (logoFilepath != "" ? Path.GetFileName(logoFilepath) : "Browse...");
                    doBrowseLogo = GUILayout.Button(browseButtonText);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();

            // TODO(@jackson): Add placeholder for no logo

            isSummaryExpanded = EditorGUILayout.Foldout(isSummaryExpanded, "Summary");
            if(isSummaryExpanded)
            {
                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("summary"),
                                              new GUIContent(""), GUILayout.Height(120));
            }

            isDescriptionExpanded = EditorGUILayout.Foldout(isDescriptionExpanded, "Description");
            if(isDescriptionExpanded)
            {
                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("description"),
                                              new GUIContent(""), GUILayout.Height(120));
            }

            isMetadataBlobExpanded = EditorGUILayout.Foldout(isMetadataBlobExpanded, "Metadata Blob");
            if(isMetadataBlobExpanded)
            {
                EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("metadata_blob"),
                                              new GUIContent(""), GUILayout.Height(120));
            }

            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("modfile"),
                                          new GUIContent("Modfile"));

            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("tags"),
                                          new GUIContent("Tags"));

            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("media"),
                                          new GUIContent("Media"));

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
            
            // Finalization
            if(doBrowseLogo)
            {
                // TODO(@jackson): Add other file-types
                string path = EditorUtility.OpenFilePanel("Select Mod Logo", "", "png");
                if (path.Length != 0)
                {
                    logoFilepath = path;
                    logoLastWrite = new DateTime();
                }
            }

            if(File.Exists(logoFilepath)
               && File.GetLastWriteTime(logoFilepath) > logoLastWrite)
            {
                logoLastWrite = File.GetLastWriteTime(logoFilepath);

                logoTexture = new Texture2D(0, 0);
                logoTexture.LoadImage(File.ReadAllBytes(logoFilepath));
            }
        }
    }
}

#endif