using System;
using System.Text.RegularExpressions;

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

        public static bool IsEmail(string toCheck)
        {
            string scottsEmailRegex = @"^([a-z0-9\+_\-]+)(\.[a-z0-9\+_\-]+)*@([a-z0-9\-]+\.)+[a-z]{2,63}$";
            Regex regex = new Regex(scottsEmailRegex, RegexOptions.IgnoreCase);

            return regex.IsMatch(toCheck);
        }

        public static bool IsSecurityCode(string toCheck)
        {
            string securityCodeRegex = @"^[a-z0-9]{5}$";
            Regex regex = new Regex(securityCodeRegex, RegexOptions.IgnoreCase);

            return regex.IsMatch(toCheck);
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

        /// <summary>Attempt to extract the video id from a YouTube URL.</summary>
        /// <para>Adapted by for C# by Jackson Wood.</para>
        /// <para>Author: Stephan Schmitz [[Email](mailto:eyecatchup@gmail.com)]</para>
        /// <para>URL: [[https://stackoverflow.com/a/10524505]].</para>
        public static string ExtractYouTubeIdFromURL(string youTubeURL)
        {
            string yt_id = null;
            string pattern = (@"(?:https?:\/\/|\/\/)?(?:www\.|m\.)?(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))([\w-]{11})(?![\w-])");

            var idMatch = Regex.Match(youTubeURL, pattern);
            if(idMatch != null)
            {
                yt_id = idMatch.Groups[1].Value;
            }

            return yt_id;
        }
    }
}
