namespace ModIO.API
{
    public class DeleteModDependenciesParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] Array containing one or more mod id's that can be deleted as dependencies.
        public int[] dependencies
        {
            set {
                this.SetStringArrayValue("dependencies[]", value);
            }
        }
    }
}
