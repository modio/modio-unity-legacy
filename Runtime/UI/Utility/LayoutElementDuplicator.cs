using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    public class LayoutElementDuplicator : UIBehaviour, ILayoutElement
    {
        // ---------[ FIELDS ]---------
        [SerializeField]
        private RectTransform copySource = null;

        [SerializeField]
        private int m_LayoutPriority = 1;
        [SerializeField]
        private bool m_CopyMinWidth = false;
        [SerializeField]
        private bool m_CopyMinHeight = false;
        [SerializeField]
        private bool m_CopyPreferredWidth = false;
        [SerializeField]
        private bool m_CopyPreferredHeight = false;
        [SerializeField]
        private bool m_CopyFlexibleWidth = false;
        [SerializeField]
        private bool m_CopyFlexibleHeight = false;

        private ILayoutElement[] m_layoutElementSources = new ILayoutElement[0];

        private float m_minWidth = -1;
        private float m_preferredWidth = -1;
        private float m_flexibleWidth = -1;
        private float m_minHeight = -1;
        private float m_preferredHeight = -1;
        private float m_flexibleHeight = -1;

        // Layout horizontal inputs
        public float minWidth
        {
            get {
                return m_minWidth;
            }
        }
        public float preferredWidth
        {
            get {
                return m_preferredWidth;
            }
        }
        public float flexibleWidth
        {
            get {
                return m_flexibleWidth;
            }
        }
        // Layout vertical inputs
        public float minHeight
        {
            get {
                return m_minHeight;
            }
        }
        public float preferredHeight
        {
            get {
                return m_preferredHeight;
            }
        }
        public float flexibleHeight
        {
            get {
                return m_flexibleHeight;
            }
        }

        public int layoutPriority
        {
            get {
                return m_LayoutPriority;
            }
        }

        // After this method is invoked, layout horizontal input properties should return up-to-date
        // values. Children will already have up-to-date layout horizontal inputs when this methods
        // is called.
        public void CalculateLayoutInputHorizontal()
        {
            CalcLayoutHorizontal_Internal();
        }

        private bool CalcLayoutHorizontal_Internal()
        {
            UpdateLayoutSources();

            float newMinWidth = -1f;
            if(m_CopyMinWidth)
            {
                foreach(ILayoutElement element in m_layoutElementSources)
                {
                    newMinWidth = Mathf.Max(element.minWidth, newMinWidth);
                }
            }

            float newPreferredWidth = -1f;
            if(m_CopyPreferredWidth)
            {
                int highestPrio = -1;
                foreach(ILayoutElement element in m_layoutElementSources)
                {
                    if(element.layoutPriority > highestPrio && element.preferredWidth >= 0f)
                    {
                        newPreferredWidth = element.preferredWidth;
                    }
                }
            }

            float newFlexibleWidth = -1f;
            if(m_CopyFlexibleWidth)
            {
                int highestPrio = -1;
                foreach(ILayoutElement element in m_layoutElementSources)
                {
                    if(element.layoutPriority > highestPrio && element.flexibleWidth >= 0f)
                    {
                        newFlexibleWidth = element.flexibleWidth;
                    }
                }
            }

            // update
            bool isDirty = (newMinWidth != m_minWidth);
            isDirty |= (newPreferredWidth != m_preferredWidth);
            isDirty |= (newFlexibleWidth != m_flexibleWidth);

            m_minWidth = newMinWidth;
            m_preferredWidth = newPreferredWidth;
            m_flexibleWidth = newFlexibleWidth;

            return isDirty;
        }

        // After this method is invoked, layout vertical input properties should return up-to-date
        // values. Children will already have up-to-date layout vertical inputs when this methods is
        // called.
        public void CalculateLayoutInputVertical()
        {
            CalcLayoutVertical_Internal();
        }

        private bool CalcLayoutVertical_Internal()
        {
            UpdateLayoutSources();

            float newMinHeight = -1f;
            if(m_CopyMinHeight)
            {
                foreach(ILayoutElement element in m_layoutElementSources)
                {
                    newMinHeight = Mathf.Max(element.minHeight, newMinHeight);
                }
            }

            float newPreferredHeight = -1f;
            if(m_CopyPreferredHeight)
            {
                int highestPrio = -1;
                foreach(ILayoutElement element in m_layoutElementSources)
                {
                    if(element.layoutPriority > highestPrio && element.preferredHeight >= 0f)
                    {
                        newPreferredHeight = element.preferredHeight;
                    }
                }
            }

            float newFlexibleHeight = -1f;
            if(m_CopyFlexibleHeight)
            {
                int highestPrio = -1;
                foreach(ILayoutElement element in m_layoutElementSources)
                {
                    if(element.layoutPriority > highestPrio && element.flexibleHeight >= 0f)
                    {
                        newFlexibleHeight = element.flexibleHeight;
                    }
                }
            }

            // update
            bool isDirty = (newMinHeight != m_minHeight);
            isDirty |= (newPreferredHeight != m_preferredHeight);
            isDirty |= (newFlexibleHeight != m_flexibleHeight);

            m_minHeight = newMinHeight;
            m_preferredHeight = newPreferredHeight;
            m_flexibleHeight = newFlexibleHeight;

            return isDirty;
        }

        private void OnGUI()
        {
            bool isDirty = false;

            isDirty |= CalcLayoutHorizontal_Internal();
            isDirty |= CalcLayoutVertical_Internal();

            if(isDirty)
            {
                SetDirty();
            }
        }

        private void UpdateLayoutSources()
        {
            if(copySource == null)
            {
                m_layoutElementSources = new ILayoutElement[0];
            }
            else
            {
                m_layoutElementSources = copySource.gameObject.GetComponents<ILayoutElement>();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnTransformParentChanged()
        {
            SetDirty();
        }

        protected override void OnDisable()
        {
            SetDirty();
            base.OnDisable();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        protected override void OnBeforeTransformParentChanged()
        {
            SetDirty();
        }

        private bool m_awaitingRebuild = false;
        protected void SetDirty()
        {
            if(!IsActive() || m_awaitingRebuild)
            {
                return;
            }

            if(!CanvasUpdateRegistry.IsRebuildingLayout())
            {
                LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
            }
            else
            {
                m_awaitingRebuild = true;
                StartCoroutine(DelayedSetDirty(transform as RectTransform));
            }
        }

        System.Collections.IEnumerator DelayedSetDirty(RectTransform rectTransform)
        {
            yield return null;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);

            m_awaitingRebuild = false;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirty();
        }
#endif
    }
}
