using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class JumpScrollAnchor : MonoBehaviour
{
    public HorizontalJumpScrollRect scrollRect;

    private void OnEnable()
    {
        // Register with HorizontalJumpScrollRect
        scrollRect = this.GetComponentInParent<HorizontalJumpScrollRect>();

        if(scrollRect != null)
        {
            scrollRect.RegisterAnchor(this);
        }
    }

    private void OnDisable()
    {
        if(scrollRect != null)
        {
            scrollRect.DeregisterAnchor(this);
        }

        scrollRect = null;
    }
}
