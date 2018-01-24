using System;

namespace ModIO
{
    [Serializable]
    public class Filehash : IEquatable<Filehash>
    {
        // - Constructors - 
        public static Filehash GenerateFromAPIObject(API.FilehashObject apiObject)
        {
            Filehash newFilehash = new Filehash();
            newFilehash._data = apiObject;
            return newFilehash;
        }

        public static Filehash[] GenerateFromAPIObjectArray(API.FilehashObject[] apiObjectArray)
        {
            Filehash[] objectArray = new Filehash[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = Filehash.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.FilehashObject _data;

        public string md5 { get { return _data.md5; } }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Filehash);
        }

        public bool Equals(Filehash other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
