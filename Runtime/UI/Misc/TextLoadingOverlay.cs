using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class TextLoadingOverlay : MonoBehaviour
    {
        [Tooltip(
            "The text component that is used by a ModIO Display Component to display a value.\nFor example, the text component assigned to the Name Display variable of a Mod Profile Display.")]
        public Text textDisplayComponent;
    }
}
