#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public class ModIOAccountHeaderView : ISceneEditorView
    {
        // - ISceneEditorView Interface -
        public string GetViewHeader() { return "Account"; }
        public void OnEnable() {}
        public void OnDisable() {}

        public void OnGUI()
        {
            EditorGUILayout.LabelField("MOD.IO HEADER");

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Welcome " + ModManager.GetActiveUser().username);
                if(GUILayout.Button("Log Out"))
                {
                    ModManager.LogUserOut();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
