#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    /// <summary>Custom editor for the EnumDropdownBase-derived components.</summary>
    [CustomEditor(typeof(EnumDropdownBase), true)]
    public class EnumDropdownEditor : Editor
    {
        SerializedProperty pairingArrayProperty = null;

        private void OnEnable()
        {
            this.pairingArrayProperty = this.serializedObject.FindProperty("enumSelectionPairings");
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EnumDropdownBase enumDropdown = (EnumDropdownBase)this.target;
            Dropdown dropdown = enumDropdown.gameObject.GetComponent<Dropdown>();

            // Early out
            if(dropdown == null)
            {
                return;
            }

            // Build popup options
            string[] popupOptions = new string[dropdown.options.Count + 1];
            popupOptions[0] = "[Not Assigned]";

            for(int i = 0; i < dropdown.options.Count; ++i)
            {
                popupOptions[i + 1] = dropdown.options[i].text;
            }

            // - Begin rendering -
            // Table Headers
            var origFontStyle = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("Enum Value", "Dropdown Option");
            EditorStyles.label.fontStyle = origFontStyle;

            // Enum options
            bool isChanged = false;
            string[] enumNames = enumDropdown.GetEnumNames();
            int[] enumValues = enumDropdown.GetEnumValues();
            var pairAssignments = new EnumDropdownBase.EnumSelectionPair[enumNames.Length];

            for(int i = 0; i < enumValues.Length; ++i)
            {
                EnumDropdownBase.EnumSelectionPair pair;

                // get stored data
                if(!enumDropdown.TryGetPairForEnum(enumValues[i], out pair))
                {
                    pair = new EnumDropdownBase.EnumSelectionPair() {
                        selectionIndex = -1,
                        enumValue = enumValues[i],
                    };
                }

                // render popup
                int oldSelection = pair.selectionIndex;

                ++pair.selectionIndex;
                pair.selectionIndex =
                    EditorGUILayout.Popup(enumNames[i], pair.selectionIndex, popupOptions);
                --pair.selectionIndex;

                // assign to array & check changed
                pairAssignments[i] = pair;
                isChanged |= (oldSelection != pair.selectionIndex);
            }

            // - Update -
            if(isChanged)
            {
                this.pairingArrayProperty.arraySize = pairAssignments.Length;
                for(int i = 0; i < pairAssignments.Length; ++i)
                {
                    var arrayElement = this.pairingArrayProperty.GetArrayElementAtIndex(i);
                    arrayElement.FindPropertyRelative("selectionIndex").intValue =
                        pairAssignments[i].selectionIndex;
                    arrayElement.FindPropertyRelative("enumValue").intValue =
                        pairAssignments[i].enumValue;
                }

                this.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}


#endif // UNITY_EDITOR
