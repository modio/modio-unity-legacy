using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Controls a StateToggleDisplay component based on whether a field display value is
    /// empty.</summary>
    public class EmptyTextToggle : MonoBehaviour
    {
        // ---------[ Nested Data ]---------
        /// <summary>Describes the polarity of the check.</summary>
        public enum StatePolarity
        {
            OffIfNullOrEmpty,
            OnIfNullOrEmpty,
        }

        // ---------[ Fields ]---------
        /// <summary>StateToggleDisplay to control.</summary>
        public StateToggleDisplay targetToggle = null;

        /// <summary>Wrapper for the text component.</summary>
        public GenericTextComponent textComponent = new GenericTextComponent();

        /// <summary>Polarity of the toggle state.</summary>
        public StatePolarity polarity = StatePolarity.OffIfNullOrEmpty;

        // ---------[ Initialization ]---------
        /// <summary>Collects the attached text component.</summary>
        private void Awake()
        {
            Debug.Assert(this.textComponent.displayComponent != null,
                         "[mod.io] EmptyTextToggle component requires an assigned text component"
                             + " in order to function correctly.",
                         this);

            Debug.Assert(
                this.targetToggle != null,
                "[mod.io] EmptyTextToggle component requires an StateToggleDisplay component"
                    + " in order to function correctly.",
                this);
        }

        /// <summary>summary</summary>
        private void OnEnable()
        {
            this.UpdateToggleState();
        }

        /// <summary>Reassign the toggle value.</summary>
        public void UpdateToggleState()
        {
            if(this.isActiveAndEnabled)
            {
                this.StartCoroutine(this.StartToggleUpdates());
            }
            else
            {
                this.UpdateToggleState_Internal();
            }
        }

        /// <summary>Workaround to ensure that the value change is caught.</summary>
        private System.Collections.IEnumerator StartToggleUpdates()
        {
            this.UpdateToggleState_Internal();

            yield return null;

            this.UpdateToggleState_Internal();
        }

        /// <summary>Updates the toggle value on the StateToggleDisplay.</summary>
        private void UpdateToggleState_Internal()
        {
            bool nullOrEmpty = string.IsNullOrEmpty(this.textComponent.text);
            bool isOn = ((this.polarity == StatePolarity.OnIfNullOrEmpty && nullOrEmpty)
                         || (this.polarity == StatePolarity.OffIfNullOrEmpty && !nullOrEmpty));

            this.targetToggle.isOn = isOn;
        }
    }
}
