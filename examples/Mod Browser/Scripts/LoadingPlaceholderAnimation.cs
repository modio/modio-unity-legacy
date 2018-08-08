using UnityEngine;
using UnityEngine.UI;

public class LoadingPlaceholderAnimation : MonoBehaviour
{
    public Image icon;
    public float secondsPerRotation;

    private void Update()
    {
        float degreesPerSecond = -360f / secondsPerRotation;

        RectTransform iconTransform = icon.GetComponent<RectTransform>();
        iconTransform.Rotate(new Vector3(0f, 0f, Time.deltaTime * degreesPerSecond));
    }
}
