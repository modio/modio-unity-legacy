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
            get
            {
                if(_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<MessageSystem>();
                }
                return _instance;
            }
        }

        [Header("Settings")]
        [Tooltip("Default base time to display a message (in seconds)")]
        public float defaultBaseTime = 4.0f;
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
        private Dictionary<MessageDisplayData.Type, MessageDisplay> m_typeDialogMap = new Dictionary<MessageDisplayData.Type, MessageDisplay>();
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
            }
            else
            {
                m_typeDialogMap[MessageDisplayData.Type.Info] = null;
            }

            if(successDialog != null)
            {
                successDialog.gameObject.SetActive(false);
                m_typeDialogMap[MessageDisplayData.Type.Success] = successDialog;
            }
            else
            {
                m_typeDialogMap[MessageDisplayData.Type.Success] = null;
            }

            if(warningDialog != null)
            {
                warningDialog.gameObject.SetActive(false);
                m_typeDialogMap[MessageDisplayData.Type.Warning] = warningDialog;
            }
            else
            {
                m_typeDialogMap[MessageDisplayData.Type.Warning] = null;
            }

            if(errorDialog != null)
            {
                errorDialog.gameObject.SetActive(false);
                m_typeDialogMap[MessageDisplayData.Type.Error] = errorDialog;
            }
            else
            {
                m_typeDialogMap[MessageDisplayData.Type.Error] = null;
            }

            queuedMessages = new List<MessageDisplayData>();
            m_displayRoutine = this.StartCoroutine(DisplayRoutine());
        }

        private void OnDisable()
        {
            this.StopCoroutine(m_displayRoutine);
            m_displayRoutine = null;

            if(_instance == this)
            {
                _instance = null;
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public static void QueueMessage(MessageDisplayData.Type messageType,
                                        string messageContent,
                                        float displayDuration = 0f)
        {
            Debug.Assert(!System.String.IsNullOrEmpty(messageContent));

            // early out
            if(instance == null) { return; }

            // check for default duration
            if(displayDuration <= 0f)
            {
                displayDuration = (instance.defaultBaseTime
                                   + messageContent.Length * instance.defaultCharacterTime);
            }

            // queue message
            MessageDisplayData newMessage = new MessageDisplayData()
            {
                type = messageType,
                content = messageContent,
                displayDuration = displayDuration,
            };

            instance.queuedMessages.Add(newMessage);
        }

        private System.Collections.IEnumerator DisplayRoutine()
        {
            while(true)
            {
                while(queuedMessages.Count == 0)
                {
                    yield return null;
                }

                MessageDisplayData message = queuedMessages[0];
                MessageDisplay dialog = m_typeDialogMap[message.type];

                if(dialog != null)
                {
                    dialog.content.text = message.content;
                    dialog.gameObject.SetActive(true);

                    yield return new WaitForSeconds(message.displayDuration);

                    dialog.gameObject.SetActive(false);
                }

                queuedMessages.Remove(message);
            }
        }
    }
}
