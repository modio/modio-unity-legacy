using UnityEngine;

namespace ModIO.UI
{
    public class HorizontalProgressBar : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        [Header("UI Components")]
        [Tooltip("The element to be resized with respect to its parent transform.")]
        public RectTransform barTransform;

        [Header("Display Data")]
        [Range(0f, 1f)]
        [SerializeField]
        private float m_percentComplete;

        // --- ACCESSORS ---
        public float percentComplete
        {
            get {
                return m_percentComplete;
            }
            set {
                if(value < 0f)
                {
                    value = 0f;
                }
                else if(value > 1f)
                {
                    value = 1f;
                }

                m_percentComplete = value;

                UpdateBarSize();
            }
        }

        private RectTransform barParent
        {
            get {
                return barTransform.parent as RectTransform;
            }
        }

        // ---------[ INITIALIZATION ]---------
        private void OnEnable()
        {
            SetBarTransformValues();
            UpdateBarSize();
        }

        private void SetBarTransformValues()
        {
            Vector2 anchorMin = barTransform.anchorMin;
            barTransform.anchorMin = new Vector2(anchorMin.x, 0f);
            barTransform.anchorMax = new Vector2(anchorMin.x, 1f);
            barTransform.pivot = new Vector2(anchorMin.x, 0.5f);
            barTransform.offsetMin = Vector2.zero;
            barTransform.offsetMax = Vector2.zero;
        }

        // ---------[ EVENTS ]---------
        private void UpdateBarSize()
        {
#if UNITY_EDITOR
            if(barTransform == null || barParent == null)
            {
                return;
            }
#endif

            float barWidth = m_percentComplete * barParent.rect.width;
            barTransform.sizeDelta = new Vector2(barWidth, 0f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(barTransform == null || barParent == null)
                {
                    return;
                }

                SetBarTransformValues();
                UpdateBarSize();
            };
        }
#endif
    }
}
