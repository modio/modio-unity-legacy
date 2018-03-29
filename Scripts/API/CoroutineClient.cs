using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO.API
{
    public class Response<T>
    {
        public T apiObject = default(T);
        public WebRequestError error = null;
        public bool isError = true;
    }

    public delegate void SetValue<T>(out T value);

    public static class CoroutineClient
    {
        private static void OnSuccess<T>(T responseObject, Response<T> response, out bool isDone)
        {
            response.apiObject = responseObject;
            response.isError = false;
            isDone = true;
        }

        private static void OnError<T>(WebRequestError error, Response<T> response, out bool isDone)
        {
            response.error = error;
            response.isError = true;
            isDone = true;
        }

        // ---------[ MOD ENDPOINTS ]---------
        // Get All Mods
        public static IEnumerator GetAllMods(GetAllModsFilter filter,
                                             Response<ObjectArray<ModObject>> response)
        {
            bool isDone = false;

            Client.GetAllMods(filter,
                              (r) => OnSuccess(r, response, out isDone),
                              (e) => OnError(e, response, out isDone));

            while(!isDone) { yield return null; }
        }
    }
}