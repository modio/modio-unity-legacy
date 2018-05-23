using System;
using System.Text.RegularExpressions;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    public static class Utility
    {
        // Author: @jon-hanna of StackOverflow (https://stackoverflow.com/users/400547/jon-hanna)
        // URL: https://stackoverflow.com/a/5419544
        public static bool Like(this string toSearch, string toFind)
        {
            return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(toSearch);
        }

        public static bool IsURL(string toCheck)
        {
            // URL Regex adapted from https://regex.wtf/url-matching-regex-javascript/
            string protocol = "^(http(s)?(://))?(www.)?";
            string domain = "[a-zA-Z0-9-_.]+";
            Regex urlRegex = new Regex(protocol + domain, RegexOptions.IgnoreCase);

            return urlRegex.IsMatch(toCheck);
        }

        public static void SafeMapArraysOrZero<T1, T2>(T1[] sourceArray,
                                                       Func<T1, T2> mapElementDelegate,
                                                       out T2[] destinationArray)
        {
            if(sourceArray == null) { destinationArray = new T2[0]; }
            else
            {
                destinationArray = new T2[sourceArray.Length];
                for(int i = 0;
                    i < sourceArray.Length;
                    ++i)
                {
                    destinationArray[i] = mapElementDelegate(sourceArray[i]);
                }
            }
        }

        public static string GenerateExceptionDebugString(Exception e)
        {
            var debugString = new System.Text.StringBuilder();

            Exception baseException = e.GetBaseException();
            debugString.Append(baseException.GetType().Name + ": " + baseException.Message + "\n");

            var stackTrace = new System.Diagnostics.StackTrace(baseException, true);

            int frameCount = Math.Min(stackTrace.FrameCount, 6);
            for(int i = 0; i < frameCount; ++i)
            {
                var stackFrame = stackTrace.GetFrame(i);
                var method = stackFrame.GetMethod();

                debugString.Append(method.ReflectedType
                                   + "." + method.Name + "(");

                var methodsParameters = method.GetParameters();
                foreach(var parameter in methodsParameters)
                {
                    debugString.Append(parameter.ParameterType.Name + " "
                                       + parameter.Name + ", ");
                }
                if(methodsParameters.Length > 0)
                {
                    debugString.Length -= 2;
                }

                debugString.Append(") @ " + stackFrame.GetFileName()
                                   + ":" + stackFrame.GetFileLineNumber()
                                   + "\n");
            }

            return debugString.ToString();
        }
    }
}
