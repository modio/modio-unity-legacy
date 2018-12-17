using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// TODO(@jackson): Implemented padding
[RequireComponent(typeof(RectTransform))]
public abstract class JumpScrollRect : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    [Header("Settings")]
    [SerializeField] private bool m_autoHideButtons;
    [SerializeField] private Button m_alignPreviousButton;
    [SerializeField] private Button m_alignNextButton;

    private RectTransform m_rectTransform;
    private bool m_isButtonUpdateRequired;

    // --- ACCESSORS ---
    private IEnumerable<JumpScrollAnchor> GetAnchors()
    {
        return m_rectTransform.GetComponentsInChildren<JumpScrollAnchor>();
    }

    // ---------[ INITIALIZATION ]---------
    protected virtual void OnEnable()
    {
        m_rectTransform = this.GetComponent<RectTransform>();

        if(m_alignPreviousButton != null)
        {
            m_alignPreviousButton.onClick.AddListener(AlignToPreviousAnchor);
        }
        if(m_alignNextButton != null)
        {
            m_alignNextButton.onClick.AddListener(AlignToNextAnchor);
        }

        ResetAlignment();
        m_isButtonUpdateRequired = true;
    }

    protected virtual void OnDisable()
    {
        if(m_alignPreviousButton != null)
        {
            m_alignPreviousButton.onClick.RemoveListener(AlignToPreviousAnchor);
        }
        if(m_alignNextButton != null)
        {
            m_alignNextButton.onClick.RemoveListener(AlignToNextAnchor);
        }
    }

    // ---------[ UPDATES ]---------
    public void UpdateButtonState()
    {
        m_isButtonUpdateRequired = true;
    }

    protected virtual void Update()
    {
        if(m_isButtonUpdateRequired)
        {
            StartCoroutine(LateUpdateButtonState());
            m_isButtonUpdateRequired = false;
        }
    }

    public System.Collections.IEnumerator LateUpdateButtonState()
    {
        yield return null;

        UpdateButtonState_Internal(m_rectTransform, GetAnchors(),
                                   m_alignPreviousButton, m_alignNextButton,
                                   m_autoHideButtons);
    }

    protected abstract void UpdateButtonState_Internal(RectTransform contentTransform,
                                                       IEnumerable<JumpScrollAnchor> anchors,
                                                       Button previousButton,
                                                       Button nextButton,
                                                       bool autoHide);

    // ---------[ SCROLLING ]---------
    public void ResetAlignment()
    {
        Rect rect = m_rectTransform.rect;
        m_rectTransform.anchoredPosition = Vector2.zero;
    }

    public void AlignToNextAnchor()
    {
        m_rectTransform.anchoredPosition = GetNextAlignmentPosition(m_rectTransform, GetAnchors());
        UpdateButtonState();
    }
    protected abstract Vector2 GetNextAlignmentPosition(RectTransform contentTransform,
                                                        IEnumerable<JumpScrollAnchor> anchorList);

    public void AlignToPreviousAnchor()
    {
        m_rectTransform.anchoredPosition = GetPreviousAlignmentPosition(m_rectTransform, GetAnchors());
        UpdateButtonState();
    }
    protected abstract Vector2 GetPreviousAlignmentPosition(RectTransform contentTransform,
                                                            IEnumerable<JumpScrollAnchor> anchorList);

    // ---------[ UTILITY ]---------
    public static Vector2 GetPivotPosition(RectTransform rt)
    {
        return new Vector2(rt.pivot.x * rt.rect.width, rt.pivot.y * rt.rect.height);
    }

    public static Vector2 GetAnchorMinPosition(RectTransform rt)
    {
        RectTransform parent = rt.parent as RectTransform;
        return new Vector2(rt.anchorMin.x * parent.rect.width, rt.anchorMin.y * parent.rect.height);
    }
}
