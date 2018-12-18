using UnityEngine;

namespace ModIO.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class JumpScrollAnchor : MonoBehaviour
    {
        private void OnEnable()
        {
            UpdateParentButtons();
        }

        private void OnDisable()
        {
            UpdateParentButtons();
        }

        private void UpdateParentButtons()
        {
            var scrollRectParents = this.GetComponentsInParent<JumpScrollRect>();

            foreach(JumpScrollRect scrollRect in scrollRectParents)
            {
                scrollRect.UpdateButtonState();
            }
        }
    }
}
