using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    public class MessageSystem : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        private static MessageSystem _instance;
        public static MessageSystem instance
        {
            get {
                if(_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<MessageSystem>();
                }
                return _instance;
            }
        }

        [Header("Settings")] [Tooltip(
            "Default base time to display a message (in seconds)")] public float defaultBaseTime =
            1.0f;
        [Tooltip("Additional time per character in the message (in seconds)")]
        public float defaultCharacterTime = 0.1f;

        [Header("UI Components")]
        public MessageDisplay successDialog;
        public MessageDisplay warningDialog;
        public MessageDisplay errorDialog;
        public MessageDisplay infoDialog;

        [Header("Display Data")]
        public List<MessageDisplayData> queuedMessages;

        // --- RUNTIME DATA ---
        private Dictionary<MessageDisplayData.Type, MessageDisplay> m_typeDialogMap =
            new Dictionary<MessageDisplayData.Type, MessageDisplay>();
        private Coroutine m_displayRoutine = null;

        // ---------[ INITIALIZATION ]---------
        private void OnEnable()
        {
            _instance = this;

            m_typeDialogMap.Clear();

            if(infoDialog != null)
            {
                infoDialog.gameObject.SetActive(false);
                m_typeDialogMap[MessageDisplayData.Type.Info] = infoDialog;

                infoDialog.onClick -= OnMessageDisplayClicked;
                infoDialog.onClick += OnMessageDisplayClicked;
            }
            else
            {
                m_typeDialogMap[MessageDisplayData.Type.Info] = null;
            }

            if(successDialog != null)
            {
                successDialog.gameObject.SetActive(false);
                m_typeDialogMap[MessageDisplayData.Type.Success] = successDialog;

                successDialog.onClick -= OnMessageDisplayClicked;
                successDialog.onClick += OnMessageDisplayClicked;
            }
            else
            {
                m_typeDialogMap[MessageDisplayData.Type.Success] = null;
            }

            if(warningDialog != null)
            {
                warningDialog.gameObject.SetActive(false);
                m_typeDialogMap[MessageDisplayData.Type.Warning] = warningDialog;

                warningDialog.onClick -= OnMessageDisplayClicked;
                warningDialog.onClick += OnMessageDisplayClicked;
            }
            else
            {
                m_typeDialogMap[MessageDisplayData.Type.Warning] = null;
            }

            if(errorDialog != null)
            {
                errorDialog.gameObject.SetActive(false);
                m_typeDialogMap[MessageDisplayData.Type.Error] = errorDialog;

                errorDialog.onClick -= OnMessageDisplayClicked;
                errorDialog.onClick += OnMessageDisplayClicked;
            }
            else
            {
                m_typeDialogMap[MessageDisplayData.Type.Error] = null;
            }

            queuedMessages = new List<MessageDisplayData>();
        }

        private void OnDisable()
        {
            if(m_displayRoutine != null)
            {
                this.StopCoroutine(m_displayRoutine);
                m_displayRoutine = null;
            }

            if(_instance == this)
            {
                _instance = null;
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public static void QueueMessage(MessageDisplayData.Type messageType, string messageContent,
                                        float displayDuration = 0f)
        {
            Debug.Assert(!System.String.IsNullOrEmpty(messageContent));

            // early out
            if(instance == null)
            {
                return;
            }

            // check for default duration
            if(displayDuration <= 0f)
            {
                displayDuration = (instance.defaultBaseTime
                                   + messageContent.Length * instance.defaultCharacterTime);
            }

            // queue message
            MessageDisplayData newMessage = new MessageDisplayData() {
                type = messageType,
                content = messageContent,
                displayDuration = displayDuration,
            };

            instance.queuedMessages.Add(newMessage);

            if(Application.isPlaying && instance.isActiveAndEnabled
               && instance.m_displayRoutine == null)
            {
                instance.m_displayRoutine =
                    instance.StartCoroutine(instance.DisplayNextMessageRoutine());
            }
        }

        private bool m_cancelCurrentMessage = false;
        private System.Collections.IEnumerator DisplayNextMessageRoutine()
        {
            MessageDisplayData message = queuedMessages[0];
            MessageDisplay dialog = m_typeDialogMap[message.type];
            ToastAnimationSettings anim = dialog.GetComponent<ToastAnimationSettings>();
            RectTransform rectTransform = dialog.GetComponent<RectTransform>();
            Vector2 origin = rectTransform.anchoredPosition;

            if(dialog != null)
            {
                dialog.content.text = message.content;
                dialog.gameObject.SetActive(true);

                if(anim != null)
                {
                    float animTimer = 0f;

                    while(animTimer < anim.duration)
                    {
                        rectTransform.anchoredPosition =
                            Vector2.Lerp(origin + anim.offset, origin, animTimer / anim.duration);

                        yield return null;

                        animTimer += Time.unscaledDeltaTime;
                    }

                    rectTransform.anchoredPosition = origin;
                }

                float displayTimer = 0f;
                m_cancelCurrentMessage = false;
                while(displayTimer < message.displayDuration && !m_cancelCurrentMessage)
                {
                    yield return null;

                    displayTimer += Time.unscaledDeltaTime;
                }

                if(anim != null)
                {
                    float animTimer = 0f;

                    while(animTimer < anim.duration)
                    {
                        rectTransform.anchoredPosition =
                            Vector2.Lerp(origin, origin + anim.offset, animTimer / anim.duration);

                        yield return null;

                        animTimer += Time.unscaledDeltaTime;
                    }

                    rectTransform.anchoredPosition = origin;
                }

                dialog.gameObject.SetActive(false);
            }

            queuedMessages.Remove(message);
            m_displayRoutine = null;

            if(Application.isPlaying && this != null && this.isActiveAndEnabled
               && queuedMessages.Count > 0)
            {
                m_displayRoutine = StartCoroutine(DisplayNextMessageRoutine());
            }
        }

        private void OnMessageDisplayClicked(MessageDisplay display)
        {
            if(instance.queuedMessages.Count > 0
               && instance.queuedMessages[0].content == display.content.text)
            {
                m_cancelCurrentMessage = true;
            }
        }
    }
}
