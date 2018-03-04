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
        private const float CHANGELOG_HEIGHT = 130.0f;
        private const float METADATABLOB_HEIGHT = 130.0f;

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
            controlRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            yPos += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(controlRect, "Changelog");

            controlRect = new Rect(position.x, yPos, position.width, CHANGELOG_HEIGHT);
            yPos += CHANGELOG_HEIGHT;

            SerializedProperty changelogProp = property.FindPropertyRelative("changelog");
            changelogProp.stringValue = EditorGUIExtensions.MultilineTextField(controlRect, changelogProp.stringValue);

            // - Metadata -
            controlRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            yPos += EditorGUIUtility.singleLineHeight;
            
            EditorGUI.LabelField(controlRect, "Metadata");

            controlRect = new Rect(position.x, yPos, position.width, METADATABLOB_HEIGHT);
            yPos += METADATABLOB_HEIGHT;

            SerializedProperty metadataProp = property.FindPropertyRelative("metadataBlob");
            metadataProp.stringValue = EditorGUIExtensions.MultilineTextField(controlRect, metadataProp.stringValue);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float labelHeight = 0f;
            if(label != null && label != GUIContent.none)
            {
                labelHeight = EditorGUIUtility.singleLineHeight;
            }

            return (labelHeight
                    + EditorGUIUtility.singleLineHeight * 3
                    + CHANGELOG_HEIGHT
                    + METADATABLOB_HEIGHT);
        }
    }
}

#endif