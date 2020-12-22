using System;
using System.IO;
using UnityEngine;

namespace Cloud.Client
{
    public class ProgressInfo
    {
        public ProgressInfo(string fileName, int bytesSended, int totalBytes)
        {
            Filename = fileName;
            BytesSended = bytesSended;
            TotalBytes = totalBytes;
            Success = false;
            Error = null;
        }

        public int TotalBytes { get; private set; }
        public int BytesSended { get; private set; }
        public float PercentComplete { get { return (float)BytesSended / TotalBytes; } }
        public string Filename { get; private set; }
        public bool IsFinished { get { return BytesSended == TotalBytes; } }
        public string Error { get; set; }
        public bool Success { get; set; }
    }

    public class ResponseInfo
    {
        public ResponseInfo(string fileName, string content)
        {
            Filename = fileName;
            ResponseContent = content;
        }

        public string Filename { get; private set; }
        public string ResponseContent { get; private set; }
    }

    public class CloudUtils
    {
        private readonly static string DOMAIN_DEV = "";
        private readonly static string DOMAIN_RELEASE = "";
        private readonly static string DOMAIN_CN_DEV = "";
        private readonly static string DOMAIN_CN_RELEASE = "";

        public readonly static int CATEGORY_CACHE = 0;
        public readonly static int CATEGORY_MOBILE_RESOURCES = 1000;

        public readonly static string FOLDER_CACHE = "Cache";
        public readonly static string FOLDER_MOBILE_RESOURCES = "MobileResource";

        public readonly static double SIZE_LIMIT = 30 * 1024 * 1024;

        public static string GetCloudDomain(string environment)
        {
            string domain = DOMAIN_DEV;
            string env = environment.ToLower();
            if (null != env && env.Contains("prod"))
            {
                domain = env.Contains("cn")? DOMAIN_CN_RELEASE:  DOMAIN_RELEASE;
            }else if(env.Contains("cn")){
                domain = DOMAIN_CN_DEV;
            }
            return domain;
        }

        public static byte[] StreamToBytes(Stream stream)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            return bytes;
        }

        public static Stream BytesToStream(byte[] bytes)
        {
            Stream stream = new MemoryStream(bytes);
            return stream;
        }
    }

    public class Log
    {
        private readonly static string TAG = "[CloudSdk]";

        private readonly bool _isDebugMode = false;
        public static Log Instance { get; private set; }
        public static void CreateInstance(bool isDebugMode)
        {
            if (Instance == null)
            {
                Instance = new Log(isDebugMode);
            }
        }

        private Log(bool isDebugMode)
        {
            _isDebugMode = isDebugMode;
        }

        public void D(string tag, string message)
        {
            if (_isDebugMode)
            {
                Debug.Log(GetLogString(tag, message));
            }
        }

        public void W(string tag, string message)
        {

            Debug.LogWarning(GetLogString(tag, message));
        }

        public void E(string tag, string message)
        {

            Debug.LogError(GetLogString(tag, message));
        }

        public void Ex(Exception message)
        {

            Debug.LogError(message);
        }

        private string GetLogString(string tag, string message)
        {
            return TAG + tag + " " + message;
        }
    }
}