using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// TODO(@jackson): Add offset
// TODO(@jackson): Disable buttons
public class HorizontalJumpScrollRect  : MonoBehaviour
{
    public Button scrollLeftButton;
    public Button scrollRightButton;

    [SerializeField] private List<JumpScrollAnchor> m_anchors;
    private RectTransform m_rectTransform;

    private void OnEnable()
    {
        m_rectTransform = this.GetComponent<RectTransform>();
        m_anchors = new List<JumpScrollAnchor>();

        JumpScrollAnchor[] childAnchors = this.GetComponentsInChildren<JumpScrollAnchor>();
        foreach(JumpScrollAnchor anchor in childAnchors)
        {
            if(anchor.scrollRect == null)
            {
                m_anchors.Add(anchor);
                anchor.scrollRect = this;
            }
        }
    }

    private void OnDisable()
    {
        foreach(JumpScrollAnchor anchor in m_anchors)
        {
            anchor.scrollRect = null;
        }

        m_anchors = null;
    }

    public void ResetAlignment()
    {
        Rect rect = m_rectTransform.rect;
        m_rectTransform.anchoredPosition = Vector2.zero;
    }

    public void JumpScrollRight()
    {
        if(m_anchors == null
           || m_anchors.Count == 0)
        {
            ResetAlignment();
            return;
        }

        float currentOffset = -m_rectTransform.anchoredPosition.x;
        float nextOffset = m_rectTransform.rect.width;

        foreach(JumpScrollAnchor anchor in m_anchors)
        {
            RectTransform rectTransform = anchor.transform as RectTransform;
            float anchorOffset = rectTransform.anchoredPosition.x;

            while(rectTransform.parent != null
                  && rectTransform.parent != m_rectTransform)
            {
                RectTransform parentTransform = rectTransform.parent as RectTransform;
                float parentOffset = GetAnchorMinPosOffset(rectTransform).x
                                     - GetPivotPosition(parentTransform).x;
                anchorOffset += parentOffset + parentTransform.anchoredPosition.x;

                rectTransform = parentTransform;
            }

            if(rectTransform.parent == null)
            {
                Debug.LogWarning("[mod.io] Attempted to calculate non-child anchor: "
                                 + anchor.name, this);
                continue;
            }
            else if(currentOffset < anchorOffset
               && anchorOffset < nextOffset)
            {
                nextOffset = anchorOffset;
            }
        }

        if(nextOffset < m_rectTransform.rect.width)
        {
            Vector2 pos = m_rectTransform.anchoredPosition;
            pos.x = -nextOffset;
            m_rectTransform.anchoredPosition = pos;
        }
    }


    public void JumpScrollLeft()
    {
        if(m_anchors == null
           || m_anchors.Count == 0)
        {
            ResetAlignment();
            return;
        }

        float currentOffset = -m_rectTransform.anchoredPosition.x;
        float prevOffset = -0.001f;

        foreach(JumpScrollAnchor anchor in m_anchors)
        {
            RectTransform rectTransform = anchor.transform as RectTransform;
            float anchorOffset = rectTransform.anchoredPosition.x;

            while(rectTransform.parent != null
                  && rectTransform.parent != m_rectTransform)
            {
                RectTransform parentTransform = rectTransform.parent as RectTransform;
                float parentOffset = GetAnchorMinPosOffset(rectTransform).x
                                     - GetPivotPosition(parentTransform).x;
                anchorOffset += parentOffset + parentTransform.anchoredPosition.x;

                rectTransform = parentTransform;
            }

            if(rectTransform.parent == null)
            {
                Debug.LogWarning("[mod.io] Attempted to calculate non-child anchor: "
                                 + anchor.name, this);
                continue;
            }
            else if(anchorOffset < currentOffset
               && prevOffset < anchorOffset)
            {
                prevOffset = anchorOffset;
            }
        }

        if(0f <= prevOffset)
        {
            Vector2 pos = m_rectTransform.anchoredPosition;
            pos.x = -prevOffset;
            m_rectTransform.anchoredPosition = pos;
        }
    }

    public void RegisterAnchor(JumpScrollAnchor anchor)
    {
        if(anchor.scrollRect != null)
        {
            anchor.scrollRect.DeregisterAnchor(anchor);
        }

        anchor.scrollRect = this;
        m_anchors.Add(anchor);
    }

    public void DeregisterAnchor(JumpScrollAnchor anchor)
    {
        if(anchor.scrollRect == this)
        {
            anchor.scrollRect = null;
        }
        #if DEBUG
        else
        {
            Debug.LogWarning("[mod.io] Deregistering anchor with different parent ["
                             + (anchor.scrollRect != null ? anchor.scrollRect.gameObject.name : "NULL") + "]",
                             this);
        }
        #endif

        m_anchors.Remove(anchor);
    }

    public static Vector2 GetPivotPosition(RectTransform rt)
    {
        return new Vector2(rt.pivot.x * rt.rect.width, rt.pivot.y * rt.rect.height);
    }

    public static Vector2 GetAnchorMinPosOffset(RectTransform rt)
    {
        RectTransform parent = rt.parent as RectTransform;
        return new Vector2(rt.anchorMin.x * parent.rect.width, rt.anchorMin.y * parent.rect.height);
    }
}
