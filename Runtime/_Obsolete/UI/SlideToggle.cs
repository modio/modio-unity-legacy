using UnityEngine;

namespace ModIO.UI
{
    [System.Obsolete("Use ModIO.UI.SlidingToggle instead.")]
    public class SlideToggle : StateToggleDisplay, UnityEngine.EventSystems.IPointerExitHandler
    {
        public enum SlideAxis
        {
            Horizontal,
            Vertical,
        }

        // ---------[ FIELDS ]---------
        [Header("Settings")]
        [Tooltip("Should the slide button untoggle when the user moves the mouse away?")]
        [SerializeField]
        private bool m_untoggleOnMouseExit = false;
        [SerializeField]
        private SlideAxis m_slideAxis = SlideAxis.Horizontal;
        [SerializeField]
        private float m_slideDuration = 0.15f;
        [Tooltip("Set duration to block clicks for after the slide animation")]
        [SerializeField]
        private float m_reactivateDelay = 0.05f;

        [Header("UI Components")]
        [SerializeField]
        private RectTransform content = null;

        [Header("Display Data")]
        [SerializeField]
        private bool m_isOn = false;

        // --- RUNTIME DATA ---
        private GameObject m_clickBlocker = null;
        private Coroutine m_animation = null;

        // --- ACCESSORS ---
        public override bool isOn
        {
            get {
                return m_isOn;
            }
            set {
                if(m_isOn != value)
                {
                    m_isOn = value;
                    UpdateScroll(true);
                }
            }
        }

        public SlideAxis slideAxis
        {
            get {
                return m_slideAxis;
            }
            set {
                if(m_slideAxis != value)
                {
                    m_slideAxis = value;
                    UpdateScroll(true);
                }
            }
        }

        public bool isAnimating
        {
            get {
                return m_animation != null;
            }
        }

        // ---------[ INITIALIZATION ]---------
        private void OnEnable()
        {
            if(m_clickBlocker == null)
            {
                m_clickBlocker = new GameObject("Click Blocker", typeof(RectTransform));

                RectTransform t = m_clickBlocker.GetComponent<RectTransform>();
                t.SetParent(content);
                t.localScale = Vector3.one;
                t.anchorMin = Vector2.zero;
                t.anchorMax = Vector2.one;
                t.offsetMin = Vector2.zero;
                t.offsetMax = Vector2.zero;

                m_clickBlocker.AddComponent<CanvasRenderer>();
                m_clickBlocker.AddComponent<Touchable>();
                m_clickBlocker.SetActive(false);
            }

            StartCoroutine(LateEnable());
        }
        private System.Collections.IEnumerator LateEnable()
        {
            yield return null;
            UpdateScroll(false);
        }

        private void OnDisable()
        {
            if(m_untoggleOnMouseExit)
            {
                isOn = false;
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        private void UpdateScroll(bool animate)
        {
            if(content == null)
            {
                return;
            }

            Vector2 startPos;
            Vector2 targetPos;
            if(m_slideAxis == SlideAxis.Horizontal)
            {
                if(m_isOn)
                {
                    startPos = SlideToggle.GetLeftPos(content);
                    targetPos = SlideToggle.GetRightPos(content);
                }
                else
                {
                    startPos = SlideToggle.GetRightPos(content);
                    targetPos = SlideToggle.GetLeftPos(content);
                }
            }
            else
            {
                if(m_isOn)
                {
                    startPos = SlideToggle.GetBottomPos(content);
                    targetPos = SlideToggle.GetTopPos(content);
                }
                else
                {
                    startPos = SlideToggle.GetTopPos(content);
                    targetPos = SlideToggle.GetBottomPos(content);
                }
            }


            animate &= (this.isActiveAndEnabled && m_slideDuration > 0f);
            if(animate)
            {
                if(m_animation != null)
                {
                    StopCoroutine(m_animation);
                }

                this.m_animation = StartCoroutine(AnimateScroll(startPos, targetPos));
            }
            else
            {
                content.anchoredPosition = targetPos;
            }
        }

        private System.Collections.IEnumerator AnimateScroll(Vector2 startPos, Vector2 targetPos)
        {
            Vector2 currentPos = content.anchoredPosition;

            float elapsed = 0f;
            float distance = Vector2.Distance(startPos, targetPos);
            float factoredDuration =
                (Vector2.Distance(currentPos, targetPos) / distance) * m_slideDuration;

            m_clickBlocker.SetActive(true);

            while(elapsed < factoredDuration)
            {
                currentPos = Vector2.LerpUnclamped(startPos, targetPos, elapsed / factoredDuration);
                content.anchoredPosition = currentPos;
                elapsed += Time.unscaledDeltaTime;

                yield return null;
            }

            content.anchoredPosition = targetPos;

            // delay enabling buttons
            yield return new WaitForSecondsRealtime(m_reactivateDelay);
            m_clickBlocker.SetActive(false);

            m_animation = null;
        }

        // ---------[ UTILITY ]---------
        private static Vector2 GetLeftPos(RectTransform content)
        {
            Rect pDim = content.parent.GetComponent<RectTransform>().rect;

            // placement of offsetMin.x to left-align
            float offsetPos = -content.anchorMin.x * pDim.width;

            // offset -> pivot
            float pivotDiff = content.anchoredPosition.x - content.offsetMin.x;

            Vector2 pos = new Vector2(offsetPos + pivotDiff, content.anchoredPosition.y);
            return pos;
        }

        private static Vector2 GetRightPos(RectTransform content)
        {
            Rect pDim = content.parent.GetComponent<RectTransform>().rect;

            // placement of offsetMax.x to right-align
            float offsetPos = (1f - content.anchorMax.x) * pDim.width;

            // offset -> pivot
            float pivotDiff = content.anchoredPosition.x - content.offsetMax.x;

            Vector2 pos = new Vector2(offsetPos + pivotDiff, content.anchoredPosition.y);
            return pos;
        }
        private static Vector2 GetBottomPos(RectTransform content)
        {
            Rect pDim = content.parent.GetComponent<RectTransform>().rect;

            // placement of offsetMin.y to bottom-align
            float offsetPos = -content.anchorMin.y * pDim.height;

            // offset -> pivot
            float pivotDiff = content.anchoredPosition.y - content.offsetMin.y;

            Vector2 pos = new Vector2(content.anchoredPosition.x, offsetPos + pivotDiff);
            return pos;
        }

        private static Vector2 GetTopPos(RectTransform content)
        {
            Rect pDim = content.parent.GetComponent<RectTransform>().rect;

            // placement of offsetMax.y to top-align
            float offsetPos = (1f - content.anchorMax.y) * pDim.height;

            // offset -> pivot
            float pivotDiff = content.anchoredPosition.y - content.offsetMax.y;

            Vector2 pos = new Vector2(content.anchoredPosition.x, offsetPos + pivotDiff);
            return pos;
        }

        // ---------[ EVENTS ]---------
        // Detect when Cursor leaves the GameObject
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData pointerEventData)
        {
            if(m_untoggleOnMouseExit)
            {
                isOn = false;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    UpdateScroll(false);
                }
            };
        }
#endif
    }
}
