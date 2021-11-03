#if UNITY_EDITOR

using System;

using UnityEngine;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomPropertyDrawer(typeof(ViewControlBindings.ButtonTriggerCondition))]
    public class ButtonTriggerConditionDrawer : PropertyDrawer
    {
        // ---------[ GUI FUNCTIONALITY ]---------
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var oldValue = (ViewControlBindings.ButtonTriggerCondition)property.intValue;
            Enum enumNew = EditorGUI.EnumFlagsField(position, label, oldValue);
            property.intValue = (int)Convert.ChangeType(
                enumNew, typeof(ViewControlBindings.ButtonTriggerCondition));
        }
    }

    [CustomPropertyDrawer(typeof(ViewControlBindings.AxisTriggerCondition))]
    public class AxisTriggerConditionDrawer : PropertyDrawer
    {
        // ---------[ GUI FUNCTIONALITY ]---------
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var oldValue = (ViewControlBindings.AxisTriggerCondition)property.intValue;
            Enum enumNew = EditorGUI.EnumFlagsField(position, label, oldValue);
            property.intValue =
                (int)Convert.ChangeType(enumNew, typeof(ViewControlBindings.AxisTriggerCondition));
        }
    }
}

#endif // UNITY_EDITOR
