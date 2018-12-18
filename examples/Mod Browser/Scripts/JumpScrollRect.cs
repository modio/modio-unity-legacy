using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Single = System.Single;
using IEnumerator = System.Collections.IEnumerator;

namespace ModIO.UI
{
    // TODO(@jackson): Implement padding
    // TODO(@jackson): Implement alignments other than bottomLeft
    // TODO(@jackson): Implement vertical jumping
    // TODO(@jackson): Allow for off-axis jumping
    [RequireComponent(typeof(ScrollRect))]
    public class JumpScrollRect : MonoBehaviour
    {
        public enum ButtonInteractivity
        {
            DoNothing,
            AutoDisable,
            AutoHide,
        };

        // ---------[ FIELDS ]---------
        [Header("Settings")]
        [SerializeField] private ButtonInteractivity m_buttonInteractivity;
        [SerializeField] private Button m_jumpLeftButton;
        [SerializeField] private Button m_jumpRightButton;

        // private Vector2 anchorAlignment;
        private bool m_isButtonUpdateRequired;

        private Coroutine m_updateCoroutine;

        // --- ACCESSORS ---
        private ScrollRect scrollRect { get { return this.gameObject.GetComponent<ScrollRect>(); } }

        // ---------[ INITIALIZATION ]---------
        private void OnEnable()
        {
            if(m_jumpLeftButton != null)
            {
                m_jumpLeftButton.onClick.AddListener(JumpLeft);
            }
            if(m_jumpRightButton != null)
            {
                m_jumpRightButton.onClick.AddListener(JumpRight);
            }

            if(m_buttonInteractivity != ButtonInteractivity.DoNothing)
            {
                m_updateCoroutine = StartCoroutine(UpdateButtonsCoroutine());
            }
        }

        private void OnDisable()
        {
            if(m_jumpLeftButton != null)
            {
                m_jumpLeftButton.onClick.RemoveListener(JumpLeft);
            }
            if(m_jumpRightButton != null)
            {
                m_jumpRightButton.onClick.RemoveListener(JumpRight);
            }

            if(m_updateCoroutine != null)
            {
                StopCoroutine(m_updateCoroutine);
                m_updateCoroutine = null;
            }
        }

        // ---------[ JUMP FUNCTIONALITY ]---------
        public void JumpLeft()
        {
            JumpInternal(true, false);
        }

        public void JumpRight()
        {
            JumpInternal(true, true);
        }

        private void JumpInternal(bool horizontal, bool positiveDir)
        {
            if(scrollRect.content == null
               || scrollRect.viewport == null)
            {
                return;
            }

            JumpScrollAnchor[] anchors = scrollRect.content.GetComponentsInChildren<JumpScrollAnchor>();
            RectTransform c = scrollRect.content;
            RectTransform v = scrollRect.viewport;

            // NOTE(@jackson): These a viewport.BL -> anchor.pivot!
            List<Vector2> jumpAnchorPositions = CalcRelativePositions(v, anchors);

            // find next jumppos
            int axis = (horizontal ? 0 : 1);
            Vector2 jumpVector;

            if(!positiveDir) // left/down
            {
                // v.BL -> c.BL
                Vector2 contentOffset = new Vector2(c.anchorMin.x * v.rect.width + c.offsetMin.x,
                                                    c.anchorMin.y * v.rect.height + c.offsetMin.y);

                jumpVector = new Vector2(contentOffset.x,
                                         contentOffset.y);

                foreach(Vector2 pos in jumpAnchorPositions)
                {
                    if(pos[axis] < 0f
                       && jumpVector[axis] < pos[axis])
                    {
                        jumpVector = pos;
                    }
                }

                if(jumpVector[axis] == contentOffset[axis])
                {
                    jumpVector[axis] -= 10f;
                    jumpVector[1-axis] = 0f;
                }
            }
            else // right/up
            {
                // v.TR -> c.TR
                Vector2 contentOffset = new Vector2((c.anchorMax.x-1f) * v.rect.width + c.offsetMax.x,
                                                    (c.anchorMax.y-1f) * v.rect.height + c.offsetMax.y);

                jumpVector = new Vector2(contentOffset.x,
                                         contentOffset.y);

                foreach(Vector2 pos in jumpAnchorPositions)
                {
                    if(0f < pos[axis]
                       && pos[axis] < jumpVector[axis])
                    {
                        jumpVector = pos;
                    }
                }

                if(jumpVector[axis] == contentOffset[axis])
                {
                    jumpVector[axis] += 10f;
                    jumpVector[1-axis] = 0f;
                }
            }


            // TODO(@jackson): jump vertically as well?
            // // Sort H then V
            // jumpAnchorPositions.Sort((a, b) =>
            // {
            //     float diff = a.x - b.x;

            //     if(diff <= 0.001f)
            //     {
            //         diff = a.y - b.y;
            //     }

            //     if(diff < 0.001f)
            //     {
            //         return -1;
            //     }
            //     else if(diff > 0.001f)
            //     {
            //         return 1;
            //     }
            //     else
            //     {
            //         return 0;
            //     }
            // });

            // work out current position?
            // determine if last


            Vector2 newContentPos = scrollRect.content.anchoredPosition;
            newContentPos[axis] -= jumpVector[axis];

            Debug.Log("ContentPos: " + scrollRect.content.anchoredPosition
                      + "\nnewPos: " + newContentPos);

            scrollRect.content.anchoredPosition = newContentPos;
        }

