#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ModIO
{
    public class ModMediaViewPart : IModProfileViewPart
    {
        // ------[ CONSTANTS ]------

        // ------[ EDITOR CACHING ]------
        // - Serialized Property -
        private SerializedProperty editableProfileProperty;
        private ModProfile profile;

        // - Foldouts -
        private bool isYouTubeExpanded;
        private bool isSketchFabExpanded;
        private bool isImagesExpanded;


        // ------[ INITIALIZATION ]------
        public void OnEnable(SerializedProperty serializedEditableModProfile, ModProfile baseProfile)
        {
            this.editableProfileProperty = serializedEditableModProfile;
            this.profile = baseProfile;

            this.isYouTubeExpanded = false;
            this.isSketchFabExpanded = false;
            this.isImagesExpanded = false;
        }
        public void OnDisable() {}

        // ------[ ONUPDATE ]------
        public void OnUpdate() {}

        // ------[ ONGUI ]------
        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Media");
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(profile == null))
                {
                    if(EditorGUILayoutExtensions.UndoButton())
                    {
                        ResetModMedia();
                    }
                }
            EditorGUILayout.EndHorizontal();

            ++EditorGUI.indentLevel;
                // - YouTube -
                EditorGUI.BeginChangeCheck();
                EditorGUILayoutExtensions.ArrayPropertyField(editableProfileProperty.FindPropertyRelative("youtubeURLs.value"),
                                                             "YouTube Links", ref isYouTubeExpanded);
                editableProfileProperty.FindPropertyRelative("youtubeURLs.isDirty").boolValue |= EditorGUI.EndChangeCheck();
                // - SketchFab -
                EditorGUI.BeginChangeCheck();
                EditorGUILayoutExtensions.ArrayPropertyField(editableProfileProperty.FindPropertyRelative("sketchfabURLs.value"),
                                                             "SketchFab Links", ref isSketchFabExpanded);
                editableProfileProperty.FindPropertyRelative("sketchfabURLs.isDirty").boolValue |= EditorGUI.EndChangeCheck();
                // - Gallery Images -
                EditorGUI.BeginChangeCheck();
                EditorGUILayoutExtensions.ArrayPropertyField(editableProfileProperty.FindPropertyRelative("galleryImageLocators.value"),
                                                             "Gallery Images Links", ref isImagesExpanded);
                editableProfileProperty.FindPropertyRelative("galleryImageLocators.isDirty").boolValue |= EditorGUI.EndChangeCheck();
            --EditorGUI.indentLevel;
        }

        // - Misc Functionality -
        private void ResetModMedia() {}
        // {
        //     // - YouTube -
        //     initialDataProp = editableProfileProperty.FindPropertyRelative("_initialData.media.youtube");
        //     currentDataProp = editableProfileProperty.FindPropertyRelative("_data.media.youtube");

        //     currentDataProp.arraySize = initialDataProp.arraySize;

        //     for(int i = 0; i < initialDataProp.arraySize; ++i)
        //     {
        //         currentDataProp.GetArrayElementAtIndex(i).stringValue
        //             = initialDataProp.GetArrayElementAtIndex(i).stringValue;
        //     }

        //     // - SketchFab -
        //     initialDataProp = editableProfileProperty.FindPropertyRelative("_initialData.media.sketchfab");
        //     currentDataProp = editableProfileProperty.FindPropertyRelative("_data.media.sketchfab");

        //     currentDataProp.arraySize = initialDataProp.arraySize;

        //     for(int i = 0; i < initialDataProp.arraySize; ++i)
        //     {
        //         currentDataProp.GetArrayElementAtIndex(i).stringValue
        //             = initialDataProp.GetArrayElementAtIndex(i).stringValue;
        //     }

        //     // - Image Gallery -
        //     initialDataProp = editableProfileProperty.FindPropertyRelative("_initialData.media.images");
        //     currentDataProp = editableProfileProperty.FindPropertyRelative("_data.media.images");

        //     currentDataProp.arraySize = initialDataProp.arraySize;

        //     for(int i = 0; i < initialDataProp.arraySize; ++i)
        //     {
        //         currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("filename").stringValue
        //             = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("filename").stringValue;
        //         currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("original").stringValue
        //             = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("original").stringValue;
        //         currentDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("thumb_320x180").stringValue
        //             = initialDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("thumb_320x180").stringValue;
        //     }
        // }
    }
}

#endif
