using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Fires an event when the count of visible grid cells changes.</summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class GridCellCounter : MonoBehaviour
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Event fired when the number of visible cells changes.</summary>
        [System.Serializable]
        public class CellCountChanged : UnityEngine.Events.UnityEvent<int>
        {
        }

        // ---------[ FIELDS ]---------
        /// <summary>Event fired when the number of visible cells changes.</summary>
        public CellCountChanged onCellCountChanged = null;

        /// <summary>Dimensions last update.</summary>
        private Rect m_lastDimensions = new Rect() { x = -1, y = -1 };

        /// <summary>Cell count last update.</summary>
        private int m_lastCellCount = -1;

        // --- Accessors ---
        /// <summary>Grid Layout component being referenced.</summary>
        public GridLayoutGroup grid
        {
            get {
                return this.GetComponent<GridLayoutGroup>();
            }
        }

        /// <summary>RectTransform component attached to the game object.</summary>
        private RectTransform rectTransform
        {
            get {
                return (RectTransform)this.transform;
            }
        }

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            this.OnGUI();
        }

        private void OnEnable()
        {
            this.OnGUI();
        }

        private void OnGUI()
        {
            if(this.m_lastDimensions != this.rectTransform.rect)
            {
                int newCount = UIUtilities.CalculateGridCellCount(this.grid);
                if(newCount != this.m_lastCellCount && this.onCellCountChanged != null)
                {
                    this.onCellCountChanged.Invoke(newCount);
                }

                this.m_lastCellCount = newCount;
                this.m_lastDimensions = this.rectTransform.rect;
            }
        }
    }
}
