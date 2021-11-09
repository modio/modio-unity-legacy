#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace ModIO.UI.EditorCode
{
    /// <summary>Draws a dropdown selection in place of the MemberReference.</summary>
    [CustomPropertyDrawer(typeof(MemberReference.DropdownDisplayAttribute))]
    public class MemberReferenceDropdownDrawer : PropertyDrawer
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>A structure for pairing the MemberInfo with a path.</summary>
        private struct MemberInfoPath
        {
            public string pathPrefix;
            public Type memberType;
            public MemberInfo info;
        }

        // ---------[ FIELDS ]---------
        /// <summary>Is the dropdown open?</summary>
        public bool isExpanded = false;

        /// <summary>DisplayValues for the popup.</summary>
        public string[] displayValues = null;

        // ---------[ GUI FUNCTIONALITY ]---------
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Generate Display Values
            if(this.displayValues == null)
            {
                MemberReference.DropdownDisplayAttribute dropdownAttribute =
                    (MemberReference.DropdownDisplayAttribute)attribute;


                Debug.Assert(dropdownAttribute != null);

                // Generate member info
                FieldInfo[] rootFields = dropdownAttribute.objectType.GetFields(
                    BindingFlags.Instance | BindingFlags.Public);

                PropertyInfo[] rootProperties = dropdownAttribute.objectType.GetProperties(
                    BindingFlags.Instance | BindingFlags.Public);

                string[] membersToIgnore = dropdownAttribute.membersToIgnore;
                if(membersToIgnore == null)
                {
                    membersToIgnore = new string[0];
                }

                List<MemberInfoPath> memberPaths =
                    GenerateMemberInfoPathList(rootFields, rootProperties, membersToIgnore,
                                               dropdownAttribute.displayEnumerables,
                                               dropdownAttribute.displayNested, string.Empty);

                // generate strings
                List<string> displayNames = new List<string>(memberPaths.Count);
                for(int i = 0; i < memberPaths.Count; ++i)
                {
                    displayNames.Add(memberPaths[i].pathPrefix + memberPaths[i].info.Name);
                }

                displayNames.Sort();

                this.displayValues = displayNames.ToArray();
            }

            // NOTE(@jackson): This needs to be fetched every time, as when displaying an array,
            // a single instance of MemberReferenceDropdownDrawer is created all elements of
            // the array individually.
            SerializedProperty memberPathProperty = property.FindPropertyRelative("m_memberPath");

            // Get Selection
            int selectedIndex = -1;
            string selection = memberPathProperty.stringValue;

            foreach(string memberPath in this.displayValues)
            {
                ++selectedIndex;

                if(selection == memberPath)
                {
                    break;
                }
            }

            if(selectedIndex > this.displayValues.Length)
            {
                selectedIndex = 0;
            }

            selectedIndex =
                EditorGUI.Popup(position, "Member Path", selectedIndex, this.displayValues);
            memberPathProperty.stringValue = this.displayValues[selectedIndex];
        }

        // ---------[ UTILITY ]---------
        /// <summary>Generates the MemberInfoPath data for the given MemberInfo.</summary>
        private static List<MemberInfoPath> GenerateMemberInfoPathList(
            FieldInfo[] fieldInfoArray, PropertyInfo[] propertyInfoArray, string[] membersToIgnore,
            bool displayEnumerables, bool displayNested, string pathPrefix)
        {
            List<MemberInfoPath> retVal = new List<MemberInfoPath>();
            List<MemberInfoPath> nested = new List<MemberInfoPath>();

            // process root members
            foreach(FieldInfo field in fieldInfoArray)
            {
                // obsolete check
                if(Attribute.IsDefined(field, typeof(ObsoleteAttribute)))
                {
                    continue;
                }

                // enumerable check
                if(!displayEnumerables
                   && MemberReferenceDropdownDrawer.IsTypeEnumerable(field.FieldType))
                {
                    continue;
                }

                // mask check
                string fullName = pathPrefix + field.Name;
                bool skipMember = false;
                for(int i = 0; i < membersToIgnore.Length && !skipMember; ++i)
                {
                    skipMember = (membersToIgnore[i] == fullName);
                }
                if(skipMember)
                {
                    continue;
                }

                // add to list
                MemberInfoPath mip = new MemberInfoPath() {
                    pathPrefix = pathPrefix,
                    memberType = field.FieldType,
                    info = field,
                };
                retVal.Add(mip);

                // add nested?
                if(displayNested && field.FieldType.IsClass && field.FieldType != typeof(string))
                {
                    nested.Add(mip);
                }
            }
            // process root members
            foreach(PropertyInfo property in propertyInfoArray)
            {
                // obsolete check
                if(Attribute.IsDefined(property, typeof(ObsoleteAttribute)))
                {
                    continue;
                }

                // enumerable check
                if(!displayEnumerables
                   && MemberReferenceDropdownDrawer.IsTypeEnumerable(property.PropertyType))
                {
                    continue;
                }

                // mask check
                string fullName = pathPrefix + property.Name;
                bool skipMember = false;
                for(int i = 0; i < membersToIgnore.Length && !skipMember; ++i)
                {
                    skipMember = (membersToIgnore[i] == fullName);
                }
                if(skipMember)
                {
                    continue;
                }

                // add to list
                MemberInfoPath mip = new MemberInfoPath() {
                    pathPrefix = pathPrefix,
                    memberType = property.PropertyType,
                    info = property,
                };
                retVal.Add(mip);

                // add nested?
                if(displayNested && property.PropertyType.IsClass
                   && property.PropertyType != typeof(string))
                {
                    nested.Add(mip);
                }
            }

            // process nested
            foreach(MemberInfoPath nestedInfo in nested)
            {
                string prefix = pathPrefix + nestedInfo.info.Name + ".";

                FieldInfo[] nestedFields =
                    nestedInfo.memberType.GetFields(BindingFlags.Instance | BindingFlags.Public);
                PropertyInfo[] nestedProperties = nestedInfo.memberType.GetProperties(
                    BindingFlags.Instance | BindingFlags.Public);

                retVal.AddRange(GenerateMemberInfoPathList(nestedFields, nestedProperties,
                                                           membersToIgnore, displayEnumerables,
                                                           displayNested, prefix));
            }

            return retVal;
        }

        /// <summary>Determins if a type is Enumerable.</summary>
        private static bool IsTypeEnumerable(Type objectType)
        {
            if(objectType == typeof(string))
            {
                return false;
            }

            foreach(Type i in objectType.GetInterfaces())
            {
                if(i == typeof(IEnumerable)
                   || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    return true;
                }
            }

            return false;
        }
    }

}


#endif
