using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(Selectable))]
    public class SelectableColorApplicator : AColorSchemeReceiver
    {
        private Selectable selectable
        { get { return this.gameObject.GetComponent<Selectable>(); } }

        public override void ApplyColorScheme(ColorScheme scheme)
        {
            selectable.colors = scheme.selectableColors;
        }

        #if UNITY_EDITOR
        public override void ApplyColorScheme_withUndo(ColorScheme scheme)
        {
            UnityEditor.Undo.RecordObject(selectable, "Applied Color Scheme");
            ApplyColorScheme(scheme);
        }
        #endif
    }
}
