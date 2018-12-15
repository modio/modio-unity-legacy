using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
	public class ModView : MonoBehaviour
	{
		// ---------[ FIELDS ]---------
        public event Action<ModView> onClick;

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

        public void DisplayProfile(ModProfile profile,
                                   IEnumerable<ModTagCategory> tagCategories)
        {

        }

        public void DisplayStatistics(ModStatistics statistics)
        {

        }

        public void DisplayLoading()
        {

        }

        // ---------[ EVENTS ]---------
        public void NotifyClicked()
        {
            if(onClick != null)
            {
                onClick(this);
            }
        }
	}
}
