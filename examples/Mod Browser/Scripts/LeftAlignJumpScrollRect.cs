using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class LeftAlignJumpScrollRect : JumpScrollRect
    {
        // ---------[ INITIALIZATION ]---------
        // c.aPos(20) - c.pPos(40) - c.aMinPos(10) = c.LOffset(-30) -> viewBoundLeft(30)
        // viewBoundLeft(20) + v.width(60) - c.width(100) = -20
        protected override void UpdateButtonState_Internal(RectTransform contentTransform,
                                                           IEnumerable<JumpScrollAnchor> anchors,
                                                           Button previousButton,
                                                           Button nextButton,
                                                           bool autoHide)
        {
            RectTransform viewTransform = contentTransform.parent as RectTransform;

            bool hide = autoHide && (contentTransform.rect.width < viewTransform.rect.width);

            float viewBoundLeft = -(contentTransform.anchoredPosition.x
                                    - GetAnchorMinPosition(contentTransform).x
                                    - GetPivotPosition(contentTransform).x);

            if(previousButton != null)
            {
                bool canJumpLeft = false;
                foreach(JumpScrollAnchor anchor in anchors)
                {
                    float anchorOffset = GetJumpAnchorOffsetLeft(contentTransform, anchor);

                    if(anchorOffset < viewBoundLeft)
                    {
                        canJumpLeft = true;
                        break;
                    }
                }

                previousButton.gameObject.SetActive(!hide);
                previousButton.interactable = canJumpLeft;
            }

            float viewBoundRight = (viewBoundLeft
                                    + viewTransform.rect.width);
            if(nextButton != null)
            {
                bool canJumpRight = false;
                foreach(JumpScrollAnchor anchor in anchors)
                {
                    float anchorRight = GetJumpAnchorOffsetLeft(contentTransform, anchor)
                                        + ((RectTransform)anchor.transform).rect.width;

                    if(viewBoundRight < anchorRight)
                    {
                        canJumpRight = true;
                        break;
                    }
                }

                nextButton.gameObject.SetActive(!hide);
                nextButton.interactable = canJumpRight;
            }
        }

        // ---------[ SCROLLING ]---------
        protected override Vector2 GetPreviousAlignmentPosition(RectTransform contentTransform,
                                                                IEnumerable<JumpScrollAnchor> anchors)
        {
            Debug.Assert(contentTransform != null);
            Debug.Assert(anchors != null);

            float viewBoundLeft = -(contentTransform.anchoredPosition.x
                                    - GetAnchorMinPosition(contentTransform).x
                                    - GetPivotPosition(contentTransform).x);
            float previousOffset = -1f;

            foreach(JumpScrollAnchor anchor in anchors)
            {
                float anchorOffset = GetJumpAnchorOffsetLeft(contentTransform, anchor);

                if(anchorOffset < viewBoundLeft
                   && previousOffset < anchorOffset)
                {
                    previousOffset = anchorOffset;
                }
            }

            if(previousOffset < 0f) { previousOffset = viewBoundLeft; }

            Vector2 pos = contentTransform.anchoredPosition;
            pos.x = -previousOffset;
            return pos;
        }

        protected override Vector2 GetNextAlignmentPosition(RectTransform contentTransform,
                                                            IEnumerable<JumpScrollAnchor> anchors)
        {
            Debug.Assert(contentTransform != null);
            Debug.Assert(anchors != null);

            float viewBoundLeft = -(contentTransform.anchoredPosition.x
                                    - GetAnchorMinPosition(contentTransform).x
                                    - GetPivotPosition(contentTransform).x);
            float nextOffset = contentTransform.rect.width;

            foreach(JumpScrollAnchor anchor in anchors)
            {
                float anchorOffset = GetJumpAnchorOffsetLeft(contentTransform, anchor);

                if(viewBoundLeft < anchorOffset
                   && anchorOffset < nextOffset)
                {
                    nextOffset = anchorOffset;
                }
            }

            if(nextOffset >= contentTransform.rect.width) { nextOffset = viewBoundLeft; }

            Vector2 pos = contentTransform.anchoredPosition;
            pos.x = -nextOffset;
            return pos;
        }

        // ---------[ UPDATE BUTTONS ]---------

        // ---------[ UTILITIES ]---------
        // [    [     [P1    A1     ]    P2]     A2]
        public float GetJumpAnchorOffsetLeft(RectTransform contentTransform, JumpScrollAnchor anchor)
        {
            RectTransform t = anchor.transform as RectTransform;
            float anchorOffset = 0f;

            while(t != contentTransform
                  && t.parent != null)
            {
                RectTransform tParent = t.parent as RectTransform;
                anchorOffset += (t.anchoredPosition.x
                                 + GetAnchorMinPosition(t).x
                                 - GetPivotPosition(t).x);
                t = tParent;
            }

            if(t != contentTransform)
            {
                Debug.LogWarning("[mod.io] Attempted to calculate offset of non-child JumpScrollAnchor: "
                                 + anchor.name, this);
            }

            return anchorOffset;
        }
    }
}
