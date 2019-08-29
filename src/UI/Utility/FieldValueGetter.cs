using System;
using System.Reflection;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Structure to use for displaying the field of a piece of data.</summary>
    [System.Serializable]
    public struct FieldValueGetter
    {
        /// <summary>Field of the object to display.</summary>
        [SerializeField]
        private string fieldPath;

        /// <summary>Field info for fetching the desired value.</summary>
        private FieldInfo[] fieldInfo;

        /// <summary>Returns the value stored at the given field path.</summary>
        public object GetValue(object objectInstance)
        {
            if(objectInstance == null) { return null; }

            if(this.fieldInfo == null)
            {
                this.fieldInfo = FieldValueGetter.GenerateFieldInfo(objectInstance.GetType(), this.fieldPath);
            }

            if(this.fieldInfo.Length > 0)
            {
                object lastObject = objectInstance;
                for(int i = 0; i < fieldInfo.Length && lastObject != null; ++i)
                {
                    lastObject = fieldInfo[i].GetValue(lastObject);
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
