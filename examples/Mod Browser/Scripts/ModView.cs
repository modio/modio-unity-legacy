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
            if(profileDisplay != null)
            {
                profileDisplay.Initialize();
            }
            if(logoDisplay != null)
            {
                logoDisplay.Initialize();
            }
            if(mediaContainer != null)
            {
                mediaContainer.Initialize();
            }
            if(creatorDisplay != null)
            {
                creatorDisplay.Initialize();
            }
            if(statisticsDisplay != null)
            {
                statisticsDisplay.Initialize();
            }
            if(tagsDisplay != null)
            {
                tagsDisplay.Initialize();
            }
            if(buildDisplay != null)
            {
                buildDisplay.Initialize();
            }
            if(downloadDisplay != null)
            {
                downloadDisplay.Initialize();
            }
        }

        // public void DisplayProfile(ModProfile profile,
        //                            IEnumerable<ModTagCategory> tagCategories)
        // {

        // }

        // public void DisplayStatistics(ModStatistics statistics)
        // {

        // }

        // public void DisplayLoading()
        // {

        // }

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
