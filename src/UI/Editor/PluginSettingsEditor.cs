#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO.UI.Editor
{
    [CustomEditor(typeof(PluginSettings))]
    public class PluginSettingsEditor : UnityEditor.Editor
    {
        SerializedProperty apiURLProperty;

        private void OnEnable()
        {
            apiURLProperty = serializedObject.FindProperty("m_data.apiURL");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Insert URL for Test API"))
            {
                apiURLProperty.stringValue = APIClient.API_URL_TESTSERVER + APIClient.API_VERSION;
            }
            if(GUILayout.Button("Insert URL for Production API"))
            {
                apiURLProperty.stringValue = APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION;
            }
            EditorGUILayout.EndHorizontal();

            var propEnum = serializedObject.FindProperty("m_data").GetEnumerator();

            while(propEnum.MoveNext())
            {
                EditorGUILayout.PropertyField(propEnum.Current as SerializedProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
