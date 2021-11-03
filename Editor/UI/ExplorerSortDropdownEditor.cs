#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomPropertyDrawer(typeof(ExplorerSortDropdownController.FieldSelectAttribute))]
    public class ExplorerSortDropdownFieldSelectDrawer : PropertyDrawer
    {
        // ---------[ NESTED DATA-TYPES ]---------
        private struct FieldData
        {
            public string fieldName;
            public string fieldValue;
        }

        // ---------[ STATIC DATA ]---------
        private static FieldData[] fieldData = null;
        private static GUIContent[] popupOptions = null;

        private static void LoadStaticData()
        {
            var props = typeof(ModIO.API.GetAllModsFilterFields).GetFields();

            // load field data
            List<FieldData> dataList = new List<FieldData>(props.Length);
            foreach(var filterFieldProperty in props)
            {
                FieldData data = new FieldData() {
                    fieldName = filterFieldProperty.Name,
                    fieldValue = (string)filterFieldProperty.GetValue(null),
                };
                dataList.Add(data);
            }

            ExplorerSortDropdownFieldSelectDrawer.fieldData = dataList.ToArray();

            // load guicontent
            GUIContent[] content = new GUIContent[dataList.Count];
            for(int i = 0; i < content.Length; ++i)
            {
                content[i] = new GUIContent() {
                    text = dataList[i].fieldName,
                };
            }
            ExplorerSortDropdownFieldSelectDrawer.popupOptions = content;
        }

        // ---------[ GUI FUNCTIONALITY ]---------
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(fieldData == null)
            {
                ExplorerSortDropdownFieldSelectDrawer.LoadStaticData();
            }

            string currentValue = property.stringValue;
            int currentSelectionIndex = -1;

            for(int i = 0; i < fieldData.Length && currentSelectionIndex < 0; ++i)
            {
                if(fieldData[i].fieldValue == currentValue)
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
                property.stringValue = fieldData[newSelectionIndex].fieldValue;
            }
        }
    }
}

#endif