        // ---------[ CALCULATIONS ]---------
        // NOTE(@jackson): These positions are rootTransform.bottomLeft -> JSA.pivot
        private static List<Vector2> CalcRelativePositions(RectTransform rootTransform,
                                                           IEnumerable<JumpScrollAnchor> jumpAnchors)
        {
            // NOTE(@jackson): Could potentially be optimised with dictionary
            // Dictionary<RectTransform, Vector2> botLeftOffsetMap = new Dictionary<RectTransform, Vector2>();

            List<Vector2> jumpAnchorPositionList = new List<Vector2>();

            foreach(JumpScrollAnchor jumpAnchor in jumpAnchors)
            {
                List<RectTransform> transformStack = new List<RectTransform>();
                RectTransform t = jumpAnchor.transform as RectTransform;

                // create stack with anchor @ 0
                while(t != null
                      && t != rootTransform)
                {
                    transformStack.Add(t);
                    t = t.parent as RectTransform;
                }

                if(t != rootTransform)
                {
                    Debug.LogWarning("[mod.io] Attempted to calculate offset of non-child JumpScrollAnchor: "
                                     + jumpAnchor.name, rootTransform);
                    continue;
                }

                Vector2 culmulativeOffset = Vector2.zero;
                RectTransform tParent = rootTransform;

                // NOTE(@jackson): rootTransform is NOT included
                for(int i = transformStack.Count - 1;
                    i >= 0;
                    --i)
                {
                    t = transformStack[i];

                    // i.BL->j.ancMin       = j.ancMin * i.size
                    // j.ancMin->j.BL       = j.offMin
                    // THUS: i.BL -> j.BL   = (j.aMin * i.size) + j.oMin
                    culmulativeOffset.x += t.anchorMin.x * tParent.rect.width + t.offsetMin.x;
                    culmulativeOffset.y += t.anchorMin.y * tParent.rect.height + t.offsetMin.y;

                    tParent = t;
                }

                Debug.Assert(t == jumpAnchor.transform);

                culmulativeOffset.x += t.pivot.x * t.rect.width;
                culmulativeOffset.y += t.pivot.y * t.rect.height;

                jumpAnchorPositionList.Add(culmulativeOffset);

                Debug.Log("FinalPivotOffset [" + t.name + "] = " + culmulativeOffset);
            }

            return jumpAnchorPositionList;
        }

        // ---------[ UPDATES ]---------
        private IEnumerator UpdateButtonsCoroutine()
        {
            while(Application.isPlaying)
            {
                yield return null;

                if(scrollRect.viewport != null
                   && scrollRect.content != null)
                {
                    bool horizontalMovement = scrollRect.viewport.rect.width < scrollRect.content.rect.width;
                    // bool verticalMovement   = scrollRect.viewport.rect.height < scrollRect.content.rect.height;
                    bool hInteractable = (horizontalMovement
                                          || m_buttonInteractivity != ButtonInteractivity.AutoDisable);
                    bool hActive = (horizontalMovement
                                    || m_buttonInteractivity != ButtonInteractivity.AutoHide);

                    m_jumpLeftButton.interactable = hInteractable;
                    m_jumpRightButton.interactable = hInteractable;
                    m_jumpLeftButton.gameObject.SetActive(hActive);
                    m_jumpRightButton.gameObject.SetActive(hActive);
                }
            }
        }
    }
}
