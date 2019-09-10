#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomEditor(typeof(SelectableColorScheme))]
    public class SelectableColorSchemeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            bool apply = GUILayout.Button("Apply to Scene");

            if(apply)
            {
                // Apply to receivers
                SelectableColorApplicator[] applicators = Object.FindObjectsOfType<SelectableColorApplicator>();
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
