using System;

namespace ModIO
{
    [Serializable]
    public class APIMessage
    {
        // - Constructors - 
        public static APIMessage GenerateFromAPIObject(API.MessageObject apiObject)
        {
            APIMessage newAvatar = new APIMessage();
            newAvatar._data = apiObject;
            return newAvatar;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.MessageObject _data;

        public int httpStatusCode   { get { return _data.code; } }
        public string content       { get { return _data.message; } }
    }   
}
