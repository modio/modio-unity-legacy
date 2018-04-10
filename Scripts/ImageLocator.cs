using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    public interface IImageLocator
    {
        string fileName  { get; }
        string source    { get; }
    }

    [Serializable]
    public class SingleVersionImageLocator : IImageLocator
    {
        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] protected string _fileName;
        [SerializeField] protected string _source;

        // ---------[ FIELDS ]---------
        public string fileName  { get { return this._fileName; } }
        public string source    { get { return this._source; } }
    }

    [Serializable]
    public abstract class MultiVersionImageLocator<E> : IImageLocator where E : struct, IConvertible
    {
        // ---------[ INNER CLASSES ]---------
        [Serializable]
        protected class VersionSourcePair
        {
            public E version;
            public string source;
        }

        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] protected string _fileName;
        [SerializeField] protected VersionSourcePair[] _versionPairing;

        // ---------[ FIELDS ]---------
        public string fileName  { get { return this._fileName; } }
        public string source    { get { return this.GetVersionSource(this.FullSizeVersionEnum()); } }

        // ---------[ ACCESSORS ]---------
        protected abstract E FullSizeVersionEnum();
        
        public string GetVersionSource(E version)
        {
            if(this._versionPairing != null)
            {
                foreach(VersionSourcePair pair in this._versionPairing)
                {
                    if(pair.version.Equals(version)) { return pair.source; }
                }
            }
            return null;
        }
    }
}