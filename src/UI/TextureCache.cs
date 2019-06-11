using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Manages caching of the textures required by the UI.</summary>
    public class TextureCache : MonoBehaviour
    {
        // ---------[ SINGLETON ]---------
        /// <summary>The singleton instance of the TextureCache.</summary>
        private static TextureCache _instance;
        /// <summary>The singleton instance of the TextureCache.</summary>
        public static TextureCache instance
        {
            get
            {
                if(TextureCache._instance == null)
                {
                    TextureCache._instance = UIUtilities.FindComponentInScene<TextureCache>(true);

                    if(TextureCache._instance == null)
                    {
                        GameObject instanceGO = new GameObject("mod.io UI Texture Cache");
                        TextureCache._instance = instanceGO.AddComponent<TextureCache>();
                    }
                }

                return TextureCache._instance;
            }
        }

        // ---------[ FIELDS ]---------
        /// <summary>Should the cache be cleared on disable</summary>
        public bool clearCacheOnDisable = true;

        /// <summary>The texture cache holding all versions of cached images.</summary>
        public Dictionary<ImageDisplayData, Texture2D[]> cache = new Dictionary<ImageDisplayData, Texture2D[]>();

        // ---------[ INITIALIZATION ]---------
        protected virtual void OnDisable()
        {
            if(this.clearCacheOnDisable)
            {
                this.cache.Clear();
            }
        }
    }
}
