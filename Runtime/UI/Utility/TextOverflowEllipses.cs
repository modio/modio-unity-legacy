using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(Text))]
    public class TextOverflowEllipses : MonoBehaviour
    {
        private string m_lastText = string.Empty;
        private Text m_text = null;

        private void Start()
        {
            m_text = this.GetComponent<Text>();
        }

        private void OnGUI()
        {
            if(m_text.text != m_lastText)
            {
                string newText = m_text.text;

                int visibleChars = m_text.cachedTextGenerator.characterCountVisible;
                float boxWidth = this.GetComponent<RectTransform>().rect.width;

                if(visibleChars < newText.Length
                   || boxWidth
                          < m_text
                                .preferredWidth) // cachedTextGeneratorForLayout.GetPreferredWidth(newText))
                {
                    Font font = m_text.font;
                    CharacterInfo charInfo;

                    // ellipses
                    font.GetCharacterInfo('.', out charInfo, m_text.fontSize, m_text.fontStyle);
                    int ellipsesWidth = 3 * charInfo.advance;

                    // calc width
                    int culmativeWidth = ellipsesWidth;
                    for(int i = 0; i < newText.Length; ++i)
                    {
                        font.GetCharacterInfo(newText[i], out charInfo, m_text.fontSize,
                                              m_text.fontStyle);

                        int charWidth = charInfo.advance;
                        if(culmativeWidth + charWidth > boxWidth)
                        {
                            if(i > 0)
                            {
                                --i;
                            }

                            newText = newText.Substring(0, i) + "...";
                            break;
                        }
                        else
                        {
                            culmativeWidth += charWidth;
                        }
                    }

                    m_text.text = newText;
                }

                m_lastText = newText;
            }
        }
    }
}
