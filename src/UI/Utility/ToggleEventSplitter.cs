using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Provides an interface for separating the toggle off and toggle on events.</summary>
    [RequireComponent(typeof(Toggle))]
    public class ToggleEventSplitter : MonoBehaviour
    {
        // ---------[ Fields ]---------
        /// <summary>UnityEvent callback for when the toggle component is toggled on.</summary>
        public Toggle.ToggleEvent toggledOn = new Toggle.ToggleEvent();

        /// <summary>UnityEvent callback for when the toggle component is toggled off.</summary>
        public Toggle.ToggleEvent toggledOff = new Toggle.ToggleEvent();

        // ---------[ Initialization ]---------
        /// <summary>Link to Toggle component events.</summary>
        private void Start()
        {
            this.GetComponent<Toggle>().onValueChanged.AddListener(OnValueChanged);
        }

        /// <summary>summary</summary>
        private void OnValueChanged(bool isOn)
        {
            if(isOn)
            {
                this.toggledOn.Invoke(true);
            }
            else
            {
                this.toggledOff.Invoke(false);
            }
        }
    }
}
