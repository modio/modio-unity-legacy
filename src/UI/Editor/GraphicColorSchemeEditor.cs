#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomEditor(typeof(GraphicColorScheme))]
    public class GraphicColorSchemeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            bool apply = GUILayout.Button("Apply to Scene");

            if(apply)
            {
                // Apply to receivers
                GraphicColorApplicator[] applicators = Object.FindObjectsOfType<GraphicColorApplicator>();
                foreach(GraphicColorApplicator gca in applicators)
                {
                    if(gca.scheme == this.target)
                    {
                        gca.UpdateColorScheme_withUndo();
                    }
                }
            }
        }
    }
}

#endif
