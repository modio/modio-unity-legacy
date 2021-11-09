namespace ModIO.API
{
    public class AddModDependenciesParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] Array containing one or more mod id's that this mod is dependent on. Max of 5
        // dependencies per request.
        public int[] dependencies
        {
            set {
                this.SetStringArrayValue("dependencies[]", value);
            }
        }
    }
}
