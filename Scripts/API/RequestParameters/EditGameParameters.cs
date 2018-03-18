namespace ModIO.API
{
    public class EditGameParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // Status of a game. We recommend you never change this once you have accepted your game to be available via the API (see status and visibility for details):
        public int status
        {
            set
            {
                this.SetStringValue("status", value);
            }
        }
        // Name of your game. Cannot exceed 80 characters.
        public string name
        {
            set
            {
                this.SetStringValue("name", value);
            }
        }
        // Subdomain for the game on mod.io. Highly recommended to not change this unless absolutely required. Cannot exceed 20 characters.
        public string name_id
        {
            set
            {
                this.SetStringValue("name_id", value);
            }
        }
        // Explain your games mod support in 1 paragraph. Cannot exceed 250 characters.
        public string summary
        {
            set
            {
                this.SetStringValue("summary", value);
            }
        }
        // Instructions and links creators should follow to upload mods. Keep it short and explain details like are mods submitted in-game or via tools you have created.
        public string instructions
        {
            set
            {
                this.SetStringValue("instructions", value);
            }
        }
        // Official homepage for your game. Must be a valid URL.
        public string homepage
        {
            set
            {
                this.SetStringValue("homepage", value);
            }
        }
        // Word used to describe user-generated content (mods, items, addons etc).
        public string ugc_name
        {
            set
            {
                this.SetStringValue("ugc_name", value);
            }
        }
        // Choose the presentation style you want on the mod.io website:
        public int presentation_option
        {
            set
            {
                this.SetStringValue("presentation_opti", value);
            }
        }
        // Choose the submission process you want modders to follow:
        public int submission_option
        {
            set
            {
                this.SetStringValue("submission_option", value);
            }
        }
        // Choose the curation process your team follows to approve mods:
        public int curation_option
        {
            set
            {
                this.SetStringValue("curation_option", value);
            }
        }
        // Choose the community features enabled on the mod.io website:
        public int community_options
        {
            set
            {
                this.SetStringValue("community_options", value);
            }
        }
        // Choose the revenue capabilities mods can enable:
        public int revenue_options
        {
            set
            {
                this.SetStringValue("revenue_options", value);
            }
        }
        // Choose the level of API access your game allows:
        public int api_access_options
        {
            set
            {
                this.SetStringValue("api_access_option", value);
            }
        }
    }
}