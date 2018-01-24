using System;

namespace ModIO
{
    [Serializable]
    public class GameTagOption : IEquatable<GameTagOption>, IAPIObjectWrapper<API.GameTagOptionObject>, UnityEngine.ISerializationCallbackReceiver
    {
        // - Enum -
        public enum TagType
        {
            SingleValue,
            MultiValue
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.GameTagOptionObject _data;

        public string name      { get { return _data.name; } }
        public TagType tagType  { get; private set; }
        public bool isHidden    { get; private set; }
        public string[] tags    { get { return _data.tags; } }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.GameTagOptionObject apiObject)
        {
            this._data = apiObject;

            // - Parse Fields -
            switch(apiObject.type.ToUpper())
            {
                case "CHECKBOXES":
                {
                    this.tagType = TagType.SingleValue;
                }
                break;
                case "MULTIVALUE":
                {
                    this.tagType = TagType.MultiValue;
                }
                break;
                default:
                {
                    UnityEngine.Debug.LogWarning("Unrecognised tag type: " + apiObject.type.ToString());
                }
                break;
            }
         
            this.isHidden = (apiObject.hidden > 0);
        }

        public API.GameTagOptionObject GetAPIObject()
        {
            return this._data;
        }

        // - ISerializationCallbackReceiver -
        public void OnBeforeSerialize() {}
        public void OnAfterDeserialize()
        {
            this.WrapAPIObject(this._data);
        }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as GameTagOption);
        }

        public bool Equals(GameTagOption other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
