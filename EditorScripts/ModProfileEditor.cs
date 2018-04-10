#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace ModIO
{
    [CustomEditor(typeof(ScriptableModProfile))]
    public class ModProfileEditor : Editor
    {
        // ---------[ EDITOR CACHING ]---------
        private ModProfile profile;

        private void OnEnable()
        {

        }

        private void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("DISPLAYING MOD PROFILE");
        }
    }
}

#endif
