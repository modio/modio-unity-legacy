using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Sets the velocity property of a ScrollRect using a float.</summary>
    public class ScrollFloatSetter : MonoBehaviour
    {
        // ---------[ Fields ]---------
        /// <summary>Multiplier to apply to the input values passed.</summary>
        public float inputMultiplier = 500f;

        /// <summary>The ScrollRect component sibling.</summary>
        [HideInInspector]
        public ScrollRect scrollRect = null;

        // ---------[ Initialization ]---------
        /// <summary>Gets the ScrollRect component sibling.</summary>
        private void Awake()
        {
            this.scrollRect = this.GetComponent<ScrollRect>();

            Debug.Assert(
                this.scrollRect != null,
                "[mod.io] This component requires a ScrollRect sibling component to function.");
        }

        // ---------[ Functionality ]---------
        /// <summary>Sets the horizontal velocity.</summary>
        public void SetHorizontalVelocity(float velocity)
        {
            Vector2 v = this.scrollRect.velocity;
            v.x = velocity * this.inputMultiplier;
            this.scrollRect.velocity = v;
        }

        /// <summary>Sets the vertical velocity.</summary>
        public void SetVerticalVelocity(float velocity)
        {
            Vector2 v = this.scrollRect.velocity;
            v.y = velocity * this.inputMultiplier;
            this.scrollRect.velocity = v;
        }
    }
}
