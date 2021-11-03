using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Allows a Toggle component to present as a slide.</summary>
    public class SlidingToggle : Toggle
    {
        public enum SlideAxis
        {
            LeftOffRightOn,
            TopOffBottomOn,
            RightOffLeftOn,
            BottomOffTopOn,
        }

        // ---------[ FIELDS ]---------
        /// <summary>Event triggered if the toggled is clicked while on.</summary>
        public UnityEvent onClickedWhileOn = new UnityEvent();
        /// <summary>Event triggered if the toggled is clicked while off.</summary>
        public UnityEvent onClickedWhileOff = new UnityEvent();

        [SerializeField]
        private RectTransform m_slideContent = null;
        [Tooltip("When enabled, the isOn value is not toggled via a click/submit action.")]
        [SerializeField]
        private bool m_disableAutoToggle = false;
        [SerializeField]
        private SlideAxis m_slideAxis = SlideAxis.LeftOffRightOn;
        [SerializeField]
        private float m_slideDuration = 0.15f;
        [Tooltip(
            "Duration for which clicks are ignored after animating is completed. A negative value will allow clicking during the slide animation.")]
        [SerializeField]
        private float m_reactivateDelay = 0f;

        // --- RUNTIME DATA ---
        /// <summary>Coroutine playing the slide animation.</summary>
        private Coroutine m_animation = null;

        /// <summary>Was on? (Required because we can't rely on notifications.)</summary>
        private bool m_wasOn = false;

        /// <summary>Is this component currently clickable?</summary>
        private bool IsClickable
        {
            get {
                return (this.m_reactivateDelay < 0f || !this.isAnimating);
            }
        }

        // --- ACCESSORS ---
        public SlideAxis slideAxis
        {
            get {
                return m_slideAxis;
            }
            set {
                if(m_slideAxis != value)
                {
                    m_slideAxis = value;
                    UpdateContentPosition(true);
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
        protected override void Start()
        {
            this.m_wasOn = this.isOn;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StartCoroutine(LateEnable());
        }
        private System.Collections.IEnumerator LateEnable()
        {
            yield return null;
            UpdateContentPosition(false);
        }

        // ---------[ Updates ]---------
        /// <summary>Checks the current state of the Toggle component.</summary>
        private void Update()
        {
            if(this.m_wasOn != this.isOn)
            {
                this.UpdateContentPosition(true);
                this.m_wasOn = this.isOn;
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        private void UpdateContentPosition(bool animate)
        {
            if(this.m_slideContent == null)
            {
                return;
            }

            // - Collect Positions -
            Vector2 startPos;
            Vector2 targetPos;

            // Left to Right
            if((this.m_slideAxis == SlideAxis.LeftOffRightOn && this.isOn)
               || (this.slideAxis == SlideAxis.RightOffLeftOn && !this.isOn))
            {
                startPos = SlidingToggle.GetLeftPos(this.m_slideContent);
                targetPos = SlidingToggle.GetRightPos(this.m_slideContent);
            }
            // Right to Left
            else if((this.m_slideAxis == SlideAxis.RightOffLeftOn && this.isOn)
                    || (this.slideAxis == SlideAxis.LeftOffRightOn && !this.isOn))
            {
                startPos = SlidingToggle.GetRightPos(this.m_slideContent);
                targetPos = SlidingToggle.GetLeftPos(this.m_slideContent);
            }
            // Top to Bottom
            else if((this.m_slideAxis == SlideAxis.TopOffBottomOn && this.isOn)
                    || (this.slideAxis == SlideAxis.BottomOffTopOn && !this.isOn))
            {
                startPos = SlidingToggle.GetTopPos(this.m_slideContent);
                targetPos = SlidingToggle.GetBottomPos(this.m_slideContent);
            }
            // Bottom to Top
            else
            {
                Debug.Assert((this.m_slideAxis == SlideAxis.BottomOffTopOn && this.isOn)
                             || (this.slideAxis == SlideAxis.TopOffBottomOn && !this.isOn));

                startPos = SlidingToggle.GetBottomPos(this.m_slideContent);
                targetPos = SlidingToggle.GetTopPos(this.m_slideContent);
            }

            // - Start Transition -
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
                this.m_slideContent.anchoredPosition = targetPos;
            }
        }

        private System.Collections.IEnumerator AnimateScroll(Vector2 startPos, Vector2 targetPos)
        {
            Vector2 currentPos = this.m_slideContent.anchoredPosition;

            float elapsed = 0f;
            float distance = Vector2.Distance(startPos, targetPos);
            float factoredDuration =
                (Vector2.Distance(currentPos, targetPos) / distance) * m_slideDuration;

            while(elapsed < factoredDuration)
            {
                currentPos = Vector2.LerpUnclamped(startPos, targetPos, elapsed / factoredDuration);
                this.m_slideContent.anchoredPosition = currentPos;
                elapsed += Time.unscaledDeltaTime;

                yield return null;
            }

            this.m_slideContent.anchoredPosition = targetPos;

            // delay enabling buttons
            yield return new WaitForSecondsRealtime(m_reactivateDelay);

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
        /// <summary>Overrides click event.</summary>
        public override void OnPointerClick(PointerEventData eventData)
        {
            if(eventData.button != PointerEventData.InputButton.Left || !this.IsClickable)
            {
                return;
            }

            if(this.isOn)
            {
                this.onClickedWhileOn.Invoke();
            }
            else
            {
                this.onClickedWhileOff.Invoke();
            }

            if(!this.m_disableAutoToggle)
            {
                base.OnPointerClick(eventData);
            }
        }

        /// <summary>Overrides submit event.</summary>
        public override void OnSubmit(BaseEventData eventData)
        {
            if(!this.IsClickable)
            {
                return;
            }

            if(this.isOn)
            {
                this.onClickedWhileOn.Invoke();
            }
            else
            {
                this.onClickedWhileOff.Invoke();
            }

            if(!this.m_disableAutoToggle)
            {
                base.OnSubmit(eventData);
            }
        }
    }
}
