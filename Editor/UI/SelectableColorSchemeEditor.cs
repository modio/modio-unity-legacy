#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomEditor(typeof(SelectableColorScheme))]
    [CanEditMultipleObjects]
    public class SelectableColorSchemeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            bool apply = GUILayout.Button("Update Applicators");

            if(apply)
            {
                // Apply to receivers
                SelectableColorApplicator[] applicators =
                    Object.FindObjectsOfType<SelectableColorApplicator>();
                foreach(SelectableColorApplicator sca in applicators)
                {
                    sca.UpdateColorScheme_withUndo();
                }
            }
        }
    }
}
#endif
