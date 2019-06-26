using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ScrollingImageCycle : MonoBehaviour
    {
        public float pixelsPerSecond = 10f;
        public float secondsBetweenRepeat = 1f;

        private float secondsUntilScroll = 0f;

        private void OnEnable()
        {
            Debug.Assert(this.gameObject.GetComponent<Graphic>() != null);

            RectTransform transform = this.transform as RectTransform;
            Vector2 pos = transform.anchoredPosition;
            pos.x = transform.rect.width * -1;
            transform.anchoredPosition = pos;
        }

        private void Update()
        {
            if(secondsUntilScroll <= 0f)
            {
                RectTransform transform = this.transform as RectTransform;
                Rect parentRect = ((RectTransform)transform.parent).rect;

                Vector2 pos = transform.anchoredPosition;
                pos.x += pixelsPerSecond * Time.unscaledDeltaTime;

                if(parentRect.width < pos.x)
                {
                    secondsUntilScroll = secondsBetweenRepeat;

                    pos.x = transform.rect.width * -1;

                    this.gameObject.GetComponent<Graphic>().enabled = false;
                }

                transform.anchoredPosition = pos;
            }
            else
            {
                secondsUntilScroll -= Time.unscaledDeltaTime;

                if(secondsUntilScroll <= 0f)
                {
                    this.gameObject.GetComponent<Graphic>().enabled = true;
                }
            }
        }
    }
}
