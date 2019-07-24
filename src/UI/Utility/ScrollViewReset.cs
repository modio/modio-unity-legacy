using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>A component that resets a scroll view in OnEnable.</summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollViewReset : MonoBehaviour
    {
        public float verticalPosition = 1f;
        public float horizontalPosition = 1f;

        private void OnEnable()
        {
            this.GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
            this.GetComponent<ScrollRect>().horizontalNormalizedPosition = 1f;
        }
    }
}
