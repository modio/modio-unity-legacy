#if UNITY_EDITOR
using System;
using System.IO;

using UnityEngine;
using UnityEditor;


namespace ModIO
{
    [CustomPropertyDrawer(typeof(ModfileProfile))]
    public class ModfileProfileDrawer : PropertyDrawer
    {
        // --- PROPERTY DRAWER OVERRIDES ---
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float yPos = position.y;
            Rect controlRect;

            // - Label -
            if(label != null && label != GUIContent.none)
            {
                controlRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                yPos += EditorGUIUtility.singleLineHeight;

                EditorGUI.LabelField(controlRect, label);
            }

            // - Version -
            controlRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            yPos += EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(controlRect, property.FindPropertyRelative("version"));

            // - Changelog -
            // TODO(@jackson): Make multi-line text field
            controlRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            yPos += EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(controlRect, property.FindPropertyRelative("changelog"));

            // - Metadata -
            // TODO(@jackson): Make multi-line text field
            controlRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            yPos += EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(controlRect, property.FindPropertyRelative("metadataBlob"));


            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float labelHeight = 0f;
            if(label != null && label != GUIContent.none)
            {
                labelHeight = EditorGUIUtility.singleLineHeight;
            }

            return labelHeight + EditorGUIUtility.singleLineHeight * 3;
        }
    }
}

#endif