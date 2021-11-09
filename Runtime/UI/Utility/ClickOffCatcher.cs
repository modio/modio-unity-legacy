using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ClickOffCatcher : MonoBehaviour
    {
        public bool constructOnEnable = true;
        public UnityEngine.Events.UnityEvent clickedOff = null;

        private RectTransform m_blocker = null;
        private bool m_hasCanvas = false;
        private int m_oldCanvasSort = -1;
        private bool m_hasRaycaster = false;

        private void OnEnable()
        {
            if(constructOnEnable)
            {
                m_blocker = ClickOffCatcher.InstantiateBlocker(this.transform as RectTransform);
                m_blocker.GetComponent<Button>().onClick.AddListener(OnButtonClick);

                // add canvas
                Canvas canvas = this.GetComponent<Canvas>();
                m_hasCanvas = (canvas != null);
                if(m_hasCanvas)
                {
                    m_oldCanvasSort = canvas.sortingOrder;
                }
                else // !m_hasCanvas
                {
                    canvas = this.gameObject.AddComponent<Canvas>();
                    canvas.overridePixelPerfect = false;
                    canvas.overrideSorting = true;
                    canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
                }

                canvas.sortingOrder = 30000;

                // add raycaster
                GraphicRaycaster raycaster = this.GetComponent<GraphicRaycaster>();
                m_hasRaycaster = (raycaster != null);
                if(!m_hasRaycaster)
                {
                    raycaster = this.gameObject.AddComponent<GraphicRaycaster>();
                    raycaster.ignoreReversedGraphics = true;
                    raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
                }
            }
        }

        private void OnDisable()
        {
            if(constructOnEnable && m_blocker != null)
            {
                GameObject.Destroy(m_blocker.gameObject);

                if(!m_hasRaycaster)
                {
                    GraphicRaycaster raycaster = this.gameObject.GetComponent<GraphicRaycaster>();
                    if(raycaster != null)
                    {
                        GameObject.Destroy(raycaster);
                    }
                }

                Canvas canvas = this.gameObject.GetComponent<Canvas>();
                if(canvas != null)
                {
                    if(m_hasCanvas)
                    {
                        canvas.sortingOrder = m_oldCanvasSort;
                    }
                    else
                    {
                        GameObject.Destroy(canvas);
                    }
                }
            }
        }

        private void OnButtonClick()
        {
            if(clickedOff != null)
            {
                clickedOff.Invoke();
            }
        }

        public static RectTransform InstantiateBlocker(RectTransform creator)
        {
            Canvas canvas = creator.gameObject.GetComponentInParent<Canvas>();
            if(canvas == null)
            {
                Debug.LogWarning("[mod.io] Unable to instantiate as no parent canvas was found for"
                                 + " the creator object.");
                return null;
            }

            if(canvas != null)
            {
                canvas = canvas.rootCanvas;
            }

            GameObject cocGO = new GameObject("Blocker", typeof(RectTransform));
            cocGO.hideFlags = HideFlags.DontSave;

            // setup transform
            RectTransform cocRT = cocGO.GetComponent<RectTransform>();
            cocRT.SetParent(canvas.transform);
            cocRT.localPosition = Vector3.zero;
            cocRT.localRotation = Quaternion.identity;
            cocRT.localScale = Vector3.one;
            cocRT.anchorMin = Vector2.zero;
            cocRT.anchorMax = Vector2.one;
            cocRT.offsetMin = cocRT.offsetMax = Vector2.zero;

            // add canvas
            Canvas cocCanvas = cocGO.AddComponent<Canvas>();
            cocCanvas.overridePixelPerfect = false;
            cocCanvas.overrideSorting = true;
            cocCanvas.sortingOrder = 29999;
            cocCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

            // add raycaster
            GraphicRaycaster cocGR = cocGO.AddComponent<GraphicRaycaster>();
            cocGR.ignoreReversedGraphics = true;
            cocGR.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            // cocGR.blockingMask = Everything;

            // add canvas renderer
            cocGO.AddComponent<CanvasRenderer>();

            // add touchable
            cocGO.AddComponent<Touchable>();

            // add button
            Button cocB = cocGO.AddComponent<Button>();
            Navigation n = new Navigation();
            n.mode = Navigation.Mode.None;

            cocB.navigation = n;

            return cocRT;
        }
    }
}
