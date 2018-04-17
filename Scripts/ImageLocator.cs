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
    public abstract class MultiVersionImageLocator : IImageLocator
    {
        // ---------[ INNER CLASSES ]---------
        [Serializable]
        public class VersionSourcePair
        {
            public int versionId;
            public string source;
        }

        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] protected string _fileName;
        [SerializeField] protected VersionSourcePair[] _versionPairing;

        // ---------[ FIELDS ]---------
        public string fileName  { get { return this._fileName; } }
        public string source    { get { return this.GetVersionSource(this.FullSizeVersion()); } }

        // ---------[ ACCESSORS ]---------
        protected abstract int FullSizeVersion();

        public string GetVersionSource(int versionId)
        {
            if(this._versionPairing != null)
            {
                foreach(VersionSourcePair pair in this._versionPairing)
                {
                    if(pair.versionId == versionId) { return pair.source; }
                }
            }
            return null;
        }
    }

    public abstract class MultiVersionImageLocator<E> : MultiVersionImageLocator
        where E : struct, IConvertible
    {
        public string GetVersionSource(E version)
        {
            int versionId = version.ToInt32(null);
            return this.GetVersionSource(versionId);
        }
    }
}