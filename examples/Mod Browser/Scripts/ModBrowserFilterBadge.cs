using UnityEngine;
using UnityEngine.UI;

public class ModBrowserFilterBadge : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    // - Settings -
    [Header("UI Components")]
    public Text textComponent;
    public Button buttonComponent;
    public Image closeImage;
    public float paddingTotal;

    // ---------[ LAYOUTING FUNCTIONALITY ]---------
    public float CalculateWidth(string testValue)
    {
        TextGenerator textGen = new TextGenerator();
        TextGenerationSettings genSettings = textComponent.GetGenerationSettings(textComponent.rectTransform.rect.size);

        return (textGen.GetPreferredWidth(testValue, genSettings)
                + closeImage.rectTransform.rect.width
                + paddingTotal);
    }
}
