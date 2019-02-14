#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO.UI.Editor
{
    [CustomEditor(typeof(SelectableColorScheme))]
    public class SelectableColorSchemeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                base.OnInspectorGUI();
                serializedObject.Update();
            }

            if(EditorGUI.EndChangeCheck())
            {
                // Apply to receivers
                Resources.LoadAll<SelectableColorApplicator>(string.Empty);
                SelectableColorApplicator[] applicators = Resources.FindObjectsOfTypeAll<SelectableColorApplicator>();
                foreach(SelectableColorApplicator sca in applicators)
                {
                    if(sca.scheme == this.target)
                    {
                        sca.UpdateColorScheme_withUndo();
                    }
                }
            }
        }
    }
}

#endif
