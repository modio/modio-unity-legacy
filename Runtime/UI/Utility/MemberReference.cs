using System;
using System.Reflection;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Structure to use for displaying the member of an object.</summary>
    [System.Serializable]
    public struct MemberReference
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Property to use for the custom property drawer.</summary>
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
        public class DropdownDisplayAttribute : PropertyAttribute
        {
            /// <summary>Type to use for reflection.</summary>
            public Type objectType = null;
            public bool displayEnumerables = false;
            public bool displayNested = false;
            public string[] membersToIgnore = null;

            public DropdownDisplayAttribute(Type objectType, bool displayEnumerables = false,
                                            bool displayNested = false,
                                            string[] membersToIgnore = null)
            {
                this.objectType = objectType;
                this.displayEnumerables = displayEnumerables;
                this.displayNested = displayNested;
                this.membersToIgnore = membersToIgnore;
            }
        }

        // ---------[ FIELDS ]---------
        /// <summary>Field of the object to display.</summary>
        [SerializeField]
        private string m_memberPath;

        /// <summary>Delegate sequence for fetching member value.</summary>
        private Func<object, object>[] m_delegateSequence;

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initialization</summary>
        public MemberReference(string memberPath = null)
        {
            this.m_memberPath = memberPath;
            this.m_delegateSequence = null;
        }

        /// <summary>Returns the value stored at the given member path.</summary>
        public object GetValue(object objectInstance)
        {
            if(objectInstance == null)
            {
                return null;
            }

            if(this.m_delegateSequence == null)
            {
                this.m_delegateSequence = MemberReference.BuildDelegateSequence(
                    objectInstance.GetType(), this.m_memberPath);
            }

            if(this.m_delegateSequence.Length > 0)
            {
                object lastObject = objectInstance;
                for(int i = 0; i < this.m_delegateSequence.Length && lastObject != null; ++i)
                {
                    lastObject = this.m_delegateSequence[i].Invoke(lastObject);
                }

                return lastObject;
            }

            return null;
        }

        // ---------[ UTILITY ]---------
        /// <summary>Generates a sequence of delegates to fetch the value of the member at the given
        /// path.</summary>
        private static Func<object, object>[] BuildDelegateSequence(Type objectType,
                                                                    string memberPath)
        {
            if(string.IsNullOrEmpty(memberPath))
            {
                return new Func<object, object>[0];
            }

            string[] memberPathElements = memberPath.Split('.');
            Func<object, object>[] delegates = new Func<object, object>[memberPathElements.Length];
            Type lastObjectType = objectType;

            for(int i = 0; i < delegates.Length && lastObjectType != null; ++i)
            {
                MemberInfo[] nextInfo = lastObjectType.GetMember(
                    memberPathElements[i], BindingFlags.Instance | BindingFlags.Public);
                lastObjectType = null;

                if(nextInfo.Length > 0 && nextInfo[0] != null)
                {
                    if(nextInfo[0] is FieldInfo)
                    {
                        FieldInfo fi = (FieldInfo)nextInfo[0];

                        delegates[i] = fi.GetValue;
                        lastObjectType = fi.FieldType;
                    }
                    else if(nextInfo[0] is PropertyInfo)
                    {
                        PropertyInfo pi = (PropertyInfo)nextInfo[0];

                        delegates[i] = (o) => MemberReference.GetPropertyValue(pi, o);
                        lastObjectType = pi.PropertyType;
                    }
                }
            }

            if(lastObjectType != null)
            {
                return delegates;
            }

            return new Func<object, object>[0];
        }

        /// <summary>Helper function for getting the value of a property.</summary>
        private static object GetPropertyValue(PropertyInfo info, object objectInstance)
        {
            return info.GetValue(objectInstance, null);
        }
    }
}
