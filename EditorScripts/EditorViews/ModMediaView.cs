#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public class ModMediaView : ISceneEditorView
    {
        // ---------[ FIELDS ]---------
        private bool isYouTubeExpanded = false;
        private bool isSketchFabExpanded = false;
        private bool isImagesExpanded = false;

        // - ISceneEditorView Interface -
        public string GetViewHeader() { return "Media"; }
        public void OnEnable()
        {
            isYouTubeExpanded = false;
            isSketchFabExpanded = false;
            isImagesExpanded = false;
        }
        public void OnDisable() {}
        
        public void OnGUI(SerializedObject serializedSceneData)
        {
            serializedSceneData.Update();

            SerializedProperty modInfoProp = serializedSceneData.FindProperty("modInfo");

            bool isNewMod = modInfoProp.FindPropertyRelative("_data.id").intValue <= 0;

            // --- Mod Media ---
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
        }

        private static void ResetModMedia(SerializedProperty modInfoProp)
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
    }
}

#endif
