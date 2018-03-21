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
        public virtual string GetViewTitle() { return "Files"; }
        public virtual void OnEnable() {}
        public virtual void OnDisable() {}

        public virtual bool IsViewDisabled()
        {
            return isModUploading;
        }

        public void OnGUI(EditorSceneData sceneData)
        {
            using(new EditorGUI.DisabledScope(this.IsViewDisabled()))
            {
                this.OnGUIInner(sceneData);
            }
        }

        // TODO(@jackson): Show all modfiles
        protected virtual void OnGUIInner(EditorSceneData sceneData)
        {
            SerializedObject serializedSceneData = new SerializedObject(sceneData);

            SerializedProperty buildLocationProp = serializedSceneData.FindProperty("buildLocation");
            SerializedProperty modfileValuesProp = serializedSceneData.FindProperty("modfileEdits");
            SerializedProperty setPrimaryProp = serializedSceneData.FindProperty("setBuildAsPrimary");

            DisplayBuildLocationProperty(buildLocationProp);
            EditorGUILayout.PropertyField(modfileValuesProp, GUIContent.none);
            EditorGUILayout.PropertyField(setPrimaryProp, new GUIContent("Set Primary"));

            serializedSceneData.ApplyModifiedProperties();

            using(new EditorGUI.DisabledScope(sceneData.modId <= 0))
            {
                if(GUILayout.Button("Package and Upload"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if(EditorSceneManager.EnsureUntitledSceneHasBeenSaved("The scene needs to be saved before publishing online"))
                        {
                            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

                            UploadModBinary(sceneData.modId, sceneData.buildLocation, sceneData.modfileValues);
                        }
                    };
                }
            }
        }

        protected virtual void DisplayBuildLocationProperty(SerializedProperty buildLocationProp)
        {
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
        }

        protected virtual void UploadModBinary(int modId,
                                               string buildLocation,
                                               ModfileEditableFields modfileValues)
        {
            isModUploading = true;

            // --- Callbacks ---
            System.Action<Modfile> onUploadSucceeded = (mf) =>
            {
                EditorUtility.DisplayDialog("Modfile Successfully Uploaded",
                                "",
                                "Ok");
                isModUploading = false;
            };
            System.Action<ErrorInfo> onUploadFailed = (mf) =>
            {
                EditorUtility.DisplayDialog("Modfile Successfully Uploaded",
                                "",
                                "Ok");
                isModUploading = false;
            };

            // --- Start Upload ---
            ModManager.UploadModBinary_Unzipped(modId,
                                                buildLocation,
                                                modfileValues,
                                                true,
                                                onUploadSucceeded,
                                                onUploadFailed);
        }
    }
}

#endif
