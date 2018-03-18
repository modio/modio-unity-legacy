using System;
using System.Collections.Generic;

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

        public static string GetTagTypeString(TagType tagType)
        {
            switch(tagType)
            {
                case TagType.SingleValue:
                {
                    return "checkboxes";
                }
                case TagType.MultiValue:
                {
                    return "dropdown";
                }
                default:
                {
                    return null;
                }
            }
        }

        public static bool TryParseStringAsTagType(string value, out TagType tagType)
        {
            switch(value)
            {
                case "checkboxes":
                {
                    tagType = TagType.SingleValue;
                    return true;
                }
                case "dropdown":
                {
                    tagType = TagType.MultiValue;
                    return true;
                }
            }

            tagType = TagType.SingleValue;
            return false;
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

            TagType tag;
            TryParseStringAsTagType(apiObject.type, out tag);
            this.tagType = tag;
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

    public class UnsubmittedGameTagOption
    {
        // --- FIELDS ---
        [UnityEngine.SerializeField]
        private API.CreatedGameTagOptionObject _data;

        // [Required] Name of the tag group, for example you may want to have 'Difficulty' as the name with 'Easy', 'Medium' and 'Hard' as the tag values.
        public string name
        {
            get { return _data.name; }
            set { _data.name = value; }
        }
        // [Required] Determines whether you allow users to only select one tag (dropdown) or multiple tags (checkbox):
        public GameTagOption.TagType tagType
        {
            get
            {
                GameTagOption.TagType retVal;
                GameTagOption.TryParseStringAsTagType(_data.type, out retVal);
                return retVal;
            }
            set { _data.type = GameTagOption.GetTagTypeString(value); }
        }
        // This group of tags should be hidden from users and mod developers. Useful for games to tag special functionality, to filter on and use behind the scenes. You can also use Metadata Key Value Pairs for more arbitary data.
        public bool isHidden
        {
            get { return _data.hidden; }
            set { _data.hidden = value; }
        }
        // [Required] Array of tags mod creators can choose to apply to their profiles.
        public string[] tagNames
        {
            get { return _data.tags; }
            set { _data.tags = value; }
        }

        // --- ACCESSORS ---
        public API.StringValueParameter[] GetValueFields()
        {
            List<API.StringValueParameter> retVal = new List<API.StringValueParameter>();

            retVal.Add(API.StringValueParameter.Create("name", name));
            retVal.Add(API.StringValueParameter.Create("type", _data.type));
            retVal.Add(API.StringValueParameter.Create("hidden", (isHidden ? "1" : "0")));

            foreach(string tagName in tagNames)
            {
                retVal.Add(API.StringValueParameter.Create("tags[]", tagName));
            }

            return retVal.ToArray();
        }

        public API.AddGameTagOptionParameters AsAddGameTagOptionParameters()
        {
            var parameters = new API.AddGameTagOptionParameters();
            parameters.stringValues = new List<API.StringValueParameter>(this.GetValueFields());
            return parameters;
        }
    }

    public class GameTagOptionToDelete
    {
        // --- FIELDS ---
        public string tagGroup = ""; // [Required] Name of the tag group that you want to delete tags from.
        public string[] tags = new string[0]; // [Required] Array of strings representing the tag options to delete. An empty array will delete the entire group.

        public API.StringValueParameter[] GetValueFields()
        {
            List<API.StringValueParameter> retVal = new List<API.StringValueParameter>(1 + tags.Length);

            retVal.Add(API.StringValueParameter.Create("name", tagGroup));
            foreach(string tag in tags)
            {
                retVal.Add(API.StringValueParameter.Create("tags[]", tag));
            }

            return retVal.ToArray();
        }

        public API.DeleteGameTagOptionParameters AsDeleteGameTagOptionParameters()
        {
            var parameters = new API.DeleteGameTagOptionParameters();
            parameters.name = tagGroup;
            parameters.tags = tags;
            return parameters;
        }
    }
}
