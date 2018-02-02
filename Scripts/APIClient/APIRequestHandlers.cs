using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace ModIO
{
    internal class RequestHandler_Coroutine : APIClient.IRequestHandler
    {
        public UnityEngine.MonoBehaviour coroutineBehaviour;

        public void BeginRequest<T_APIObj>(UnityWebRequest webRequest,
                                           Action<T_APIObj> successCallback,
                                           Action<ErrorInfo> errorCallback)
        {
            coroutineBehaviour.StartCoroutine(ExecuteRequest<T_APIObj>(webRequest,
                                                           successCallback,
                                                           errorCallback));
        }

        // ---------[ REQUEST EXECUTION ]---------
        private System.Collections.IEnumerator ExecuteRequest<T_APIObj>(UnityWebRequest webRequest,
                                                                        Action<T_APIObj> successCallback,
                                                                        Action<ErrorInfo> errorCallback)
        {
            yield return webRequest.SendWebRequest();

            API.WebRequests.ProcessWebResponse(webRequest,
                                               successCallback,
                                               errorCallback);
        }
    }

    internal class RequestHandler_OnUpdate : APIClient.IRequestHandler
    {
        private List<ActiveAPIRequest> activeRequests = new List<ActiveAPIRequest>();

        private class ActiveAPIRequest
        {
            public UnityWebRequest webRequest;
            public Action processResponse;
        }

        public void BeginRequest<T_APIObj>(UnityWebRequest webRequest,
                                           Action<T_APIObj> successCallback,
                                           Action<ErrorInfo> errorCallback)
        {
            ActiveAPIRequest newRequest = new ActiveAPIRequest();
            newRequest.webRequest = webRequest;

            // - Start Request -
            webRequest.SendWebRequest();
            activeRequests.Add(newRequest);

            newRequest.processResponse = () =>
            {
                API.WebRequests.ProcessWebResponse<T_APIObj>(webRequest,
                                                             successCallback,
                                                             errorCallback);
            };
        }

        public void OnUpdate()
        {
            List<ActiveAPIRequest> activeRequestsCopy = new List<ActiveAPIRequest>(activeRequests);

            foreach(ActiveAPIRequest request in activeRequestsCopy)
            {
                if(request.webRequest.isDone)
                {
                    request.processResponse();
                    activeRequests.Remove(request);
                }
            }
        }
    }
}
