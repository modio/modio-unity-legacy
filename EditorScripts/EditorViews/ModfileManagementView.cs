#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public class ModfileManagementView : IEditorView
    {
        public string GetDisplayName() { return "Files"; }

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
    }
}

#endif
