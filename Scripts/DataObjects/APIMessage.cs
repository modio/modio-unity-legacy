using System;

namespace ModIO
{
    [Serializable]
    public class APIMessage : IAPIObjectWrapper<API.MessageObject>
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.MessageObject _data;

        public int httpStatusCode   { get { return _data.code; } }
        public string content       { get { return _data.message; } }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.MessageObject apiObject)
        {
            this._data = apiObject;
        }

        public API.MessageObject GetAPIObject()
        {
            return this._data;
        }
    }   
}
