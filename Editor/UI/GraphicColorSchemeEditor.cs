#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomEditor(typeof(GraphicColorScheme))]
    [CanEditMultipleObjects]
    public class GraphicColorSchemeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            bool apply = GUILayout.Button("Update Applicators");

            if(apply)
            {
                // Apply to receivers
                GraphicColorApplicator[] applicators =
                    Object.FindObjectsOfType<GraphicColorApplicator>();
                foreach(GraphicColorApplicator gca in applicators)
                {
                    gca.UpdateColorScheme_withUndo();
                }
            }
        }
    }
}

#endif
