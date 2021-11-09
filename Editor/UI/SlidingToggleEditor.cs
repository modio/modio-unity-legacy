#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UI;

namespace ModIO.UI.EditorCode
{
    [CustomEditor(typeof(SlidingToggle), true)]
    [CanEditMultipleObjects]
    public class SlidingToggleEditor : SelectableEditor
    {
        // Toggle Properties
        SerializedProperty m_OnValueChangedProperty;
        SerializedProperty m_TransitionProperty;
        SerializedProperty m_GraphicProperty;
        SerializedProperty m_GroupProperty;
        SerializedProperty m_IsOnProperty;

        // Sliding Toggle Properties
        SerializedProperty m_OnClickedWhileOnProperty;
        SerializedProperty m_OnClickedWhileOffProperty;
        SerializedProperty m_ContentProperty;
        SerializedProperty m_DisableAutoToggleProperty;
        SerializedProperty m_SlideAxisProperty;
        SerializedProperty m_SlideDurationProperty;
        SerializedProperty m_ReactivateDelayProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Toggle class properties
            this.m_TransitionProperty = serializedObject.FindProperty("toggleTransition");
            this.m_GraphicProperty = serializedObject.FindProperty("graphic");
            this.m_GroupProperty = serializedObject.FindProperty("m_Group");
            this.m_IsOnProperty = serializedObject.FindProperty("m_IsOn");
            this.m_OnValueChangedProperty = serializedObject.FindProperty("onValueChanged");

            // Sliding toggle properties
            this.m_OnClickedWhileOnProperty = serializedObject.FindProperty("onClickedWhileOn");
            this.m_OnClickedWhileOffProperty = serializedObject.FindProperty("onClickedWhileOff");
            this.m_ContentProperty = serializedObject.FindProperty("m_slideContent");
            this.m_DisableAutoToggleProperty = serializedObject.FindProperty("m_disableAutoToggle");
            this.m_SlideAxisProperty = serializedObject.FindProperty("m_slideAxis");
            this.m_SlideDurationProperty = serializedObject.FindProperty("m_slideDuration");
            this.m_ReactivateDelayProperty = serializedObject.FindProperty("m_reactivateDelay");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(this.m_IsOnProperty);
            EditorGUILayout.PropertyField(this.m_TransitionProperty);
            EditorGUILayout.PropertyField(this.m_GraphicProperty);
            EditorGUILayout.PropertyField(this.m_GroupProperty);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(this.m_DisableAutoToggleProperty);
            EditorGUILayout.PropertyField(this.m_ContentProperty);
            EditorGUILayout.PropertyField(this.m_SlideAxisProperty);
            EditorGUILayout.PropertyField(this.m_SlideDurationProperty);
            EditorGUILayout.PropertyField(this.m_ReactivateDelayProperty);

            EditorGUILayout.Space();

            // Draw the event notification options
            EditorGUILayout.PropertyField(this.m_OnClickedWhileOnProperty);
            EditorGUILayout.PropertyField(this.m_OnClickedWhileOffProperty);
            EditorGUILayout.PropertyField(this.m_OnValueChangedProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif // UNITY_EDITOR
