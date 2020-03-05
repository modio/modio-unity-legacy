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
        public class ButtonEvent : UnityEvent {}

        /// <summary>A pairing of the UnityEvent and the control that activates it.</summary>
        [System.Serializable]
        public struct ButtonBinding
        {
            /// <summary>Name of the input.</summary>
            public string inputName;

            /// <summary>Should the event be fired on down?</summary>
            public bool fireOnDown;

            /// <summary>Should the event be fired on up?</summary>
            public bool fireOnUp;

            /// <summary>Should the event be fired when held?</summary>
            public bool fireOnHeld;

            /// <summary>Event to activate.</summary>
            public ButtonEvent actions;
        }

        /// <summary>KeyCode variant of the ButtonBinding struct.</summary>
        [System.Serializable]
        public struct KeyCodeBinding
        {
            /// <summary>Name of the input.</summary>
            public KeyCode keyCode;

            /// <summary>Should the event be fired on down?</summary>
            public bool fireOnDown;

            /// <summary>Should the event be fired on up?</summary>
            public bool fireOnUp;

            /// <summary>Should the event be fired when held?</summary>
            public bool fireOnHeld;

            /// <summary>Event to activate.</summary>
            public ButtonEvent actions;
        }

        /// <summary>The Unity Event for axis inputs.</summary>
        [System.Serializable]
        public class AxisEvent : UnityEvent<float> {}

        /// <summary>A pairing of the UnityEvent and the control that activates it.</summary>
        [System.Serializable]
        public struct AxisBinding
        {
            /// <summary>Name of the input.</summary>
            public string inputName;

            /// <summary>Threshold value to trigger the event at.</summary>
            public float thresholdValue;

            /// <summary>Should the event be fired when the threshold is crossed?</summary>
            public bool fireOnBecameGreaterThan;

            /// <summary>Should the event be fired when the threshold is crossed?</summary>
            public bool fireOnBecameLessThan;

            /// <summary>Should the event be fired when the axis value is above the threshold value?</summary>
            public bool fireOnIsGreaterThan;

            /// <summary>Should the event be fired when the axis value is below the threshold value?</summary>
            public bool fireOnIsLessThan;

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
