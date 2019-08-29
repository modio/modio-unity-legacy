using System;
using System.Reflection;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Structure to use for displaying the field of a piece of data.</summary>
    [System.Serializable]
    public struct FieldValueGetter
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Property to use for the custom property drawer.</summary>
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
        public class DropdownDisplayAttribute : PropertyAttribute
        {
            /// <summary>Type to use for reflection.</summary>
            public Type objectType = null;
            public bool displayArrays = false;
            public bool displayNested = false;

            public DropdownDisplayAttribute(Type objectType, bool displayArrays = false, bool displayNested = false)
            {
                this.objectType = objectType;
                this.displayArrays = displayArrays;
                this.displayNested = displayNested;
            }
        }

        // ---------[ FIELDS ]---------
        /// <summary>Field of the object to display.</summary>
        [SerializeField]
        private string m_fieldPath;

        /// <summary>Field info for fetching the desired value.</summary>
        private FieldInfo[] m_fieldInfo;

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initialization</summary>
        public FieldValueGetter(string fieldPath = null)
        {
            this.m_fieldPath = fieldPath;
            this.m_fieldInfo = null;
        }

        /// <summary>Returns the value stored at the given field path.</summary>
        public object GetValue(object objectInstance)
        {
            if(objectInstance == null) { return null; }

            if(this.m_fieldInfo == null)
            {
                this.m_fieldInfo = FieldValueGetter.GenerateFieldInfo(objectInstance.GetType(), this.m_fieldPath);
            }

            if(this.m_fieldInfo.Length > 0)
            {
                object lastObject = objectInstance;
                for(int i = 0; i < this.m_fieldInfo.Length && lastObject != null; ++i)
                {
                    lastObject = this.m_fieldInfo[i].GetValue(lastObject);
                }

                if(lastObject != null)
                {
                    return lastObject;
                }
            }

            return null;
        }

        /// <summary>Generates a FieldInfo array based on the given field path.</summary>
        public static FieldInfo[] GenerateFieldInfo(Type objectType, string fieldPath)
        {
            if(string.IsNullOrEmpty(fieldPath)) { return new FieldInfo[0]; }

            string[] fieldPathElements = fieldPath.Split('.');
            FieldInfo[] info = new FieldInfo[fieldPathElements.Length];
            Type lastObjectType = objectType;

            for(int i = 0; i < info.Length && lastObjectType != null; ++i)
            {
                FieldInfo nextInfo = lastObjectType.GetField(fieldPathElements[i]);
                lastObjectType = null;

                if(nextInfo != null)
                {
                    info[i] = nextInfo;
                    lastObjectType = nextInfo.FieldType;
                }
            }

            if(lastObjectType != null)
            {
                return info;
            }
            return new FieldInfo[0];
        }
    }
}
