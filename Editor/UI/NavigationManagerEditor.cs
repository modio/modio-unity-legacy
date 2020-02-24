#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    /// <summary>Provides inspector code for the NavigationManager.</summary>
    public class NavigationManagerEditor
    {
        /// <summary>Provides the functionality for displaying the selection priority for a view.</summary>
        public static void DisplaySelectionPriority(IBrowserView view)
        {
            if(view == null) { return; }

            var focusItems = new List<SelectionFocusPriority>(view.gameObject.GetComponentsInChildren<SelectionFocusPriority>());

            EditorGUILayout.LabelField("On Focus Selection Priority");

            if(focusItems.Count > 0)
            {
                focusItems.Sort(CompareSelectionFocusPriority);

                using(new EditorGUI.DisabledScope(true))
                {
                    foreach(var item in focusItems)
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(item.priority.ToString(),
                                                   GUILayout.Width(28));

                        EditorGUILayout.ObjectField(item.gameObject,
                                                    typeof(GameObject),
                                                    true);

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No SelectionFocusPriority components detected in children."
                                        + " Adding these components allows for prioritized selection"
                                        + " when this view is focused.",
                                        MessageType.None);
            }
        }

        /// <summary>Describes the comparison functionality for sorting a SelectionFocusPriority list.</summary>
        public static int CompareSelectionFocusPriority(SelectionFocusPriority a, SelectionFocusPriority b)
        {
            if(a == null) { return 1; }
            if(b == null) { return -1; }
            return (a.priority - b.priority);
        }
    }
}

#endif
