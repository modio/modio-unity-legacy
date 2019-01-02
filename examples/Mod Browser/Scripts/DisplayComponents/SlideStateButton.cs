using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(ScrollRect))]
    public class SlideStateButton : MonoBehaviour, UnityEngine.EventSystems.IPointerExitHandler
    {
        public enum SlideAxis
        {
            Horizontal,
            Vertical,
        }

        // ---------[ FIELDS ]---------
        [SerializeField] private bool m_isToggled = false;
        [Tooltip("Should the slide button untoggle when the user moves the mouse away?")]
        [SerializeField] private bool m_untoggleOnMouseExit = false;
        [SerializeField] private SlideAxis m_slideAxis = SlideAxis.Horizontal;
        [SerializeField] private float m_slideDuration = 0.15f;

        [SerializeField] private UnityEvent m_onUntoggledClick = new UnityEvent();
        [SerializeField] private UnityEvent m_onToggledClick = new UnityEvent();

        private Coroutine m_animation = null;

        // --- ACCESSORS ---
        public UnityEvent onUntoggledClick
        {
            get { return m_onUntoggledClick; }
            set { m_onUntoggledClick = value; }
        }
        public UnityEvent onToggledClick
        {
            get { return m_onToggledClick; }
            set { m_onToggledClick = value; }
        }

        public bool isToggled
        {
            get { return m_isToggled; }
            set
            {
                if(m_isToggled != value)
                {
                    m_isToggled = value;
                    UpdateScroll(true);
                }
            }
        }

        public SlideAxis slideAxis
        {
            get { return m_slideAxis; }
            set
            {
                if(m_slideAxis != value)
                {
                    m_slideAxis = value;
                    UpdateScroll(true);
                }
            }
        }

        private void UpdateScroll(bool animate)
        {
            float pos = (m_isToggled ? 1f : 0f);

            if(animate && m_slideDuration > 0f)
            {
                if(m_animation != null)
                {
                    StopCoroutine(m_animation);
                }

                if(m_slideAxis == SlideAxis.Horizontal)
                {
                    m_animation = StartCoroutine(AnimateScroll_H(pos));
                }
                else
                {
                    m_animation = StartCoroutine(AnimateScroll_V(pos));
                }
            }
            else
            {
                if(m_slideAxis == SlideAxis.Horizontal)
                {
                    scrollRect.horizontalNormalizedPosition = pos;
                }
                else
                {
                    scrollRect.verticalNormalizedPosition = pos;
                }
            }
        }

        private System.Collections.IEnumerator AnimateScroll_H(float targetPos)
        {
            button.interactable = false;

            float elapsed = 0f;
            float startPos = scrollRect.horizontalNormalizedPosition;

            float factoredDuration = m_slideDuration;
            if(startPos < targetPos)
            {
                factoredDuration *= (targetPos - startPos);
            }
            else
            {
                factoredDuration *= (startPos - targetPos);
            }

            while(elapsed < factoredDuration)
            {
                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPos, targetPos, elapsed / factoredDuration);

                elapsed += Time.deltaTime;

                yield return null;
            }

            scrollRect.horizontalNormalizedPosition = targetPos;
            button.interactable = true;
            m_animation = null;
        }

        private System.Collections.IEnumerator AnimateScroll_V(float targetPos)
        {
            button.interactable = false;

            float elapsed = 0f;
            float startPos = scrollRect.verticalNormalizedPosition;

            float factoredDuration = m_slideDuration;
            if(startPos < targetPos)
            {
                factoredDuration *= (targetPos - startPos);
            }
            else
            {
                factoredDuration *= (startPos - targetPos);
            }

            while(elapsed < factoredDuration)
            {
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPos, targetPos, elapsed / factoredDuration);

                elapsed += Time.deltaTime;

                yield return null;
            }

            scrollRect.verticalNormalizedPosition = targetPos;
            button.interactable = true;
            m_animation = null;
        }

        private ScrollRect scrollRect
        {
            get { return this.gameObject.GetComponent<ScrollRect>(); }
        }

        private Button button
        {
            get { return this.gameObject.GetComponent<Button>(); }
        }

        // ---------[ INITIALIZATION ]---------
        protected virtual void Start()
        {
            button.onClick.AddListener(NotifyClick);
        }

        // ---------[ EVENTS ]---------
        private void NotifyClick()
        {
            if(m_isToggled)
            {
                if(onToggledClick != null)
                {
                    onToggledClick.Invoke();
                }
            }
            else
            {
                if(onUntoggledClick != null)
                {
                    onUntoggledClick.Invoke();
                }
            }
        }

        //Detect when Cursor leaves the GameObject
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData pointerEventData)
        {
            if(m_untoggleOnMouseExit)
            {
                isToggled = false;
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null
                   && this.gameObject != null
                   && scrollRect != null)
                {
                    UpdateScroll(false);
                }
            };
        }
        #endif
    }
}
