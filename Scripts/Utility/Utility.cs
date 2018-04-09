using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using JsonUtility = UnityEngine.JsonUtility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ModIO
{
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

        public static bool TryParseJsonFile<T>(string filePath,
                                               out T targetObject)
        {
            try
            {
                targetObject = JsonUtility.FromJson<T>(File.ReadAllText(filePath));
            }
            #pragma warning disable CS0168
            catch(Exception e)
            {
                targetObject = default(T);
                return false;
            }
            #pragma warning restore CS0168

            return true;
        }

        public static bool TryParseJsonString<T>(string jsonObject,
                                                 out T targetObject)
        {
            try
            {
                targetObject = JsonUtility.FromJson<T>(jsonObject);
            }
            #pragma warning disable CS0168
            catch(Exception e)
            {
                targetObject = default(T);
                return false;
            }
            #pragma warning restore CS0168

            return true;
        }

        public static bool TryLoadTextureFromFile(string filePath,
                                                  out Texture2D texture)
        {
            bool retVal;
            try
            {
                texture = new Texture2D(0, 0);
                texture.LoadImage(File.ReadAllBytes(filePath));
                retVal = true;
            }
            #pragma warning disable CS0168
            catch(Exception e)
            {
                texture = null;
                retVal = false;
            }
            #pragma warning restore CS0168
            return retVal;
        }
    }

    // TODO(@jackson): Remove after ModMediaObject.Equals is removed
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
    public static class EditorGUIExtensions
    {
        public static string MultilineTextField(Rect position, string content)
        {
            bool wasWordWrapEnabled = GUI.skin.textField.wordWrap;
            
            GUI.skin.textField.wordWrap = true;

            string retVal = EditorGUI.TextField(position, content);

            GUI.skin.textField.wordWrap = wasWordWrapEnabled;

            return retVal;
        }

    }

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

        public static bool BrowseButton(string path, GUIContent label)
        {
            bool doBrowse = false;

            if(String.IsNullOrEmpty(path))
            {
                path = "Browse...";
            }

            EditorGUILayout.BeginHorizontal();
                if(label != null && label != GUIContent.none)
                {
                    EditorGUILayout.PrefixLabel(label);
                }

                if(Event.current.type == EventType.Layout)
                {
                    EditorGUILayout.TextField(path);
                }
                else
                {
                    doBrowse = GUILayout.Button(path, GUI.skin.textField);
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

        public static string MultilineTextField(string content)
        {
            Rect controlRect = EditorGUILayout.GetControlRect(false, 130.0f, null);
            return EditorGUIExtensions.MultilineTextField(controlRect, content);
        }
    }
    #endif
}