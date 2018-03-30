using System.Collections.Generic;
using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    [System.Serializable]
    public class ModTagCategory
    {
        private const string APIOBJECT_TYPESTRING_CATEGORYISFLAG = "checkboxes";
        private const string APIOBJECT_TYPESTRING_CATEGORYISNOTFLAG = "dropdown";

        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private string _name;
        [SerializeField] private bool _isFlag;
        [SerializeField] private bool _isHidden;
        [SerializeField] private string[] _tags;

        // ---------[ FIELDS ]---------
        public string name              { get { return this._name; } }
        public bool isFlag              { get { return this._isFlag; } }
        public bool isHidden            { get { return this._isHidden; } }
        public ICollection<string> tags { get { return new List<string>(this._tags); } }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyAPIObjectValues(API.GameTagOptionObject apiObject)
        {
            this._name = apiObject.name;
            this._isFlag = ModTagCategory.ParseAPITypeStringAsIsFlag(apiObject.type);
            this._isHidden = apiObject.hidden;
            this._tags = new string[apiObject.tags.Length];
            System.Array.Copy(apiObject.tags, this._tags, apiObject.tags.Length);
        }

        public static ModTagCategory CreateFromAPIObject(API.GameTagOptionObject apiObject)
        {
            var retVal = new ModTagCategory();
            retVal.ApplyAPIObjectValues(apiObject);
            return retVal;
        }

        public static bool ParseAPITypeStringAsIsFlag(string apiTypeString)
        {
            return apiTypeString.ToLower().Equals(APIOBJECT_TYPESTRING_CATEGORYISFLAG);
        }

        public static string IsFlagToAPIString(bool isFlag)
        {
            return (isFlag
                    ? APIOBJECT_TYPESTRING_CATEGORYISFLAG
                    : APIOBJECT_TYPESTRING_CATEGORYISNOTFLAG);
        }
    }
}
