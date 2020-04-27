#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomEditor(typeof(PluginSettings))]
    public class PluginSettingsEditor : Editor
    {
        SerializedProperty apiURLProperty;
        SerializedProperty gameIdProperty;

        private void OnEnable()
        {
            apiURLProperty = serializedObject.FindProperty("m_data.apiURL");
            gameIdProperty = serializedObject.FindProperty("m_data.gameId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var propEnum = serializedObject.FindProperty("m_data").GetEnumerator();

            while(propEnum.MoveNext())
            {
                SerializedProperty displayProp = propEnum.Current as SerializedProperty;
                EditorGUILayout.PropertyField(displayProp);

                if(displayProp.name.Contains("Directory"))
                {
                    DisplayProcessedDirectory(displayProp.stringValue);
                }
            }

            EditorGUILayout.Space();

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

            serializedObject.ApplyModifiedProperties();


            bool isProductionAPIURL =   (apiURLProperty.stringValue == APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION);
            bool isTestAPIURL =         (apiURLProperty.stringValue == APIClient.API_URL_TESTSERVER + APIClient.API_VERSION);

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

        private void DisplayProcessedDirectory(string directoryValue)
        {
            string processedDir = PluginSettings.ReplaceDirectoryVariables(directoryValue,
                                                                           gameIdProperty.intValue,
                                                                           apiURLProperty.stringValue.StartsWith("https://api.test.mod.io"));

            EditorGUILayout.BeginHorizontal();
                using(new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.LabelField(new GUIContent(processedDir, processedDir));
                }

                bool directoryIsValid = false;

                try
                {
                    string testDir = processedDir;

                    while(!directoryIsValid
                          && !string.IsNullOrEmpty(testDir))
                    {
                        testDir = System.IO.Path.GetDirectoryName(testDir);
                        directoryIsValid = LocalDataStorage.GetDirectoryExists(testDir);
                    }
                }
                catch
                {
                    directoryIsValid = false;
                }

                using(new EditorGUI.DisabledScope(!directoryIsValid))
                {
                    string toolTip = null;
                    if(directoryIsValid)
                    {
                        toolTip = "Locate directory";
                    }
                    else
                    {
                        toolTip = "Invalid directory";
                    }

                    if(GUILayout.Button(new GUIContent("...", toolTip), GUILayout.Width(21), GUILayout.Height(14)))
                    {
                        LocalDataStorage.CreateDirectory(processedDir);

                        EditorUtility.RevealInFinder(processedDir);
                    }
                }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
