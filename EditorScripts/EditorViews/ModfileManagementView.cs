#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public class ModfileManagementView : ISceneEditorView
    {
        // - ISceneEditorView Interface -
        public string GetViewHeader() { return "Files"; }
        public void OnEnable() {}
        public void OnDisable() {}


        // TODO(@jackson): Show all modfiles
        public void OnGUI(SerializedObject serializedSceneData)
        {
            SerializedProperty buildLocationProp = serializedSceneData.FindProperty("buildLocation");
            SerializedProperty modfileProfileProp = serializedSceneData.FindProperty("buildProfile");
            SerializedProperty setPrimaryProp = serializedSceneData.FindProperty("setBuildAsPrimary");

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
