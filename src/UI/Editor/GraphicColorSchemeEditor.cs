#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO.UI.Editor
{
    [CustomEditor(typeof(GraphicColorScheme))]
    public class GraphicColorSchemeEditor : UnityEditor.Editor
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
                Resources.LoadAll<GraphicColorApplicator>(string.Empty);
                GraphicColorApplicator[] applicators = Resources.FindObjectsOfTypeAll<GraphicColorApplicator>();
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
