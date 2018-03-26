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

        public virtual bool IsViewDisabled()
        {
            return isUploading;
        }

        // - ISceneEditorView Interface -
        public string GetViewTitle() { return "Media"; }
        public void OnEnable()
        {
            isYouTubeExpanded = false;
            isSketchFabExpanded = false;
            isImagesExpanded = false;
        }
        public void OnDisable() {}
        
        public void OnGUI(EditorSceneData sceneData)
        {
            using(new EditorGUI.DisabledScope(this.IsViewDisabled()))
            {
                this.OnGUIInner(sceneData);
            }
        }

        protected virtual void OnGUIInner(EditorSceneData sceneData)
        {
            bool isNewMod = sceneData.modId <= 0;

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
                EditorApplication.delayCall += () => SendModMediaChanges(sceneData);
            }
        }

        // - Misc Functionality -
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

        private void SendModMediaChanges(EditorSceneData sceneData)
        {
            throw new System.NotImplementedException();
            // int modId = sceneData.modId;

            // if(EditorSceneManager.EnsureUntitledSceneHasBeenSaved("The scene needs to be saved before uploading mod data"))
            // {
            //     EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            //     isUploading = true;

            //     System.Action<APIMessage> onDeleteCompleted = (m) =>
            //     {
            //         API.Client.GetMod(modId,
            //                          (mod) => { sceneData.modInfo = new ModProfile(); modInfo.WrapAPIObject(mod); isUploading = false; },
            //                          (e) => { isUploading = false; });
            //     };

            //     System.Action<APIMessage> onAddCompleted = (m) =>
            //     {
            //         ModManager.DeleteModMedia(modInfo.GetRemovedMedia(),
            //                                   onDeleteCompleted,
            //                                   (e) =>
            //                                   {
            //                                     isUploading = false;
            //                                     EditorUtility.DisplayDialog("Mod Media Removal failed",
            //                                                                 e.message,
            //                                                                 "Ok");
            //                                   });
            //     };

            //     ModManager.AddModMedia(modInfo.GetAddedMedia(),
            //                            onAddCompleted,
            //                            (e) =>
            //                            {
            //                             isUploading = false;
            //                             EditorUtility.DisplayDialog("Mod Media Submission failed",
            //                                                         e.message,
            //                                                         "Ok");
            //                            });
            // }
        }
    }
}

#endif
