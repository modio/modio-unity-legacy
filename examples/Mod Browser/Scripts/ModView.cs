using UnityEngine;

namespace ModIO.UI
{
	public class ModView : MonoBehaviour
	{
		// ---------[ FIELDS ]---------
        [Header("UI Components")]
        public ModProfileDisplayComponent           profileDisplay;
        public ModLogoDisplayComponent              logoDisplay;
        public ModMediaCollectionDisplayComponent   mediaContainer;
        public UserDisplayComponent                 creatorDisplay;
        public ModStatisticsDisplayComponent        statisticsDisplay;
        public ModTagCollectionDisplayComponent     tagsDisplay;
        public ModfileDisplayComponent              buildDisplay;
        public ModBinaryRequestDisplay              downloadDisplay;

        // ---[ ACCESSORS ]---
        public ModDisplayData data
        {
            get
            {
                return new ModDisplayData();
            }
            set
            {
                return;
            }
        }

        // ---------[ INITIALIZATION ]---------
        public void Initialize()
        {

        }
	}
}
