#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ModIO
{
    public class ModfileManagementView : ISceneEditorView
    {
        // ---------[ FIELDS ]---------
        private bool isModUploading = false;

        // - ISceneEditorView Interface -
        public string GetViewHeader() { return "Files"; }
        public void OnEnable() {}
        public void OnDisable() {}


        // TODO(@jackson): Show all modfiles
        public void OnGUI(EditorSceneData sceneData)
        {
            SerializedObject serializedSceneData = new SerializedObject(sceneData);
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

            serializedSceneData.ApplyModifiedProperties();

            if(GUILayout.Button("Publish Build to Mod.IO"))
            {
                EditorApplication.delayCall += UploadModBinary;
            }
        }


        private void UploadModBinary()
        {
            string buildLocation = "";
            ModfileProfile profile = null;

            if(EditorSceneManager.EnsureUntitledSceneHasBeenSaved("The scene needs to be saved before publishing online"))
            {
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

                isModUploading = true;

                System.Action<Modfile> onUploadSucceeded = (mf) =>
                {
                    Debug.Log("Upload succeeded!");
                    isModUploading = false;
                };

                ModManager.UploadModBinary_Unzipped(buildLocation,
                                                    profile,
                                                    true,
                                                    onUploadSucceeded,
                                                    (e) => isModUploading = false);
            }
        }
    }
}

#endif
