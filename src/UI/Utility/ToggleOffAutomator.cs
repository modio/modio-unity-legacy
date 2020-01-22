using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>Automatically disables a sibling toggle on mouse exit.</summary>
    [RequireComponent(typeof(Toggle))]
    public class ToggleOffAutomator : MonoBehaviour, IPointerExitHandler, IDeselectHandler
    {
        // ---------[ Settings ]---------
        /// <summary>Should the toggle be off when disabled?</summary>
        public bool offWhenDisabled = true;

        /// <summary>Should the toggle be off when deselected?</summary>
        public bool offWhenDeselected = true;

        /// <summary>Should the toggle be off when pointer exits?</summary>
        public bool offWhenMouseExits = true;

        // ---------[ Functionality ]--------
        /// <summary>Handle disable event.</summary>
        private void OnDisable()
        {
            if(this.offWhenDisabled)
            {
                this.GetComponent<Toggle>().isOn = false;
            }
        }

        /// <summary>Hnadle deselect event.</summary>
        public void OnDeselect(BaseEventData eventData)
        {
            if(this.offWhenDeselected)
            {
                this.GetComponent<Toggle>().isOn = false;
            }
        }

        /// <summary>Handle PointerExit event.</summary>
        public void OnPointerExit(PointerEventData pointerEventData)
        {
            if(this.offWhenMouseExits)
            {
                this.GetComponent<Toggle>().isOn = false;
            }
        }
    }
}
