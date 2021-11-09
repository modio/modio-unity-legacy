#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomEditor(typeof(PluginSettings))]
    public class PluginSettingsEditor : Editor
    {
        SerializedProperty apiURLProperty;

        private void OnEnable()
        {
            serializedObject.FindProperty("m_data").isExpanded = true;

            apiURLProperty = serializedObject.FindProperty("m_data.apiURL");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Insert URL for Test API"))
            {
                apiURLProperty.stringValue = APIClient.API_URL_TESTSERVER + APIClient.API_VERSION;
            }
            if(GUILayout.Button("Insert URL for Production API"))
            {
                apiURLProperty.stringValue =
                    APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION;
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();


            bool isProductionAPIURL =
                (apiURLProperty.stringValue
                 == APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION);
            bool isTestAPIURL = (apiURLProperty.stringValue
                                 == APIClient.API_URL_TESTSERVER + APIClient.API_VERSION);

            using(new EditorGUI.DisabledScope(!isProductionAPIURL && !isTestAPIURL))
            {
                string buttonText = "Locate ID and API Key";
                string httpPrefix = @"https://";

                if(isTestAPIURL)
                {
                    buttonText += " [Test Server]";
                    httpPrefix += "test.";
                }
                else if(!isProductionAPIURL)
                {
                    buttonText += " [Unrecognized API URL]";
                }

                if(GUILayout.Button(buttonText))
                {
                    Application.OpenURL(httpPrefix + @"mod.io/apikey");
                }
            }
        }
    }
}
#endif
