using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>Scales a RectTransform to fit its parent.</summary>
    public class ScaleFitter : UIBehaviour, ILayoutSelfController
    {
        // ---------[ NESTED TYPES ]---------
        /// <summary>Methods of scaling. Similar to AspectRatioFitter.AspectMode.</summary>
        public enum ScaleMode
        {
            /// <summary>Effectively disables the scaling.</summary>
            Disabled,

            /// <summary>Maintains the aspect ratio, scaling to fill the parent
            /// width-wise.</summary>
            WidthControlsHeight,

            /// <summary>Maintains the aspect ratio, scaling to fill the parent
            /// height-wise.</summary>
            HeightControlsWidth,

            /// <summary>Maintains the aspect ratio, scaling to the largest size that fits the
            /// parent.</summary>
            FitInParent,

            /// <summary>Maintains the aspect ratio, scaling to size necessary to envelope the
            /// parent.</summary>
            EnvelopeParent,

            /// <summary>Matches the size of the parent, ignoring the aspect ratio.</summary>
            Stretch,
        }

        // ---------[ FIELDS ]---------
        /// <summary>The method of scaling used.</summary>
        [SerializeField]
        private ScaleMode m_scaleMode = ScaleMode.Disabled;

        /// <summary>RectTransform component sibling.</summary>
        [System.NonSerialized]
        private RectTransform m_rect;

        /// <summary>DrivenRectTransformTracker</summary>
        private DrivenRectTransformTracker m_tracker;


        // --- ACCESSORS ---
        /// <summary>The method of scaling used.</summary>
        public virtual ScaleMode scaleMode
        {
            get {
                return m_scaleMode;
            }
            set {
                if(m_scaleMode != value)
                {
                    m_scaleMode = value;
                    UpdateRectScale();
                }
            }
        }

        /// <summary>RectTransform component sibling.</summary>
        protected RectTransform rectTransform
        {
            get {
                if(m_rect == null)
                    m_rect = GetComponent<RectTransform>();
                return m_rect;
            }
        }

        /// <summary>Gets the size of the parent transform.</summary>
        protected virtual Vector2 GetParentSize()
        {
            RectTransform parent = rectTransform.parent as RectTransform;
            if(!parent)
            {
                return Vector2.zero;
            }

            return parent.rect.size;
        }

        /// <summary>Gets the size of this GameObject's transform.</summary>
        protected virtual Vector2 GetThisSize()
        {
            return rectTransform.rect.size;
        }

        // ---------[ INITIALIZATION ]---------
        protected ScaleFitter() {}

        // ---------[ SCALE UPDATING ]---------
        /// <summary>Called when this RectTransform changes.</summary>
        protected override void OnRectTransformDimensionsChange()
        {
            UpdateRectScale();
        }

        /// <summary>Called if the RectTransform's parent is changed.</summary>
        protected override void OnTransformParentChanged()
        {
            UpdateRectScale();
        }

        /// <summary>Updates the scale property of the RectTransform.</summary>
        protected virtual void UpdateRectScale()
        {
            m_tracker.Clear();

            if(m_scaleMode == ScaleMode.Disabled)
            {
                rectTransform.localScale = new Vector3(1f, 1f, rectTransform.localScale.z);
            }

            if(!IsActive() || m_scaleMode == ScaleMode.Disabled)
            {
                return;
            }

            ScaleMode calcMode = GetCalculationMode(m_scaleMode);
            Vector3 scale = CalculateScaleValues(calcMode);
            ApplyCalculatedValues(scale, calcMode);
        }

        /// <summary>Determines the scale mode to use for calculations.</summary>
        protected virtual ScaleMode GetCalculationMode(ScaleMode selectedScaleMode)
        {
            ScaleMode scaleMode = selectedScaleMode;
            Vector2 thisSize = rectTransform.rect.size;

            if(thisSize.x == 0f)
            {
                if(scaleMode == ScaleMode.WidthControlsHeight)
                {
                    scaleMode = ScaleMode.Disabled;
                }
                else if(scaleMode == ScaleMode.FitInParent || scaleMode == ScaleMode.EnvelopeParent)
                {
                    scaleMode = ScaleMode.HeightControlsWidth;
                }
            }

            if(thisSize.y == 0f)
            {
                if(scaleMode == ScaleMode.HeightControlsWidth)
                {
                    scaleMode = ScaleMode.Disabled;
                }
                else if(scaleMode == ScaleMode.FitInParent || scaleMode == ScaleMode.EnvelopeParent)
                {
                    scaleMode = ScaleMode.WidthControlsHeight;
                }
            }

            return scaleMode;
        }

        /// <summary>Calculates the scale necessary using the given scale mode.</summary>
        protected virtual Vector3 CalculateScaleValues(ScaleMode calculationScaleMode)
        {
            // calc scales
            Vector2 parentSize = GetParentSize();
            Vector2 thisSize = GetThisSize();

            float xScale = 1f;
            float yScale = 1f;
            float zScale = rectTransform.localScale.z;

            // apply scaling
            switch(calculationScaleMode)
            {
                    // case ScaleMode.Disabled:
                    // No modifications necessary

                case ScaleMode.WidthControlsHeight:
                {
                    xScale = parentSize.x / thisSize.x;
                    yScale = xScale;
                    break;
                }
                case ScaleMode.HeightControlsWidth:
                {
                    yScale = parentSize.y / thisSize.y;
                    xScale = yScale;
                    break;
                }
                case ScaleMode.FitInParent:
                {
                    xScale = yScale =
                        Mathf.Min(parentSize.x / thisSize.x, parentSize.y / thisSize.y);
                    break;
                }
                case ScaleMode.EnvelopeParent:
                {
                    xScale = yScale =
                        Mathf.Max(parentSize.x / thisSize.x, parentSize.y / thisSize.y);
                    break;
                }
                case ScaleMode.Stretch:
                {
                    xScale = parentSize.x / thisSize.x;
                    yScale = parentSize.y / thisSize.y;
                    break;
                }
            }

            return new Vector3(xScale, yScale, zScale);
        }

        /// <summary>Applies the scale and driven properties as necessary.</summary>
        protected virtual void ApplyCalculatedValues(Vector3 calculcatedLocalScale,
                                                     ScaleMode calculcationScaleMode)
        {
            // apply scale
            m_tracker.Add(this, rectTransform,
                          DrivenTransformProperties.ScaleX | DrivenTransformProperties.ScaleY);

            rectTransform.localScale = calculcatedLocalScale;

            // control anchors
            if(m_scaleMode == ScaleMode.FitInParent || m_scaleMode == ScaleMode.EnvelopeParent
               || m_scaleMode == ScaleMode.Stretch)
            {
                m_tracker.Add(this, rectTransform,
                              DrivenTransformProperties.Anchors
                                  | DrivenTransformProperties.AnchoredPosition
                                  | DrivenTransformProperties.Pivot);

                rectTransform.pivot = rectTransform.anchorMin = rectTransform.anchorMax =
                    new Vector2(0.5f, 0.5f);

                rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        // ---------[ EVENTS ]---------
        /// <summary>Calls the UpdateRectScale function.</summary>
        public virtual void SetLayoutHorizontal()
        {
            UpdateRectScale();
        }

        /// <summary>ILayoutSelfController stub. Has no effect.</summary>
        public virtual void SetLayoutVertical()
        {
            // NOTE(@jackson): Is _always_ called in tandem with SetLayoutHorizontal(), and thus
            // needs not call UpdateRectScale();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    UpdateRectScale();
                }
            };
        }
#endif
    }
}
