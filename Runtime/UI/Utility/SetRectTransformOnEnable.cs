using UnityEngine;

namespace ModIO.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SetRectTransformOnEnable : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public bool setAnchorMin = false;
        public Vector2 anchorMin = Vector2.zero;
        public bool setAnchorMax = false;
        public Vector2 anchorMax = Vector2.zero;

        public bool setOffsetMin = false;
        public Vector2 offsetMin = Vector2.zero;
        public bool setOffsetMax = false;
        public Vector2 offsetMax = Vector2.zero;

        public bool setPivot = false;
        public Vector2 pivot = Vector2.zero;

        public bool setAnchoredPos = false;
        public Vector2 anchoredPos = Vector2.zero;

        // ---------[ INITIALIZATION ]---------
        private void OnEnable()
        {
            RectTransform rectTransform = (RectTransform)this.transform;

            if(setAnchorMin)
            {
                rectTransform.anchorMin = anchorMin;
            }
            if(setAnchorMax)
            {
                rectTransform.anchorMax = anchorMax;
            }

            if(setPivot)
            {
                rectTransform.pivot = pivot;
            }
            if(setAnchoredPos)
            {
                rectTransform.anchoredPosition = anchoredPos;
            }

            if(setOffsetMin)
            {
                rectTransform.offsetMin = offsetMin;
            }
            if(setOffsetMax)
            {
                rectTransform.offsetMax = offsetMax;
            }
        }
    }
}
