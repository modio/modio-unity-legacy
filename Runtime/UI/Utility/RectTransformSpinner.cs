using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class RectTransformSpinner : MonoBehaviour
    {
        public float secondsPerRotation;

        private void Update()
        {
            float degreesPerSecond = -360f / secondsPerRotation;
            this.transform.Rotate(new Vector3(0f, 0f, Time.unscaledDeltaTime * degreesPerSecond));
        }
    }
}
