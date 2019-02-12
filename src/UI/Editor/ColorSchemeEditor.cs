#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO.UI.Editor
{
    [CustomEditor(typeof(ColorScheme))]
    public class ColorSchemeEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            if(ColorScheme.defaultInstance == null)
            {
                serializedObject.Update();
                serializedObject.FindProperty("m_isDefault").boolValue = true;
                serializedObject.ApplyModifiedProperties();

                ColorScheme.SetDefault(this.target as ColorScheme);
            }
        }

        public override void OnInspectorGUI()
        {
            bool isDefault = (ColorScheme.defaultInstance == this.target);

            EditorGUI.BeginChangeCheck();
            {
                base.OnInspectorGUI();
                serializedObject.Update();
            }
            bool updateReceivers = EditorGUI.EndChangeCheck() && isDefault;

            using(new EditorGUI.DisabledScope(isDefault))
            {
                if(GUILayout.Button("Set As Project Default"))
                {
                    if(ColorScheme.defaultInstance != null
                       && ColorScheme.defaultInstance != this.target)
                    {
                        SerializedObject default_so = new SerializedObject(ColorScheme.defaultInstance);
                        default_so.FindProperty("m_isDefault").boolValue = false;
                        default_so.ApplyModifiedProperties();
                    }

                    serializedObject.FindProperty("m_isDefault").boolValue = true;
                    serializedObject.ApplyModifiedProperties();

                    ColorScheme.SetDefault(this.target as ColorScheme);

                    updateReceivers = true;
                }
            }

            if(isDefault)
            {
                EditorGUILayout.HelpBox("This is current the default color scheme",
                                        MessageType.Info);
            }

            if(updateReceivers)
            {
                // Apply to receivers
                Resources.LoadAll<AColorSchemeReceiver>(string.Empty);
                AColorSchemeReceiver[] receievers = Resources.FindObjectsOfTypeAll<AColorSchemeReceiver>();
                foreach(AColorSchemeReceiver csr in receievers)
                {
                    Debug.Log("scheming on " + csr.name);
                    csr.ApplyColorScheme_withUndo(this.target as ColorScheme);
                }
            }
        }
    }
}

#endif
