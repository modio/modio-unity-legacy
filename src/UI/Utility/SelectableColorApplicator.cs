using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(Selectable))]
    public class SelectableColorApplicator : MonoBehaviour
    {
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

            selectable.colors = scheme.functionalColors;
        }

        #if UNITY_EDITOR
        public void UpdateColorScheme_withUndo()
        {
            UnityEditor.Undo.RecordObject(selectable, "Applied Color Scheme");
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
