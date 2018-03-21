using Debug = UnityEngine.Debug;

namespace ModIO
{
    public static class ModImageIdentifier
    {
        private const string LOGO_PREFIX = "MOD_LI:";
        private const string IMAGE_PREFIX = "MOD_GI:";
        private const int PREFIX_LENGTH = 7;
        private const int PART_COUNT = 2;

        public static string GenerateForLogo(int modId, string fileName)
        {
            Debug.Assert(modId > 0 && !System.String.IsNullOrEmpty(fileName));
            
            return (LOGO_PREFIX + modId.ToString()
                    + @"/" + fileName);
        }
        
        public static string GenerateForGalleryImage(int modId, string fileName)
        {
            Debug.Assert(modId > 0 && !System.String.IsNullOrEmpty(fileName));

            return (IMAGE_PREFIX + modId.ToString()
                    + @"/" + fileName);
        }

        public static bool TryParse(string identifier,
                                    out bool isLogo,
                                    out int modId,
                                    out string fileName)
        {
            Debug.Assert(!System.String.IsNullOrEmpty(identifier));

            isLogo = false;
            modId = 0;
            fileName = string.Empty;
            // version = ImageVersion.Original;

            if(identifier.Length <= PREFIX_LENGTH) { return false; }

            // - Get Prefix -
            string prefix = identifier.Substring(0, PREFIX_LENGTH);
            if(prefix.Equals(LOGO_PREFIX))
            {
                isLogo = true;
            }
            else if(prefix.Equals(IMAGE_PREFIX))
            {
                isLogo = false;
            }
            else
            {
                return false;
            }

            // - Split -
            string[] identifierParts = identifier.Substring(PREFIX_LENGTH).Split('/');
            if(identifierParts.Length != PART_COUNT) { return false; }

            // - Mod Id -
            if(!int.TryParse(identifierParts[0], out modId)) { return false; }

            // - FileName -
            if(identifierParts[1].Length > 0) { return false; }
            fileName = identifierParts[1];

            // int versionInt = -1;
            // if(!int.TryParse(identifierParts[2], out versionInt)) { return false; }
            // version = (ImageVersion)versionInt;

            return true;
        }
    }
}