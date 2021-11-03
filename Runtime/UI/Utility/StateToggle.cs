using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>A wrapper for the Unity UI Toggle component to allow it to be used as a
    /// StateToggleDisplay.</summary>
    [RequireComponent(typeof(Toggle))]
    public class StateToggle : StateToggleDisplay
    {
        /// <summary>Pass-through to the Toggle sibling component.</summary>
        public override bool isOn
        {
            get {
                return this.gameObject.GetComponent<Toggle>().isOn;
            }
            set {
                this.gameObject.GetComponent<Toggle>().SetIsOnWithoutNotify(value);
            }
        }
    }
}
