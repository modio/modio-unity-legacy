using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Provides compatibility fixes for functionality missing from earlier versions of
    /// Unity.</summary>
    public static class UIComponentExtensions
    {
#if !UNITY_2019_1_OR_NEWER

        /// <summary>Allows a Toggle value to be set without triggering the onValueChanged
        /// event.</summary>
        public static void SetIsOnWithoutNotify(this Toggle toggle, bool value)
        {
            Toggle.ToggleEvent oldEvent = toggle.onValueChanged;
            toggle.onValueChanged = new Toggle.ToggleEvent();

            toggle.isOn = value;

            toggle.onValueChanged = oldEvent;
        }

#endif
    }
}
