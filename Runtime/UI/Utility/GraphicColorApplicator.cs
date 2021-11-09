using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif

    [RequireComponent(typeof(Graphic))]
    public class GraphicColorApplicator : MonoBehaviour
    {
        public GraphicColorScheme scheme = null;
        public Graphic[] innerElements = new Graphic[0];

        private Graphic graphic
        {
            get {
                return this.gameObject.GetComponent<Graphic>();
            }
        }

        private void Start()
        {
            UpdateColorScheme();
        }

        public void UpdateColorScheme()
        {
            if(graphic == null || scheme == null)
            {
                return;
            }

            graphic.color = scheme.baseColor;

            foreach(Graphic g in innerElements)
            {
                if(g != null)
                {
                    g.color = scheme.innerElementColor;
                }
            }
        }

#if UNITY_EDITOR
        public void UpdateColorScheme_withUndo()
        {
            if(graphic == null || scheme == null)
            {
                return;
            }

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
