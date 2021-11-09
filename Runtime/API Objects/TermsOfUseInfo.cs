using System.Runtime.Serialization;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModIO
{
    /// <summary>Terms of Use Object returned by the API.</summary>
    [System.Serializable]
    public class TermsOfUseInfo
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Link data for further information.</summary>
        [System.Serializable]
        public struct LinkData
        {
            /// <summary>Text to display on a button for the link.</summary>
            [JsonProperty("text")]
            public string buttonText;

            /// <summary>URL to the full information.</summary>
            [JsonProperty("url")]
            public string URL;

            /// <summary>Whether our terms require the URL to be made available to a user.</summary>
            [JsonProperty("required")]
            public bool required;
        }

        // ---------[ FIELDS ]---------
        /// <summary>Terms of use the user needs to agree to in order to authenticate.</summary>
        [JsonProperty("plaintext")]
        public string terms;

        /// <summary>Terms of use the user needs to agree to in order to authenticate.
        /// (HTML)</summary>
        [JsonProperty("html")]
        public string terms_HTML;

        /// <summary>UI Text for the "agree" button.</summary>
        [JsonProperty("buttons.agree.text")]
        public string buttonText_agree;

        /// <summary>UI Text for the "disagree" button.</summary>
        [JsonProperty("buttons.disagree.text")]
        public string buttonText_disagree;

        /// <summary>Additional information URLs to be provided to the user.</summary>
        [JsonProperty("links")]
        public Dictionary<string, LinkData> links;

        // ---------[ API DESERIALIZATION ]---------
        private const string APIOBJECT_LINKKEY_WEBSITE = "website";
        private const string APIOBJECT_LINKKEY_TERMS = "terms";
        private const string APIOBJECT_LINKKEY_PRIVACY = "privacy";
        private const string APIOBJECT_LINKKEY_ACCOUNT = "manage";

        private struct ButtonInfo
        {
            public string text;
        }

        [JsonExtensionData]
        private IDictionary<string, JToken> m_extensionData;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if(this.m_extensionData != null && this.m_extensionData.Count > 0)
            {
                JToken infoToken;
                if(this.m_extensionData.TryGetValue("buttons", out infoToken))
                {
                    this.buttonText_agree = (string)infoToken["agree"]["text"];
                    this.buttonText_disagree = (string)infoToken["disagree"]["text"];
                }
            }
        }
    }
}
