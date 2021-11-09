#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomPropertyDrawer(typeof(SubscriptionSortDropdownController.FieldSelectAttribute))]
    public class SubscriptionSortDropdownFieldSelectDrawer : PropertyDrawer
    {
        // ---------[ STATIC DATA ]---------
        private static GUIContent[] popupOptions = null;

        private static void LoadStaticData()
        {
            // load guicontent
            List<GUIContent> content = new List<GUIContent>(
                SubscriptionSortDropdownController.subscriptionSortOptions.Count);
            foreach(var kvp in SubscriptionSortDropdownController.subscriptionSortOptions)
            {
                content.Add(new GUIContent() {
                    text = kvp.Key,
                });
            }
            SubscriptionSortDropdownFieldSelectDrawer.popupOptions = content.ToArray();
        }

        // ---------[ GUI FUNCTIONALITY ]---------
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(popupOptions == null)
            {
                SubscriptionSortDropdownFieldSelectDrawer.LoadStaticData();
            }

            string currentValue = property.stringValue;
            int currentSelectionIndex = -1;

            for(int i = 0; i < popupOptions.Length && currentSelectionIndex < 0; ++i)
            {
                if(popupOptions[i].text == currentValue)
                {
                    currentSelectionIndex = i;
                }
            }

            if(currentSelectionIndex == -1)
            {
                currentSelectionIndex = 0;
            }

            int newSelectionIndex =
                EditorGUI.Popup(position, label, currentSelectionIndex, popupOptions);

            if(newSelectionIndex != currentSelectionIndex)
            {
                property.stringValue = popupOptions[newSelectionIndex].text;
            }
        }
    }
}

#endif
