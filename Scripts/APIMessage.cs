using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    [System.Serializable]
    public class APIMessage
    {
        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private int _responseCode;
        [SerializeField] private string _content;

        // ---------[ FIELDS ]---------
        public int responseCode { get { return this._responseCode; } }
        public string content   { get { return this._content; } }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyMessageObjectValues(API.MessageObject apiObject)
        {
            this._responseCode = apiObject.code;
            this._content = apiObject.message;
        }

        public static APIMessage CreateFromMessageObject(API.MessageObject apiObject)
        {
            var retVal = new APIMessage();
            retVal.ApplyMessageObjectValues(apiObject);
            return retVal;
        }
    }
}
