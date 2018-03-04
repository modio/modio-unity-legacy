using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ModIO
{
    [Serializable]
    public struct TimeStamp : IComparable<TimeStamp>, IEquatable<TimeStamp>
    {
        // --- CONSTS ---
        private static readonly DateTime UNIX_EPOCH = new DateTime(1970,1,1,0,0,0,0,System.DateTimeKind.Utc);

        // --- CREATION FUNCTIONS ---
        public static TimeStamp GenerateFromLocalDateTime(DateTime localDateTime)
        {
            Debug.Assert(localDateTime.Kind == DateTimeKind.Utc,
                         "Provided DateTime is not Local. Please use TimeStamp.GenerateFromUTCDateTime() instead");

            return GenerateFromUTCDateTime(localDateTime.ToUniversalTime());
        }

        public static TimeStamp GenerateFromUTCDateTime(DateTime utcDateTime)
        {
            Debug.Assert(utcDateTime.Kind == DateTimeKind.Utc,
                         "Provided DateTime is not UTC. Consider using TimeStamp.Now() instead of DateTime.Now() or by converting a local time to UTC using the DateTime.ToUniversalTime() method");

            TimeStamp modDT = new TimeStamp();
            modDT.serverTimeStamp = (int)utcDateTime.Subtract(UNIX_EPOCH).TotalSeconds;
            return modDT;
        }

        public static TimeStamp GenerateFromServerTimeStamp(int timeStamp)
        {
            TimeStamp modDT = new TimeStamp();
            modDT.serverTimeStamp = timeStamp;
            return modDT;
        }

        public static TimeStamp Now()
        {
            return GenerateFromUTCDateTime(DateTime.UtcNow);
        }

        // --- VARIABLES ---
        [SerializeField]
        private int serverTimeStamp;

        // --- ACCESSORS ---
        public DateTime AsLocalDateTime()
        {
            DateTime dateTime = UNIX_EPOCH.AddSeconds(serverTimeStamp).ToLocalTime();
            return dateTime;
        }

        public DateTime AsUTCDateTime()
        {
            DateTime dateTime = UNIX_EPOCH.AddSeconds(serverTimeStamp);
            return dateTime;
        }

        public int AsServerTimeStamp()
        {
            return serverTimeStamp;
        }

        // --- INTERFACES ---
        public int CompareTo(TimeStamp other)
        {
        	return serverTimeStamp.CompareTo(other.serverTimeStamp);
        }

        public bool Equals(TimeStamp other)
        {
        	return this.serverTimeStamp == other.serverTimeStamp;
        }

        // --- OPERATOR OVERLOADS ---
        public static bool operator == (TimeStamp a, TimeStamp b)
        {
            return a.serverTimeStamp == b.serverTimeStamp;
        }
        public static bool operator != (TimeStamp a, TimeStamp b)
        {
            return a.serverTimeStamp != b.serverTimeStamp;
        }
        public static bool operator > (TimeStamp a, TimeStamp b)
        {
            return a.serverTimeStamp > b.serverTimeStamp;
        }
        public static bool operator >= (TimeStamp a, TimeStamp b)
        {
            return a.serverTimeStamp >= b.serverTimeStamp;
        }
        public static bool operator < (TimeStamp a, TimeStamp b)
        {
            return a.serverTimeStamp < b.serverTimeStamp;
        }
        public static bool operator <= (TimeStamp a, TimeStamp b)
        {
            return a.serverTimeStamp <= b.serverTimeStamp;
        }

        public override bool Equals(object o)
        {
            if(o == null) { return false; }

            TimeStamp other = (TimeStamp)o;
            return this.Equals(other);
        }

        public override int GetHashCode()
        {
            return serverTimeStamp;
        }
    }

    public static class Utility
    {
        // Author: @jon-hanna of StackOverflow (https://stackoverflow.com/users/400547/jon-hanna)
        // URL: https://stackoverflow.com/a/5419544
        public static bool Like(this string toSearch, string toFind)
        {
            return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(toSearch);
        }

        public static bool IsURL(string toCheck)
        {
            // URL Regex adapted from https://regex.wtf/url-matching-regex-javascript/
            string protocol = "^(http(s)?(://))?(www.)?";
            string domain = "[a-zA-Z0-9-_.]+";
            Regex urlRegex = new Regex(protocol + domain, RegexOptions.IgnoreCase);

            return urlRegex.IsMatch(toCheck);
        }

        public static string GetMD5ForFile(string path)
        {
            Debug.Assert(System.IO.File.Exists(path));
            return GetMD5ForData(System.IO.File.ReadAllBytes(path));
        }

        public static string GetMD5ForData(byte[] data)
        {
            string hashString = "";
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                hashString = BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
            }
            return hashString;
        }
    }

    // Author: @ohad-schneider of StackOverflow (https://stackoverflow.com/users/67824/ohad-schneider)
    // URL: https://stackoverflow.com/a/3790621
    public class MultiSetComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        private readonly IEqualityComparer<T> m_comparer;
        public MultiSetComparer(IEqualityComparer<T> comparer = null)
        {
            m_comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(IEnumerable<T> first, IEnumerable<T> second)
        {
            if (first == null)
                return second == null;

            if (second == null)
                return false;

            if (ReferenceEquals(first, second))
                return true;

            var firstCollection = first as ICollection<T>;
            var secondCollection = second as ICollection<T>;
            if (firstCollection != null && secondCollection != null)
            {
                if (firstCollection.Count != secondCollection.Count)
                    return false;

                if (firstCollection.Count == 0)
                    return true;
            }

            return !HaveMismatchedElement(first, second);
        }

        private bool HaveMismatchedElement(IEnumerable<T> first, IEnumerable<T> second)
        {
            int firstNullCount;
            int secondNullCount;

            var firstElementCounts = GetElementCounts(first, out firstNullCount);
            var secondElementCounts = GetElementCounts(second, out secondNullCount);

            if (firstNullCount != secondNullCount || firstElementCounts.Count != secondElementCounts.Count)
                return true;

            foreach (var kvp in firstElementCounts)
            {
                var firstElementCount = kvp.Value;
                int secondElementCount;
                secondElementCounts.TryGetValue(kvp.Key, out secondElementCount);

                if (firstElementCount != secondElementCount)
                    return true;
            }

            return false;
        }

        private Dictionary<T, int> GetElementCounts(IEnumerable<T> enumerable, out int nullCount)
        {
            var dictionary = new Dictionary<T, int>(m_comparer);
            nullCount = 0;

            foreach (T element in enumerable)
            {
                if (element == null)
                {
                    nullCount++;
                }
                else
                {
                    int num;
                    dictionary.TryGetValue(element, out num);
                    num++;
                    dictionary[element] = num;
                }
            }

            return dictionary;
        }

        public int GetHashCode(IEnumerable<T> enumerable)
        {
            if (enumerable == null) throw new ArgumentNullException();

            int hash = 17;

            foreach (T val in enumerable.OrderBy(x => x))
                hash = hash * 23 + (val == null ? 42 : val.GetHashCode());

            return hash;
        }
    }

    #if UNITY_EDITOR
    public static class EditorGUILayoutExtensions
    {
        public static void ArrayPropertyField(SerializedProperty arrayProperty, string dispName, ref bool isExpanded)
        {
            isExpanded = EditorGUILayout.Foldout(isExpanded, dispName, true);

            if(isExpanded)
            {
                EditorGUI.indentLevel += 3;
         
                EditorGUILayout.PropertyField(arrayProperty.FindPropertyRelative("Array.size"),
                                              new GUIContent("Size"));

                for (int i = 0; i < arrayProperty.arraySize; ++i)
                {
                    SerializedProperty prop = arrayProperty.FindPropertyRelative("Array.data[" + i + "]");
                    EditorGUILayout.PropertyField(prop);
                }

                EditorGUI.indentLevel -= 3;
            }
        }

        public static bool BrowseButton(string buttonContent, GUIContent label)
        {
            bool doBrowse = false;

            if(String.IsNullOrEmpty(buttonContent))
            {
                buttonContent = "Browse...";
            }

            EditorGUILayout.BeginHorizontal();
                if(label != null && label != GUIContent.none)
                {
                    EditorGUILayout.PrefixLabel(label);
                }

                if(Event.current.type == EventType.Layout)
                {
                    EditorGUILayout.TextField(buttonContent);
                }
                else
                {
                    doBrowse = GUILayout.Button(buttonContent, GUI.skin.textField);
                }
            EditorGUILayout.EndHorizontal();

            return doBrowse;
        }

        private static GUILayoutOption[] buttonLayout = new GUILayoutOption[]{ GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight) };
        public static bool UndoButton()
        {
            return GUILayout.Button(UISettings.Instance.EditorTexture_UndoButton,
                                    GUI.skin.label,
                                    buttonLayout);
        }
    }
    #endif
}