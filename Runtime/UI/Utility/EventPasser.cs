using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>Passes events to another object.</summary>
    public class EventPasser : MonoBehaviour,
                               IMoveHandler,
                               IPointerDownHandler,
                               IPointerUpHandler,
                               IPointerEnterHandler,
                               IPointerExitHandler,
                               IPointerClickHandler,
                               ISubmitHandler,
                               ICancelHandler
    {
        // ---------[ Fields ]---------
        /// <summary>Target object to pass the data to.</summary>
        public GameObject target = null;

        /// <summary>Pass a move event?</summary>
        public bool onMove = false;
        /// <summary>Pass a PointerDown event?</summary>
        public bool onPointerDown = false;
        /// <summary>Pass a PointerUp event?</summary>
        public bool onPointerUp = false;
        /// <summary>Pass a PointerEnter event?</summary>
        public bool onPointerEnter = false;
        /// <summary>Pass a PointerExit event?</summary>
        public bool onPointerExit = false;
        /// <summary>Pass a PointerClick event?</summary>
        public bool onPointerClick = false;
        /// <summary>Pass a Submit event?</summary>
        public bool onSubmit = false;
        /// <summary>Pass a Cancel event?</summary>
        public bool onCancel = false;

        // ---------[ Events ]---------
        /// <summary>OnMove implementation to pass the event to the target object.</summary>
        public void OnMove(AxisEventData eventData)
        {
            if(this.target != null && this.onMove)
            {
                foreach(var targetComponent in this.target.gameObject.GetComponents<IMoveHandler>())
                {
                    if(targetComponent != null
                       && ((MonoBehaviour)targetComponent).isActiveAndEnabled)
                    {
                        targetComponent.OnMove(eventData);
                    }
                }
            }
        }
        /// <summary>OnPointerDown implementation to pass the event to the target object.</summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if(this.target != null && this.onPointerDown)
            {
                foreach(var targetComponent in this.target.gameObject
                            .GetComponents<IPointerDownHandler>())
                {
                    if(targetComponent != null
                       && ((MonoBehaviour)targetComponent).isActiveAndEnabled)
                    {
                        targetComponent.OnPointerDown(eventData);
                    }
                }
            }
        }
        /// <summary>OnPointerUp implementation to pass the event to the target object.</summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if(this.target != null && this.onPointerUp)
            {
                foreach(var targetComponent in this.target.gameObject
                            .GetComponents<IPointerUpHandler>())
                {
                    if(targetComponent != null
                       && ((MonoBehaviour)targetComponent).isActiveAndEnabled)
                    {
                        targetComponent.OnPointerUp(eventData);
                    }
                }
            }
        }
        /// <summary>OnPointerEnter implementation to pass the event to the target object.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if(this.target != null && this.onPointerEnter)
            {
                foreach(var targetComponent in this.target.gameObject
                            .GetComponents<IPointerEnterHandler>())
                {
                    if(targetComponent != null
                       && ((MonoBehaviour)targetComponent).isActiveAndEnabled)
                    {
                        targetComponent.OnPointerEnter(eventData);
                    }
                }
            }
        }
        /// <summary>OnPointerExit implementation to pass the event to the target object.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if(this.target != null && this.onPointerExit)
            {
                foreach(var targetComponent in this.target.gameObject
                            .GetComponents<IPointerExitHandler>())
                {
                    if(targetComponent != null
                       && ((MonoBehaviour)targetComponent).isActiveAndEnabled)
                    {
                        targetComponent.OnPointerExit(eventData);
                    }
                }
            }
        }
        /// <summary>OnPointerClick implementation to pass the event to the target object.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if(this.target != null && this.onPointerClick)
            {
                foreach(var targetComponent in this.target.gameObject
                            .GetComponents<IPointerClickHandler>())
                {
                    if(targetComponent != null
                       && ((MonoBehaviour)targetComponent).isActiveAndEnabled)
                    {
                        targetComponent.OnPointerClick(eventData);
                    }
                }
            }
        }
        /// <summary>OnSubmit implementation to pass the event to the target object.</summary>
        public void OnSubmit(BaseEventData eventData)
        {
            if(this.target != null && this.onSubmit)
            {
                foreach(var targetComponent in this.target.gameObject
                            .GetComponents<ISubmitHandler>())
                {
                    if(targetComponent != null
                       && ((MonoBehaviour)targetComponent).isActiveAndEnabled)
                    {
                        targetComponent.OnSubmit(eventData);
                    }
                }
            }
        }
        /// <summary>OnCancel implementation to pass the event to the target object.</summary>
        public void OnCancel(BaseEventData eventData)
        {
            if(this.target != null && this.onCancel)
            {
                foreach(var targetComponent in this.target.gameObject
                            .GetComponents<ICancelHandler>())
                {
                    if(targetComponent != null
                       && ((MonoBehaviour)targetComponent).isActiveAndEnabled)
                    {
                        targetComponent.OnCancel(eventData);
                    }
                }
            }
        }
    }
}
