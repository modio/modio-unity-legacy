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
        public virtual string GetViewHeader() { return "Files"; }
        public virtual void OnEnable() {}
        public virtual void OnDisable() {}

        public virtual bool IsViewDisabled()
        {
            return isModUploading;
        }

        // TODO(@jackson): Show all modfiles
        public void OnGUI(EditorSceneData sceneData)
        {
            using(new EditorGUI.DisabledScope(this.IsViewDisabled()))
            {
                this.OnGUIInner(sceneData);
            }
        }

        protected virtual void OnGUIInner(EditorSceneData sceneData)
        {
            SerializedObject serializedSceneData = new SerializedObject(sceneData);

            SerializedProperty buildLocationProp = serializedSceneData.FindProperty("buildLocation");
            SerializedProperty modfileProfileProp = serializedSceneData.FindProperty("buildProfile");
            SerializedProperty setPrimaryProp = serializedSceneData.FindProperty("setBuildAsPrimary");

            DisplayBuildLocationProperty(buildLocationProp);
            EditorGUILayout.PropertyField(modfileProfileProp, GUIContent.none);
            EditorGUILayout.PropertyField(setPrimaryProp, new GUIContent("Set Primary"));

            serializedSceneData.ApplyModifiedProperties();

            using(new EditorGUI.DisabledScope(sceneData.modInfo.id <= 0))
            {
                if(GUILayout.Button("Package and Upload"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        // TODO(@jackson): Unhack
                        sceneData.buildProfile.modId = sceneData.modInfo.id;

                        if(EditorSceneManager.EnsureUntitledSceneHasBeenSaved("The scene needs to be saved before publishing online"))
                        {
                            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

                            UploadModBinary(sceneData.buildLocation, sceneData.buildProfile);
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

        protected virtual void UploadModBinary(string buildLocation, ModfileProfile profile)
        {
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

#endif
