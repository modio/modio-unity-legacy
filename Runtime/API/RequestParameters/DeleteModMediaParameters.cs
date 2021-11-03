namespace ModIO.API
{
    public class DeleteModMediaParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // Filename's of the image(s) you want to delete - example 'gameplay2.jpg'.
        public string[] images
        {
            set {
                this.SetStringArrayValue("images[]", value);
            }
        }
        // Full Youtube link(s) you want to delete - example
        // 'https://www.youtube.com/watch?v=IGVZOLV9SPo'.
        public string[] youtube
        {
            set {
                this.SetStringArrayValue("youtube[]", value);
            }
        }
        // Full Sketchfab link(s) you want to delete - example
        // 'https://sketchfab.com/models/71f04e390ff54e5f8d9a51b4e1caab7e'.
        public string[] sketchfab
        {
            set {
                this.SetStringArrayValue("sketchfab[]", value);
            }
        }
    }
}
