using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(Graphic))]
    public class GraphicColorApplicator : MonoBehaviour
    {
        public GraphicColorScheme scheme = null;
        public Graphic[] innerElements = new Graphic[0];

        private Graphic graphic
        { get { return this.gameObject.GetComponent<Graphic>(); } }

        public void UpdateColorScheme()
        {
            if(scheme == null) { return; }

            foreach(Graphic g in innerElements)
            {
                if(g != null)
                {
                    g.color = scheme.innerElementColor;
                }
            }

            graphic.color = scheme.baseColor;
        }

        #if UNITY_EDITOR
        public void UpdateColorScheme_withUndo()
        {
            UnityEditor.Undo.RecordObject(graphic, "Applied Color Scheme");

            foreach(Graphic g in innerElements)
            {
                if(g != null)
                {
                    UnityEditor.Undo.RecordObject(g, "Applied Color Scheme");
                }
            }

            UpdateColorScheme();
        }

        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    UpdateColorScheme_withUndo();
                }
            };
        }
        #endif
    }
}
