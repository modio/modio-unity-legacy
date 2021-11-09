namespace ModIO.API
{
    public class EditGameParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // Status of a game. We recommend you never change this once you have accepted your game to
        // be available via the API (see status and visibility for details).
        public GameStatus status
        {
            set {
                this.SetStringValue("status", (int)value);
            }
        }

        // Name of your game. Cannot exceed 80 characters.
        public string name
        {
            set {
                this.SetStringValue("name", value);
            }
        }

        // Subdomain for the game on mod.io. Highly recommended to not change this unless absolutely
        // required. Cannot exceed 20 characters.
        public string nameId
        {
            set {
                this.SetStringValue("name_id", value);
            }
        }

        // Explain your games mod support in 1 paragraph. Cannot exceed 250 characters.
        public string summary
        {
            set {
                this.SetStringValue("summary", value);
            }
        }

        // Instructions and links creators should follow to upload mods. Keep it short and explain
        // details like are mods submitted in-game or via tools you have created.
        public string instructions
        {
            set {
                this.SetStringValue("instructions", value);
            }
        }

        // Link to a mod.io guide, your modding wiki or a page where modders can learn how to make
        // and submit mods to your games profile.
        public string instructionsURL
        {
            set {
                this.SetStringValue("instructions_url", value);
            }
        }

        // Word used to describe user-generated content (mods, items, addons etc).
        public string ugcName
        {
            set {
                this.SetStringValue("ugc_name", value);
            }
        }

        // Choose the presentation style you want on the mod.io website:
        public GameModGalleryPresentation modGalleryPresentation
        {
            set {
                this.SetStringValue("presentation_option", (int)value);
            }
        }
        // Choose the submission process you want modders to follow:
        public GameModSubmissionPermission modSubmissionPermission
        {
            set {
                this.SetStringValue("submission_option", (int)value);
            }
        }
        // Choose the curation process your team follows to approve mods:
        public GameModCuration modCuration
        {
            set {
                this.SetStringValue("curation_option", (int)value);
            }
        }
        // Choose the community features enabled on the mod.io website:
        public GameCommunityFeatures communityFeatures
        {
            set {
                this.SetStringValue("community_options", (int)value);
            }
        }
        // Choose the revenue capabilities mods can enable:
        public GameModRevenuePermissions modRevenuePermissions
        {
            set {
                this.SetStringValue("revenue_options", (int)value);
            }
        }
        // Choose the level of API access your game allows:
        public GameAPIPermissions apiPermissions
        {
            set {
                this.SetStringValue("api_access_options", (int)value);
            }
        }
        // Choose if you want to allow developers to select if they can flag their mods as
        // containing mature content
        public GameModContentPermission contentPermission
        {
            set {
                this.SetStringValue("maturity_options", (int)value);
            }
        }
    }
}
