using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(Selectable))]
    public class SelectableColorApplicator : MonoBehaviour
    {
        public Graphic[] innerElements = new Graphic[0];
        public SelectableColorScheme scheme = null;

        private Selectable selectable
        { get { return this.gameObject.GetComponent<Selectable>(); } }

        public void UpdateColorScheme()
        {
            if(scheme == null) { return; }

            if(selectable.targetGraphic != null)
            {
                selectable.targetGraphic.color = scheme.imageColor;
            }

            foreach(Graphic g in innerElements)
            {
                if(g != null)
                {
                    g.color = scheme.innerElementColor;
                }
            }

            selectable.colors = scheme.functionalColors;
        }

        #if UNITY_EDITOR
        public void UpdateColorScheme_withUndo()
        {
            UnityEditor.Undo.RecordObject(selectable, "Applied Color Scheme");

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
