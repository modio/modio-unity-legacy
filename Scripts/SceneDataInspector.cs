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
        public static bool isTagsExpanded = false;

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

            EditorModLayout.ModProfilePanel(modInfoProp, logoTexture, logoSource, selectedTags, ref isTagsExpanded);
            EditorGUILayout.Space();
            EditorModLayout.ModMediaPanel(modInfoProp);
            EditorGUILayout.Space();
            EditorModLayout.ModfileManagementPanel(serializedSceneData.FindProperty("buildLocation"),
                                                     serializedSceneData.FindProperty("buildProfile"),
                                                     serializedSceneData.FindProperty("setBuildAsPrimary"));
        }
    }
}

#endif