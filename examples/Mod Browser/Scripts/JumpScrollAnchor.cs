using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class JumpScrollAnchor : MonoBehaviour
{
    private void OnEnable()
    {
        var scrollRectParents = this.GetComponentsInParent<JumpScrollRect>();

        foreach(JumpScrollRect scrollRect in scrollRectParents)
        {
            scrollRect.UpdateButtonState();
        }
    }

    private void OnDisable()
    {
        var scrollRectParents = this.GetComponentsInParent<JumpScrollRect>();

        foreach(JumpScrollRect scrollRect in scrollRectParents)
        {
            scrollRect.UpdateButtonState();
        }
    }
}
