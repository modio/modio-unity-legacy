#if UNITY_EDITOR
using System;

using UnityEditor;
using UnityEngine;

namespace ModIO.EditorCode
{
    public static class EditorUtilityExtensions
    {
        public static string[] GetSerializedPropertyStringArray(SerializedProperty arrayProperty)
        {
            Debug.Assert(arrayProperty.isArray);
            Debug.Assert(arrayProperty.arrayElementType.Equals("string"));

            var retVal = new string[arrayProperty.arraySize];
            for(int i = 0; i < arrayProperty.arraySize; ++i)
            {
                retVal[i] = arrayProperty.GetArrayElementAtIndex(i).stringValue;
            }
            return retVal;
        }

        public static void SetSerializedPropertyStringArray(SerializedProperty arrayProperty,
                                                            string[] value)
        {
            Debug.Assert(arrayProperty.isArray);
            Debug.Assert(arrayProperty.arrayElementType.Equals("string"));

            arrayProperty.arraySize = value.Length;
            for(int i = 0; i < value.Length; ++i)
            {
                arrayProperty.GetArrayElementAtIndex(i).stringValue = value[i];
            }
        }
    }

    public static class EditorGUIExtensions
    {
        public static string MultilineTextField(Rect position, string content)
        {
            bool wasWordWrapEnabled = GUI.skin.textField.wordWrap;

            GUI.skin.textField.wordWrap = true;

            string retVal = EditorGUI.TextField(position, content);

            GUI.skin.textField.wordWrap = wasWordWrapEnabled;

            return retVal;
        }
    }

    public static class EditorGUILayoutExtensions
    {
        public static void ArrayPropertyField(SerializedProperty arrayProperty, string arrayLabel,
                                              ref bool isExpanded)
        {
            CustomLayoutArrayPropertyField(arrayProperty, arrayLabel, ref isExpanded,
                                           (i, p) => EditorGUILayout.PropertyField(p));
        }

        public static void CustomLayoutArrayPropertyField(
            SerializedProperty arrayProperty, string arrayLabel, ref bool isExpanded,
            Action<int, SerializedProperty> customLayoutFunction)
        {
            isExpanded = EditorGUILayout.Foldout(isExpanded, arrayLabel, true);

            if(isExpanded)
            {
                EditorGUI.indentLevel += 1;

                EditorGUILayout.PropertyField(arrayProperty.FindPropertyRelative("Array.size"),
                                              new GUIContent("Size"));

                for(int i = 0; i < arrayProperty.arraySize; ++i)
                {
                    SerializedProperty prop =
                        arrayProperty.FindPropertyRelative("Array.data[" + i + "]");
                    customLayoutFunction(i, prop);
                }

                EditorGUI.indentLevel -= 1;
            }
        }

        public static bool BrowseButton(string path, GUIContent label)
        {
            bool doBrowse = false;

            if(String.IsNullOrEmpty(path))
            {
                path = "Browse...";
            }

            EditorGUILayout.BeginHorizontal();
            if(label != null && label != GUIContent.none)
            {
                EditorGUILayout.PrefixLabel(label);
            }

            if(Event.current.type == EventType.Layout)
            {
                EditorGUILayout.TextField(path, "");
            }
            else
            {
                doBrowse = GUILayout.Button(path, GUI.skin.textField);
            }
            EditorGUILayout.EndHorizontal();

            return doBrowse;
        }

        private static GUILayoutOption[] buttonLayout =
            new GUILayoutOption[] { GUILayout.Width(EditorGUIUtility.singleLineHeight),
                                    GUILayout.Height(EditorGUIUtility.singleLineHeight) };
        public static bool UndoButton(bool isEnabled = true)
        {
            using(new EditorGUI.DisabledScope(!isEnabled))
            {
                return GUILayout.Button(EditorImages.UndoButton, GUI.skin.label, buttonLayout);
            }
        }
        public static bool ClearButton(bool isEnabled = true)
        {
            using(new EditorGUI.DisabledScope(!isEnabled))
            {
                return GUILayout.Button(EditorImages.ClearButton, GUI.skin.label, buttonLayout);
            }
        }

        public static string MultilineTextField(string content)
        {
            Rect controlRect = EditorGUILayout.GetControlRect(false, 130.0f);
            return EditorGUIExtensions.MultilineTextField(controlRect, content);
        }
    }
}
#endif
