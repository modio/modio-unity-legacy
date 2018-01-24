using System;

namespace ModIO
{
    [Serializable]
    public class GameTagOption : IEquatable<GameTagOption>
    {
        // - Enum -
        public enum TagType
        {
            SingleValue,
            MultiValue
        }

        // - Constructors - 
        public static GameTagOption GenerateFromAPIObject(API.GameTagOptionObject apiObject)
        {
            GameTagOption newGameTagOption = new GameTagOption();
            newGameTagOption._data = apiObject;

            // - Parse Fields -
            switch(apiObject.type.ToUpper())
            {
                case "CHECKBOXES":
                {
                    newGameTagOption.tagType = TagType.SingleValue;
                }
                break;
                case "MULTIVALUE":
                {
                    newGameTagOption.tagType = TagType.MultiValue;
                }
                break;
                default:
                {
                    UnityEngine.Debug.LogWarning("Unrecognised tag type: " + newGameTagOption.ToString());
                }
                break;
            }
         
            newGameTagOption.isHidden = (apiObject.hidden > 0);


            return newGameTagOption;
        }

        public static GameTagOption[] GenerateFromAPIObjectArray(API.GameTagOptionObject[] apiObjectArray)
        {
            GameTagOption[] objectArray = new GameTagOption[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = GameTagOption.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.GameTagOptionObject _data;

        public string name      { get { return _data.name; } }
        public TagType tagType  { get; private set; }
        public bool isHidden    { get; private set; }
        public string[] tags    { get { return _data.tags; } }

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
