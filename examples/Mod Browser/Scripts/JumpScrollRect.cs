using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class JumpScrollRect  : MonoBehaviour
{
    public RectTransform content;
    public Vector2 jumpItemPivot;

    public void ResetAlignment()
    {
        Rect rect = content.rect;
        content.anchoredPosition = Vector2.zero;
    }

    public void AlignToNextChildHorizontally()
    {
        if(content.childCount == 0)
        {
            ResetAlignment();
            return;
        }

        float currentOffset = -content.offsetMin.x;
        float nextOffset = float.MaxValue;

        foreach(Transform t in content.transform)
        {
            RectTransform rt = t as RectTransform;
            float itemOffset = (rt.offsetMin.x * (1-jumpItemPivot.x)
                                + rt.offsetMax.x * (jumpItemPivot.x));

            if(itemOffset > currentOffset
               && itemOffset < nextOffset)
            {
                nextOffset = itemOffset;
            }
        }

        if(nextOffset < float.MaxValue)
        {
            Vector2 pos = content.anchoredPosition;
            pos.x = -nextOffset;
            content.anchoredPosition = pos;
        }
    }

    public void AlignToPrevChildHorizontally()
    {
        if(content.childCount == 0)
        {
            ResetAlignment();
            return;
        }

        float currentOffset = -content.offsetMin.x;
        float prevOffset = -1f;

        foreach(Transform t in content.transform)
        {
            RectTransform rt = t as RectTransform;
            float itemOffset = (rt.offsetMin.x * (1-jumpItemPivot.x)
                                + rt.offsetMax.x * (jumpItemPivot.x));

            if(itemOffset < currentOffset
               && prevOffset < itemOffset)
            {
                prevOffset = itemOffset;
            }
        }

        if(prevOffset >= 0f)
        {
            Vector2 pos = content.anchoredPosition;
            pos.x = -prevOffset;
            content.anchoredPosition = pos;
        }
    }
}
