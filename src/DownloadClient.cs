using System;
using System.IO;

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

            ImageRequest request = new ImageRequest();
            request.isDone = false;

            string logoURL = profile.logoLocator.GetSizeURL(size);

            UnityWebRequest webRequest = UnityWebRequest.Get(logoURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(operation, request);

            return request;
        }

        // TODO(@jackson): Take ModMediaCollection instead of profile
        public static ImageRequest DownloadModGalleryImage(ModProfile profile,
                                                           string imageFileName,
                                                           ModGalleryImageSize size)
        {
            Debug.Assert(profile != null, "[mod.io] Profile parameter cannot be null");
            Debug.Assert(profile.media != null, "[mod.io] The given profile has no media information");
            Debug.Assert(!String.IsNullOrEmpty(imageFileName),
                         "[mod.io] imageFileName parameter needs to be not null or empty (used as identifier for gallery images)");

            ImageRequest request = null;
            GalleryImageLocator locator = profile.media.GetGalleryImageWithFileName(imageFileName);

            if(locator != null)
            {
                string imageURL = locator.GetSizeURL(size);

                UnityWebRequest webRequest = UnityWebRequest.Get(imageURL);
                webRequest.downloadHandler = new DownloadHandlerTexture(true);

                request = new ImageRequest();

                var operation = webRequest.SendWebRequest();
                operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(operation, request);
            }
            else
            {
                Debug.LogWarning("[mod.io] Unable to find mod gallery image with the file name \'"
                                 + imageFileName + "\' for the mod profile \'" + profile.name +
                                 "\'[" + profile.id + "]");
            }

            return request;
        }

        public static ImageRequest DownloadUserAvatar(UserProfileStub profile,
                                                      UserAvatarSize size)
        {
            Debug.Assert(profile != null, "[mod.io] Profile parameter cannot be null");

            ImageRequest request = new ImageRequest();
            request.isDone = false;

            string avatarURL = profile.avatarLocator.GetSizeURL(size);

            UnityWebRequest webRequest = UnityWebRequest.Get(avatarURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(operation, request);

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

            UnityWebRequest webRequest = UnityWebRequest.Get(thumbnailURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            request = new ImageRequest();

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
                if(GlobalSettings.LOG_ALL_WEBREQUESTS)
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
        public static ModBinaryRequest DownloadModBinary(int modId, int modfileId,
                                                         string downloadFilePath)
        {
            ModBinaryRequest request = new ModBinaryRequest();

            request.isDone = false;
            request.binaryFilePath = downloadFilePath;

            // - Acquire Download URL -
            APIClient.GetModfile(modId, modfileId,
                                 (mf) => DownloadClient.OnGetModfile(mf, request),
                                 (e) => { request.error = e; request.NotifyFailed(); });

            return request;
        }

        private static void OnGetModfile(Modfile modfile, ModBinaryRequest request)
        {
            string tempFilePath = request.binaryFilePath + ".download";

            request.modfile = modfile;
            request.webRequest = UnityWebRequest.Get(modfile.downloadLocator.binaryURL);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));
                request.webRequest.downloadHandler = new DownloadHandlerFile(tempFilePath);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to create download file on disk."
                                      + "\nFile: " + tempFilePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                request.NotifyFailed();

                return;
            }

            var operation = request.webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnModBinaryRequestCompleted(operation,
                                                                                     request);
        }

        private static void OnModBinaryRequestCompleted(UnityWebRequestAsyncOperation operation,
                                                        ModBinaryRequest request)
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
                if(GlobalSettings.LOG_ALL_WEBREQUESTS)
                {
                    var responseTimeStamp = ServerTimeStamp.Now;
                    Debug.Log("DOWNLOAD SUCEEDED"
                              + "\nDownload completed at: " + ServerTimeStamp.ToLocalDateTime(responseTimeStamp)
                              + "\nURL: " + webRequest.url
                              + "\nFilePath: " + request.binaryFilePath);
                }
                #endif

                try
                {
                    if(File.Exists(request.binaryFilePath))
                    {
                        File.Delete(request.binaryFilePath);
                    }

                    File.Move(request.binaryFilePath + ".download", request.binaryFilePath);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to save mod binary."
                                          + "\nFile: " + request.binaryFilePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    request.NotifyFailed();
                }

                request.NotifySucceeded();
            }
        }
    }
}
