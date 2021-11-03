using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace ModIO.UI
{
    /// <summary>Allows controls to be bound to a functions in editor.</summary>
    public class ViewControlBindings : MonoBehaviour
    {
        // ---------[ Nested Data-Types ]---------
        /// <summary>The Unity Event for buttons inputs.</summary>
        [System.Serializable]
        public class ButtonEvent : UnityEvent
        {
        }

        /// <summary>Enum to indicate when the event should be triggered for a
        /// ButtonBinding.</summary>
        [System.Flags]
        public enum ButtonTriggerCondition
        {
            OnDown = 0x01,
            OnUp = 0x02,
            OnHeld = 0x04,
        }

        /// <summary>Enum to indicate when the event should be triggered for an
        /// AxisBinding.</summary>
        [System.Flags]
        public enum AxisTriggerCondition
        {
            BecameGreaterThan = 0x01,
            BecameLessThan = 0x02,
            BecameEqualTo = 0x04,
            IsGreaterThan = 0x08,
            IsLessThan = 0x10,
            IsEqualTo = 0x20,
        }

        /// <summary>A pairing of the UnityEvent and the control that activates it.</summary>
        [System.Serializable]
        public struct ButtonBinding
        {
            /// <summary>Name of the input.</summary>
            public string inputName;

            /// <summary>When the event should be fired.</summary>
            public ButtonTriggerCondition condition;

            /// <summary>Event to activate.</summary>
            public ButtonEvent actions;
        }

        /// <summary>KeyCode variant of the ButtonBinding struct.</summary>
        [System.Serializable]
        public struct KeyCodeBinding
        {
            /// <summary>Name of the input.</summary>
            public KeyCode keyCode;

            /// <summary>When the event should be fired.</summary>
            public ButtonTriggerCondition condition;

            /// <summary>Event to activate.</summary>
            public ButtonEvent actions;
        }

        /// <summary>The Unity Event for axis inputs.</summary>
        [System.Serializable]
        public class AxisEvent : UnityEvent<float>
        {
        }

        /// <summary>A pairing of the UnityEvent and the control that activates it.</summary>
        [System.Serializable]
        public struct AxisBinding
        {
            /// <summary>Name of the input.</summary>
            public string inputName;

            /// <summary>Threshold value to trigger the event at.</summary>
            public float thresholdValue;

            /// <summary>When the event should be fired.</summary>
            public AxisTriggerCondition condition;

            /// <summary>Event to activate.</summary>
            public AxisEvent actions;
        }

        // ---------[ Fields ]---------
        /// <summary>List of control-function bindings for button inputs.</summary>
        public List<ButtonBinding> buttonBindings = new List<ButtonBinding>();

        /// <summary>List of control-function bindings for axis inputs.</summary>
        public List<KeyCodeBinding> keyCodeBindings = new List<KeyCodeBinding>();

        /// <summary>List of control-function bindings for axis inputs.</summary>
        public List<AxisBinding> axisBindings = new List<AxisBinding>();
    }
}
