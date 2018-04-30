using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    public interface IImageLocator
    {
        string fileName  { get; }
        string url       { get; }
    }

    [Serializable]
    public class SingleVersionImageLocator : IImageLocator
    {
        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] protected string _fileName;
        [SerializeField] protected string _url;

        // ---------[ FIELDS ]---------
        public string fileName  { get { return this._fileName; } }
        public string url       { get { return this._url; } }
    }

    [Serializable]
    public abstract class MultiVersionImageLocator : IImageLocator
    {
        // ---------[ INNER CLASSES ]---------
        [Serializable]
        protected class VersionSourcePair
        {
            public int versionId;
            public string url;
        }

        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] protected string _fileName;
        [SerializeField] protected VersionSourcePair[] _versionPairing;

        // ---------[ FIELDS ]---------
        public string fileName  { get { return this._fileName; } }
        public string url       { get { return this.GetVersionURL(this.FullSizeVersion()); } }

        // ---------[ ACCESSORS ]---------
        protected abstract int FullSizeVersion();

        public string GetVersionURL(int versionId)
        {
            if(this._versionPairing != null)
            {
                foreach(VersionSourcePair pair in this._versionPairing)
                {
                    if(pair.versionId == versionId) { return pair.url; }
                }
            }
            return null;
        }
    }

    public abstract class MultiVersionImageLocator<E> : MultiVersionImageLocator
        where E : struct, IConvertible
    {
        public string GetVersionURL(E version)
        {
            int versionId = version.ToInt32(null);
            return this.GetVersionURL(versionId);
        }
    }
}