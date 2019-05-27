using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public static class DownloadClient
    {
        // ---------[ IMAGE DOWNLOADS ]---------
        public static ImageRequest DownloadModLogo(ModProfile profile, LogoSize size)
        {
            Debug.Assert(profile != null, "[mod.io] Profile parameter cannot be null");

            return DownloadImage(profile.logoLocator.GetSizeURL(size));
        }

        // TODO(@jackson): Take ModMediaCollection instead of profile
        public static ImageRequest DownloadModGalleryImage(ModProfile profile,
                                                           string imageFileName,
                                                           ModGalleryImageSize size)
        {
            Debug.Assert(profile != null, "[mod.io] Profile parameter cannot be null");
            Debug.Assert(!String.IsNullOrEmpty(imageFileName),
                         "[mod.io] imageFileName parameter needs to be not null or empty (used as identifier for gallery images)");

            ImageRequest request = null;

            if(profile.media == null)
            {
                Debug.LogWarning("[mod.io] The given mod profile has no media information");
            }
            else
            {
                GalleryImageLocator locator = profile.media.GetGalleryImageWithFileName(imageFileName);
                if(locator == null)
                {
                    Debug.LogWarning("[mod.io] Unable to find mod gallery image with the file name \'"
                                     + imageFileName + "\' for the mod profile \'" + profile.name +
                                     "\'[" + profile.id + "]");
                }
                else
                {
                    request = DownloadClient.DownloadModGalleryImage(locator, size);
                }
            }

            return request;
        }

        public static ImageRequest DownloadModGalleryImage(GalleryImageLocator imageLocator,
                                                           ModGalleryImageSize size)
        {
            Debug.Assert(imageLocator != null, "[mod.io] imageLocator parameter cannot be null.");
            Debug.Assert(!String.IsNullOrEmpty(imageLocator.fileName), "[mod.io] imageFileName parameter needs to be not null or empty (used as identifier for gallery images)");

            ImageRequest request = null;
            request = DownloadImage(imageLocator.GetSizeURL(size));
            return request;
        }

        public static ImageRequest DownloadUserAvatar(UserProfile profile,
                                                      UserAvatarSize size)
        {
            Debug.Assert(profile != null, "[mod.io] Profile parameter cannot be null");

            ImageRequest request = null;

            if(profile.avatarLocator == null
               || String.IsNullOrEmpty(profile.avatarLocator.GetSizeURL(size)))
            {
                Debug.LogWarning("[mod.io] User Profile has no associated avatar information");
            }
            else
            {
                request = DownloadImage(profile.avatarLocator.GetSizeURL(size));
            }

            return request;
        }

        public static ImageRequest DownloadYouTubeThumbnail(string youTubeId)
        {
            Debug.Assert(!String.IsNullOrEmpty(youTubeId),
                         "[mod.io] YouTube video identifier cannot be empty");

            ImageRequest request = null;

            string thumbnailURL = (@"https://img.youtube.com/vi/"
                                   + youTubeId
                                   + @"/hqdefault.jpg");

            request = DownloadImage(thumbnailURL);

            return request;
        }

        public static ImageRequest DownloadImage(string imageURL)
        {
            ImageRequest request = new ImageRequest();
            request.isDone = false;

            UnityWebRequest webRequest = UnityWebRequest.Get(imageURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            #if DEBUG
            if(PluginSettings.data.logAllRequests)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(APIClient.UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(APIClient.MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        requestHeaders += "\n" + headerKey + ": " + headerValue;
                    }
                }

                Debug.Log("GENERATING DOWNLOAD REQUEST"
                          + "\nURL: " + webRequest.url
                          + "\nHeaders: " + requestHeaders
                          + "\n"
                          );
            }
            #endif

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(operation, request);

            return request;
        }

        private static void OnImageDownloadCompleted(UnityWebRequestAsyncOperation operation,
                                                     ImageRequest request)
        {
            UnityWebRequest webRequest = operation.webRequest;
            request.isDone = true;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                request.error = WebRequestError.GenerateFromWebRequest(webRequest);
                request.NotifyFailed();
            }
            else
            {
                #if DEBUG
                if(PluginSettings.data.logAllRequests)
                {
                    var responseTimeStamp = ServerTimeStamp.Now;
                    Debug.Log(String.Format("{0} REQUEST SUCEEDED\nResponse received at: {1} [{2}]\nURL: {3}\nResponse: {4}\n",
                                            webRequest.method.ToUpper(),
                                            ServerTimeStamp.ToLocalDateTime(responseTimeStamp),
                                            responseTimeStamp,
                                            webRequest.url,
                                            webRequest.downloadHandler.text));
                }
                #endif

                request.imageTexture = (webRequest.downloadHandler as DownloadHandlerTexture).texture;
                request.NotifySucceeded();
            }
        }

        // ---------[ BINARY DOWNLOADS ]---------
        public static event Action<ModfileIdPair, FileDownloadInfo> modfileDownloadSucceeded;
        public static event Action<ModfileIdPair, WebRequestError> modfileDownloadFailed;
        public static Dictionary<ModfileIdPair, FileDownloadInfo> modfileDownloadMap = new Dictionary<ModfileIdPair, FileDownloadInfo>();

        public static FileDownloadInfo GetActiveModBinaryDownload(int modId, int modfileId)
        {
            ModfileIdPair idPair = new ModfileIdPair()
            {
                modId = modId,
                modfileId = modfileId,
            };

            FileDownloadInfo downloadInfo;
            if(DownloadClient.modfileDownloadMap.TryGetValue(idPair, out downloadInfo))
            {
                return downloadInfo;
            }

            return null;
        }

        public static FileDownloadInfo StartModBinaryDownload(int modId, int modfileId,
                                                              string targetFilePath)
        {
            ModfileIdPair idPair = new ModfileIdPair()
            {
                modId = modId,
                modfileId = modfileId,
            };

            if(modfileDownloadMap.Keys.Contains(idPair))
            {
                Debug.LogWarning("[mod.io] Mod Binary with matching ids already downloading. TargetFilePath was not updated.");
            }
            else
            {
                modfileDownloadMap[idPair] = new FileDownloadInfo()
                {
                    target = targetFilePath,
                    fileSize = -1,
                    request = null,
                    isDone = false,
                };

                // - Acquire Download URL -
                APIClient.GetModfile(modId, modfileId,
                                     (mf) =>
                                     {
                                        // NOTE(@jackson): May have been cancelled
                                        FileDownloadInfo downloadInfo = GetActiveModBinaryDownload(modId, modfileId);
                                        if(downloadInfo != null)
                                        {
                                            modfileDownloadMap[idPair].fileSize = mf.fileSize;
                                            DownloadModBinary_Internal(idPair, mf.downloadLocator.binaryURL);
                                        }
                                     },
                                     (e) => { if(modfileDownloadFailed != null) { modfileDownloadFailed(idPair, e); } });
            }
            return modfileDownloadMap[idPair];
        }

        public static FileDownloadInfo StartModBinaryDownload(Modfile modfile, string targetFilePath)
        {
            Debug.Assert(modfile.downloadLocator.dateExpires > ServerTimeStamp.Now);

            ModfileIdPair idPair = new ModfileIdPair()
            {
                modId = modfile.modId,
                modfileId = modfile.id,
            };

            if(modfileDownloadMap.Keys.Contains(idPair))
            {
                Debug.LogWarning("[mod.io] Mod Binary for modfile is already downloading. TargetFilePath was not updated.");
            }
            else
            {
                modfileDownloadMap[idPair] = new FileDownloadInfo()
                {
                    target = targetFilePath,
                    fileSize = modfile.fileSize,
                    request = null,
                    isDone = false,
                };

                DownloadModBinary_Internal(idPair, modfile.downloadLocator.binaryURL);
            }

            return modfileDownloadMap[idPair];
        }

        private static void DownloadModBinary_Internal(ModfileIdPair idPair, string downloadURL)
        {
            FileDownloadInfo downloadInfo = modfileDownloadMap[idPair];
            downloadInfo.request = UnityWebRequest.Get(downloadURL);

            string tempFilePath = downloadInfo.target + ".download";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));
                downloadInfo.request.downloadHandler = new DownloadHandlerFile(tempFilePath);

                #if PLATFORM_PS4
                // NOTE(@jackson): This workaround addresses an issue in UnityWebRequests on the
                //  PS4 whereby redirects fail in specific cases. Special thanks to @Eamon of
                //  Spiderling Studios (http://spiderlinggames.co.uk/)
                downloadInfo.request.redirectLimit = 0;
                #endif
            }
            catch(Exception e)
            {
                string warningInfo = ("Failed to create download file on disk."
                                      + "\nFile: " + tempFilePath + "\n\n");

                Debug.LogWarning("[mod.io] " + warningInfo + Utility.GenerateExceptionDebugString(e));

                if(modfileDownloadFailed != null)
                {
                    modfileDownloadFailed(idPair, WebRequestError.GenerateLocal(warningInfo));
                }

                return;
            }

            var operation = downloadInfo.request.SendWebRequest();

            #if DEBUG
            if(PluginSettings.data.logAllRequests)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(APIClient.UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(APIClient.MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = downloadInfo.request.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        requestHeaders += "\n" + headerKey + ": " + headerValue;
                    }
                }

                int timeStamp = ServerTimeStamp.Now;
                Debug.Log("DOWNLOAD REQUEST SENT"
                          + "\nTimeStamp: [" + timeStamp.ToString() + "] "
                          + ServerTimeStamp.ToLocalDateTime(timeStamp).ToString()
                          + "\nURL: " + downloadInfo.request.url
                          + "\nHeaders: " + requestHeaders);
            }
            #endif


            operation.completed += (o) => DownloadClient.OnModBinaryRequestCompleted(idPair);
        }


        public static void CancelModBinaryDownload(int modId, int modfileId)
        {
            ModfileIdPair idPair = new ModfileIdPair()
            {
                modId = modId,
                modfileId = modfileId,
            };

            CancelModfileDownload_Internal(idPair);
        }

        public static void CancelAnyModBinaryDownloads(int modId)
        {
            List<ModfileIdPair> downloadsToCancel = new List<ModfileIdPair>();

            foreach(var kvp in modfileDownloadMap)
            {
                if(kvp.Key.modId == modId)
                {
                    downloadsToCancel.Add(kvp.Key);
                }
            }

            foreach(var idPair in downloadsToCancel)
            {
                CancelModfileDownload_Internal(idPair);
            }
        }

        private static void CancelModfileDownload_Internal(ModfileIdPair idPair)
        {
            FileDownloadInfo downloadInfo = null;
            if(modfileDownloadMap.TryGetValue(idPair, out downloadInfo))
            {
                if(downloadInfo.request != null)
                {
                    downloadInfo.request.Abort();
                }
                else
                {
                    downloadInfo.isDone = true;
                    downloadInfo.wasAborted = true;

                    modfileDownloadMap.Remove(idPair);
                }
            }
        }

        private static void OnModBinaryRequestCompleted(ModfileIdPair idPair)
        {
            FileDownloadInfo downloadInfo = DownloadClient.modfileDownloadMap[idPair];
            UnityWebRequest request = downloadInfo.request;
            bool succeeded = false;
            downloadInfo.isDone = true;

            if(request.isNetworkError || request.isHttpError)
            {
                if(request.error.ToUpper() == "USER ABORTED"
                   || request.error.ToUpper() == "REQUEST ABORTED")
                {
                    #if DEBUG
                    if(PluginSettings.data.logAllRequests)
                    {
                        Debug.Log("DOWNLOAD ABORTED"
                                  + "\nDownload aborted at: " + ServerTimeStamp.Now
                                  + "\nURL: " + request.url);
                    }
                    #endif

                    downloadInfo.wasAborted = true;
                }

                // NOTE(@jackson): This workaround addresses an issue in UnityWebRequests on the
                //  PS4 whereby redirects fail in specific cases. Special thanks to @Eamon of
                //  Spiderling Studios (http://spiderlinggames.co.uk/)
                #if PLATFORM_PS4
                else if (downloadInfo.error.responseCode == 302) // Redirect limit exceeded
                {
                    string headerLocation = string.Empty;
                    if (downloadInfo.error.responseHeaders.TryGetValue("location", out headerLocation)
                        && !request.url.Equals(headerLocation))
                    {
                        if (PluginSettings.data.logAllRequests)
                        {
                            Debug.LogFormat("CAUGHT DOWNLOAD REDIRECTION\nURL: {0}", headerLocation);
                        }

                        downloadInfo.error = null;
                        downloadInfo.isDone = false;
                        DownloadModBinary_Internal(idPair, headerLocation);
                        return;
                    }
                }
                #endif

                else
                {
                    downloadInfo.error = WebRequestError.GenerateFromWebRequest(request);

                    if(PluginSettings.data.logAllRequests)
                    {
                        WebRequestError.LogAsWarning(downloadInfo.error);
                    }

                    if(modfileDownloadFailed != null)
                    {
                        modfileDownloadFailed(idPair, downloadInfo.error);
                    }
                }
            }
            else
            {
                try
                {
                    if(File.Exists(downloadInfo.target))
                    {
                        File.Delete(downloadInfo.target);
                    }

                    File.Move(downloadInfo.target + ".download", downloadInfo.target);

                    succeeded = true;
                }
                catch(Exception e)
                {
                    string warningInfo = ("Failed to save mod binary."
                                          + "\nFile: " + downloadInfo.target + "\n\n");

                    Debug.LogWarning("[mod.io] " + warningInfo + Utility.GenerateExceptionDebugString(e));

                    downloadInfo.error = WebRequestError.GenerateLocal(warningInfo);

                    if(modfileDownloadFailed != null)
                    {
                        modfileDownloadFailed(idPair, downloadInfo.error);
                    }
                }
            }

            if(succeeded)
            {
                #if DEBUG
                if(PluginSettings.data.logAllRequests)
                {
                    var responseTimeStamp = ServerTimeStamp.Now;
                    Debug.Log("DOWNLOAD SUCEEDED"
                              + "\nDownload completed at: " + ServerTimeStamp.ToLocalDateTime(responseTimeStamp)
                              + "\nURL: " + request.url
                              + "\nFilePath: " + downloadInfo.target);
                }
                #endif

                if(modfileDownloadSucceeded != null)
                {
                    modfileDownloadSucceeded(idPair, downloadInfo);
                }
            }

            modfileDownloadMap.Remove(idPair);
        }

        // ---------[ OBSOLETE ]---------
        /// <summary>Enable logging of all web requests.</summary>
        [Obsolete("Use PluginSettings.data.logAllRequests instead.")]
        public static bool logAllRequests
        {
            get { return PluginSettings.data.logAllRequests; }
        }
    }
}
