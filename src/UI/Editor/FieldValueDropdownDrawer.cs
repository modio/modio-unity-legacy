#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace ModIO.UI.EditorCode
{
    /// <summary>Draws a dropdown selection in place of the FieldValueGetter.</summary>
    [CustomPropertyDrawer(typeof(FieldValueGetter.DropdownDisplayAttribute))]
    public class FieldValueDropdownDrawer : PropertyDrawer
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>A structure for pairing the FieldInfo with a path.</summary>
        private struct FieldInfoPath
        {
            public string pathPrefix;
            public FieldInfo info;
        }

        // ---------[ FIELDS ]---------
        /// <summary>Is the dropdown open?</summary>
        public bool isExpanded = false;

        /// <summary>Field Path property.</summary>
        public SerializedProperty fieldPathProperty = null;

        /// <summary>DisplayValues for the popup.</summary>
        public string[] displayValues = null;

        // ---------[ GUI FUNCTIONALITY ]---------
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Generate Display Values
            if(this.fieldPathProperty == null
               || this.displayValues == null)
            {
                FieldValueGetter.DropdownDisplayAttribute dropdownAttribute
                    = (FieldValueGetter.DropdownDisplayAttribute)attribute;

                this.fieldPathProperty = property.FindPropertyRelative("m_fieldPath");

                Debug.Assert(dropdownAttribute != null);

                // Generate field info
                FieldInfo[] rootFields
                    = dropdownAttribute.objectType.GetFields(BindingFlags.Instance | BindingFlags.Public);

                List<FieldInfoPath> fieldPaths = GenerateFieldInfoPathList(rootFields,
                                                                           dropdownAttribute.displayArrays,
                                                                           dropdownAttribute.displayNested,
                                                                           string.Empty);

                // generate strings
                List<string> displayNames = new List<string>(fieldPaths.Count);
                for(int i = 0; i < fieldPaths.Count; ++i)
                {
                    displayNames.Add(fieldPaths[i].pathPrefix + fieldPaths[i].info.Name);
                }

                displayNames.Sort();

                this.displayValues = displayNames.ToArray();
            }

            // Get Selection
            int selectedIndex = -1;
            string selection = fieldPathProperty.stringValue;

            foreach(string fieldPath in this.displayValues)
            {
                ++selectedIndex;

                if(selection == fieldPath) { break; }
            }

            if(selectedIndex > this.displayValues.Length)
            {
                selectedIndex = 0;
            }

            selectedIndex = EditorGUI.Popup(position, "Field Path", selectedIndex, this.displayValues);
            this.fieldPathProperty.stringValue = this.displayValues[selectedIndex];
        }

        // ---------[ UTILITY ]---------
        /// <summary>Generates the FieldInfoPath data for the given FieldInfo.</summary>
        private static List<FieldInfoPath> GenerateFieldInfoPathList(FieldInfo[] infoArray,
                                                                     bool displayArrays,
                                                                     bool displayNested,
                                                                     string pathPrefix)
        {
            List<FieldInfoPath> retVal = new List<FieldInfoPath>();
            List<FieldInfoPath> nested = new List<FieldInfoPath>();

            // process root fields
            foreach(FieldInfo field in infoArray)
            {
                // array check
                if(field.FieldType.IsArray && !displayArrays)
                {
                    continue;
                }

                // add to list
                FieldInfoPath fip = new FieldInfoPath()
                {
                    pathPrefix = pathPrefix,
                    info = field,
                };
                retVal.Add(fip);

                // add nested?
                if(displayNested
                   && field.FieldType.IsClass
                   && field.FieldType != typeof(string))
                {
                    nested.Add(fip);
                }
            }

            // process nested
            foreach(FieldInfoPath nestedInfo in nested)
            {
                string prefix = pathPrefix + nestedInfo.info.Name + ".";
                FieldInfo[] nestedInfoArray
                    = nestedInfo.info.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public);

                retVal.AddRange(GenerateFieldInfoPathList(nestedInfoArray,
                                                          displayArrays,
                                                          displayNested,
                                                          prefix));
            }

            return retVal;
        }
    }
}


#endif
