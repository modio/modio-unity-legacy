#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ModIO
{
    public class ModMediaView : ISceneEditorView
    {
        // ---------[ FIELDS ]---------
        private bool isYouTubeExpanded = false;
        private bool isSketchFabExpanded = false;
        private bool isImagesExpanded = false;

        private bool isUploading = false;

        // - ISceneEditorView Interface -
        public string GetViewHeader() { return "Media"; }
        public void OnEnable()
        {
            isYouTubeExpanded = false;
            isSketchFabExpanded = false;
            isImagesExpanded = false;
        }
        public void OnDisable() {}
        
        public void OnGUI(EditorSceneData sceneData)
        {
            bool isNewMod = sceneData.modInfo.id <= 0;

            SerializedObject serializedSceneData = new SerializedObject(sceneData);
            SerializedProperty modInfoProp = serializedSceneData.FindProperty("modInfo");

            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Media");
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(isNewMod))
                {
                    if(EditorGUILayoutExtensions.UndoButton())
                    {
                        ResetModMedia(modInfoProp);
                    }
                }
            EditorGUILayout.EndHorizontal();

            ++EditorGUI.indentLevel;
                EditorGUILayoutExtensions.ArrayPropertyField(modInfoProp.FindPropertyRelative("_data.media.youtube"),
                                                             "YouTube Links", ref isYouTubeExpanded);
                EditorGUILayoutExtensions.ArrayPropertyField(modInfoProp.FindPropertyRelative("_data.media.sketchfab"),
                                                             "SketchFab Links", ref isSketchFabExpanded);
                EditorGUILayoutExtensions.ArrayPropertyField(modInfoProp.FindPropertyRelative("_data.media.images"),
                                                             "Gallery Images", ref isImagesExpanded);
            --EditorGUI.indentLevel;

            serializedSceneData.ApplyModifiedProperties();

            // --- Uploading ---
            if(GUILayout.Button("Update Mod Media"))
            {
                EditorApplication.delayCall += () => SendModMediaChanges(sceneData.modInfo);
            }
        }

        private void ResetModMedia(SerializedProperty modInfoProp)
        {
            SerializedProperty initialDataProp;
            SerializedProperty currentDataProp;

            // - YouTube -
            initialDataProp = modInfoProp.FindPropertyRelative("_initialData.media.youtube");
            currentDataProp = modInfoProp.FindPropertyRelative("_data.media.youtube");

            currentDataProp.arraySize = initialDataProp.arraySize;

            for(int i = 0; i < initialDataProp.arraySize; ++i)
            {
                currentDataProp.GetArrayElementAtIndex(i).stringValue
                    = initialDataProp.GetArrayElementAtIndex(i).stringValue;
            }

            // - SketchFab -
            initialDataProp = modInfoProp.FindPropertyRelative("_initialData.media.sketchfab");
            currentDataProp = modInfoProp.FindPropertyRelative("_data.media.sketchfab");

            currentDataProp.arraySize = initialDataProp.arraySize;

            for(int i = 0; i < initialDataProp.arraySize; ++i)
            {
                currentDataProp.GetArrayElementAtIndex(i).stringValue
                    = initialDataProp.GetArrayElementAtIndex(i).stringValue;
            }

            // - Image Gallery -
            initialDataProp = modInfoProp.FindPropertyRelative("_initialData.media.images");
            currentDataProp = modInfoProp.FindPropertyRelative("_data.media.images");

            currentDataProp.arraySize = initialDataProp.arraySize;

            for(int i = 0; i < initialDataProp.arraySize; ++i)
            {
                currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("filename").stringValue
                    = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("filename").stringValue;
                currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("original").stringValue
                    = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("original").stringValue;
                currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("thumb_320x180").stringValue
                    = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("thumb_320x180").stringValue;
            }
        }

        private void SendModMediaChanges(EditableModInfo modInfo)
        {
            if(EditorSceneManager.EnsureUntitledSceneHasBeenSaved("The scene needs to be saved before uploading mod data"))
            {
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

                bool isAddCompleted = false;
                bool isDeleteCompleted = false;

                isUploading = true;

                System.Action onAddCompleted = () =>
                {
                    // TODO(@jackson): Update the object with the changes
                    isAddCompleted = true;
                    if(isDeleteCompleted)
                    {
                        APIClient.GetMod(modInfo.id,
                                         (mod) => { modInfo = EditableModInfo.FromModInfo(mod); isUploading = false; },
                                         (e) => { isUploading = false; });
                    }
                };

                System.Action onDeleteCompleted = () =>
                {
                    // TODO(@jackson): Update the object with the changes
                    isDeleteCompleted = true;
                    if(isAddCompleted)
                    {
                        APIClient.GetMod(modInfo.id,
                                         (mod) => { modInfo = EditableModInfo.FromModInfo(mod); isUploading = false; },
                                         (e) => { isUploading = false; });
                    }
                };

                ModManager.AddModMedia(modInfo.GetAddedMedia(),
                                       (m) => { onAddCompleted(); },
                                       (e) => { onAddCompleted(); });
                ModManager.DeleteModMedia(modInfo.GetRemovedMedia(),
                                          (m) => { onDeleteCompleted(); },
                                          (e) => { onDeleteCompleted(); });
            }
        }
    }
}

#endif
