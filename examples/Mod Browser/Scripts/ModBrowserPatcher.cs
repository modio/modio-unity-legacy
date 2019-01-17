using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

using ModIO.UI;

namespace ModIO
{
    public static class ModBrowserPatcher
    {
        public static readonly ModBrowserVersion Jan16_2019 = new ModBrowserVersion(0, 9);
        public static void Patch_2019_Jan16()
        {
            Debug.Log("[mod.io] Applying Jan16_2019 Patch");

            // Clear downloads
            string installDirectory = IOUtilities.CombinePath(CacheClient.settings.directory, "_installedMods");
            IOUtilities.DeleteDirectory(installDirectory);

            // ensure all subscribed mods are otherwise up-to-date
            Action<ModProfile> assertBinaryIsDownloaded = (p) =>
            {
                int modId = p.id;
                int modfileId = p.activeBuild.id;

                Action unzip = () =>
                {
                    string unzipLocation = IOUtilities.CombinePath(CacheClient.settings.directory,
                                                                   "_installedMods",
                                                                   modId.ToString() + "/");

                    IOUtilities.DeleteDirectory(unzipLocation);

                    ModManager.UnzipModBinaryToLocation(modId, modfileId,
                                                        unzipLocation);
                };

                if(ModManager.IsBinaryDownloaded(modId, modfileId))
                {
                    unzip();
                }
                else
                {
                    FileDownloadInfo downloadInfo = DownloadClient.GetActiveModBinaryDownload(modId, modfileId);

                    if(downloadInfo == null)
                    {
                        string zipFilePath = CacheClient.GenerateModBinaryZipFilePath(modId, modfileId);
                        DownloadClient.StartModBinaryDownload(modId, modfileId, zipFilePath);

                        downloadInfo = DownloadClient.GetActiveModBinaryDownload(modId, modfileId);
                    }

                    if(!downloadInfo.isDone)
                    {
                        ModView[] sceneViews = Resources.FindObjectsOfTypeAll<ModView>();
                        foreach(ModView modView in sceneViews)
                        {
                            if(modView.data.profile.modId == modId)
                            {
                                modView.DisplayDownload(downloadInfo);
                            }
                        }

                        // unzip is attached to DownloadClient in ModBrowser.Start
                    }
                    else
                    {
                        unzip();
                    }
                }
            };

            var subscribedModIds = ModManager.GetSubscribedModIds();
            foreach(int modId in subscribedModIds)
            {
                ModManager.GetModProfile(modId,
                                         assertBinaryIsDownloaded,
                                         WebRequestError.LogAsWarning);
            }
        }

        public static void Run(ModBrowserVersion currentVersion)
        {
            if(currentVersion < Jan16_2019)
            {
                Patch_2019_Jan16();
            }
        }
    }
}
