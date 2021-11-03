using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IEnumerator = System.Collections.IEnumerator;

namespace ModIO.UI
{
    // TODO(@jackson): Implement padding
    // TODO(@jackson): Implement alignments other than bottomLeft
    // TODO(@jackson): Implement vertical jumping
    // TODO(@jackson): Allow for off-axis jumping
    public class JumpScrollRect : MonoBehaviour
    {
        // ---------[ NESTED TYPES ]---------
        public enum ButtonInteractivity
        {
            DoNothing,
            AutoDisable,
            AutoHide,
        }

        // ---------[ CONSTANTS ]---------
        /// <summary>Amount to allow for float errors in jump calculations.</summary>
        public const float JUMP_TOLERANCE = 0.0001f;

        // ---------[ FIELDS ]---------
        [Header("Settings")]
        [SerializeField]
        private ButtonInteractivity m_buttonInteractivity = ButtonInteractivity.AutoHide;
        [SerializeField]
        private Button m_jumpLeftButton = null;
        [SerializeField]
        private Button m_jumpRightButton = null;
        [Tooltip("If at first/last anchor, set the jump target this amount beyond the anchor")]
        [SerializeField]
        private float m_overshootAmount = 0f;

        [Header("UI Components")]
        public RectTransform viewport;
        public RectTransform content;

        private Coroutine m_updateCoroutine;

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
            if(content == null || viewport == null)
            {
                return;
            }

            JumpScrollAnchor[] anchors = content.GetComponentsInChildren<JumpScrollAnchor>();

            // NOTE(@jackson): These are viewport.BL -> anchor.pivot!
            List<Vector2> jumpAnchorPositions = CalcRelativePositions(viewport, anchors);

            // find next jumppos
            int axis = (horizontal ? 0 : 1);
            Vector2 jumpVector;

            if(!positiveDir) // left/down
            {
                jumpVector = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);

                foreach(Vector2 anchorPos in jumpAnchorPositions)
                {
                    bool aheadOfContentBorder = (anchorPos[axis] < -JUMP_TOLERANCE);
                    bool closerThanCurrentJump = (jumpVector[axis] < anchorPos[axis]);

                    if(aheadOfContentBorder && closerThanCurrentJump)
                    {
                        jumpVector = anchorPos;
                    }
                }

                // no jump found
                if(jumpVector[axis] == Mathf.NegativeInfinity)
                {
                    jumpVector = Vector2.zero;
                    jumpVector[axis] -= m_overshootAmount;
                }
            }
            else // right/up
            {
                jumpVector = new Vector2(Mathf.Infinity, Mathf.Infinity);

                foreach(Vector2 anchorPos in jumpAnchorPositions)
                {
                    bool aheadOfContentBorder = (JUMP_TOLERANCE < anchorPos[axis]);
                    bool closerThanCurrentJump = (anchorPos[axis] < jumpVector[axis]);

                    if(aheadOfContentBorder && closerThanCurrentJump)
                    {
                        jumpVector = anchorPos;
                    }
                }

                // no jump found
                if(jumpVector[axis] == Mathf.Infinity)
                {
                    jumpVector = Vector2.zero;
                    jumpVector[axis] += m_overshootAmount;
                }
            }


            // TODO(@jackson): jump on off-axis as well?
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

            Vector2 newContentPos = content.anchoredPosition;
            newContentPos[axis] -= jumpVector[axis];

            content.anchoredPosition = newContentPos;
        }

        // ---------[ CALCULATIONS ]---------
        // NOTE(@jackson): These positions are rootTransform.bottomLeft -> JSA.pivot
        private static List<Vector2> CalcRelativePositions(
            RectTransform rootTransform, IEnumerable<JumpScrollAnchor> jumpAnchors)
        {
            // NOTE(@jackson): Could potentially be optimised with dictionary
            // Dictionary<RectTransform, Vector2> botLeftOffsetMap = new Dictionary<RectTransform,
            // Vector2>();

            List<Vector2> jumpAnchorPositionList = new List<Vector2>();

            foreach(JumpScrollAnchor jumpAnchor in jumpAnchors)
            {
                List<RectTransform> transformStack = new List<RectTransform>();
                RectTransform t = jumpAnchor.transform as RectTransform;

                // create stack with anchor @ 0
                while(t != null && t != rootTransform)
                {
                    transformStack.Add(t);
                    t = t.parent as RectTransform;
                }

                if(t != rootTransform)
                {
                    Debug.LogWarning(
                        "[mod.io] Attempted to calculate offset of non-child JumpScrollAnchor: "
                            + jumpAnchor.name,
                        rootTransform);
                    continue;
                }

                Vector2 culmulativeOffset = Vector2.zero;
                RectTransform tParent = rootTransform;

                // NOTE(@jackson): rootTransform is NOT included
                for(int i = transformStack.Count - 1; i >= 0; --i)
                {
                    t = transformStack[i];

                    // i.BL->j.ancMin       = j.ancMin * i.size
                    // j.ancMin->j.BL       = j.offMin
                    // THUS: i.BL -> j.BL   = (j.aMin * i.size) + j.oMin
                    Vector2 childOffset = new Vector2();
                    childOffset.x = t.anchorMin.x * tParent.rect.width + t.offsetMin.x;
                    childOffset.y = t.anchorMin.y * tParent.rect.height + t.offsetMin.y;

                    culmulativeOffset.x += childOffset.x;
                    culmulativeOffset.y += childOffset.y;

                    tParent = t;
                }

                Debug.Assert(t == jumpAnchor.transform);

                culmulativeOffset.x += t.pivot.x * t.rect.width;
                culmulativeOffset.y += t.pivot.y * t.rect.height;

                jumpAnchorPositionList.Add(culmulativeOffset);
            }

            return jumpAnchorPositionList;
        }

        // ---------[ UPDATES ]---------
        private IEnumerator UpdateButtonsCoroutine()
        {
            while(Application.isPlaying)
            {
                yield return null;

                if(viewport != null && content != null)
                {
                    bool horizontalMovement = viewport.rect.width < content.rect.width;
                    // bool verticalMovement   = viewport.rect.height < content.rect.height;
                    bool hInteractable =
                        (horizontalMovement
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
