using System;
using System.IO;
using System.Collections.Generic;

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
                    request = DownloadImage(locator.GetSizeURL(size));
                }
            }

            return request;
        }

        public static ImageRequest DownloadUserAvatar(UserProfileStub profile,
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
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
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

            UnityWebRequest webRequest = UnityWebRequest.Get(modfile.downloadLocator.binaryURL);

            request.modfile = modfile;
            request.webRequest = webRequest;

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


            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
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
